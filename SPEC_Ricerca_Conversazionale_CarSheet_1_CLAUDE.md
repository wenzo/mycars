# MyCars/EasyCars — Migrazione alla ricerca veicoli conversazionale (AI-aided)

> **Documento di specifica per Claude Code.**
> Questo file descrive un intervento da eseguire sul **repository CarSheet reale** (backend ASP.NET Core + app mobile Ionic/Vue/Capacitor). Non assumere alcuna struttura: prima **analizza** il codice esistente, poi applica l'architettura descritta qui.

## Istruzioni per l'esecutore (leggere prima di iniziare)

1. Leggi **tutto** questo documento prima di scrivere qualsiasi riga di codice.
2. Procedi **per fasi**, nell'ordine indicato.
3. **Non scrivere codice durante la Fase 0.** Completa l'analisi e **presentamela**; attendi conferma prima di passare alla Fase 1.
4. Lavora a **piccoli step incrementali ad alta confidenza**. Dopo ogni modifica, spiega cosa è cambiato e quali file hai toccato.
5. In caso di dubbio o ambiguità (es. rimozione di filtri esistenti, contratti non chiari), **fermati e chiedi** invece di procedere.

---

## 1. Contesto del progetto

MyCars è un'app per concessionarie/rivenditori di auto: vetrina dei veicoli + servizio di noleggio.

- **Backend**: ASP.NET Core 8, Web API.
- **Accesso dati**: ADO.NET tramite **Npgsql** su PostgreSQL. **Niente Entity Framework.** Il progetto usa il pattern **entità → DTO → repository → controller**.
- **App mobile**: Ionic + Vue 3 + Capacitor.
- **Router**: esiste una separazione tra area **public** (catalogo) e area **admin**. Va preservata.

Oggi la ricerca veicoli è basata su **filtri classici** (marca, prezzo, km, ecc.). L'obiettivo è affiancare/sostituire questa ricerca con una **ricerca conversazionale assistita da AI**.

---

## 2. Obiettivo

Permettere all'utente di cercare i veicoli scrivendo una frase in linguaggio naturale — es. *"SUV diesel sotto i 15.000 € per famiglia con due bambini"* — che viene tradotta in criteri strutturati e poi in una query sul catalogo.

Vincolo trasversale: l'integrazione deve essere **retrocompatibile** (contratti API, DTO, router e struttura esistenti restano invariati) e deve supportare **tre provider LLM intercambiabili** (Groq, Anthropic, OpenAI) selezionabili da `appsettings.json`.

---

## 3. Architettura di riferimento (già concordata)

Pipeline da realizzare:

```
frase utente (mobile)
   -> endpoint di ricerca esistente (parametro testo libero, retrocompatibile)
   -> ICriteriaExtractor (estrazione via LLM, function calling con tool forzato)
   -> SearchCriteria (DTO)
   -> QueryBuilder (SQL parametrizzato, ORDER BY da whitelist)
   -> repository Npgsql esistente
   -> stesso DTO di risposta già consumato dal client
```

**Principio di sicurezza non negoziabile:** il modello **non genera mai SQL**. Compila soltanto un DTO validato; tutti i valori passano come **parametri Npgsql** e l'`ORDER BY` è scelto da una **whitelist**. Nessuna concatenazione di stringhe provenienti dall'utente o dal modello.

**Robustezza:** in caso di errore del provider o estrazione vuota, **fallback** automatico su una ricerca per parole chiave (`ILIKE` su marca/modello). Cache opzionale dei criteri estratti per query identiche (es. `IMemoryCache`).

### 3.1 Schema logico dei criteri (`SearchCriteria`)

DTO semplice (POCO), già previsto. Campi e domini:

| Campo | Tipo | Valori ammessi | Note |
|---|---|---|---|
| `BodyType` | `List<string>?` | berlina, suv, station_wagon, city_car, monovolume, coupe, cabrio, pickup | opzionale |
| `FuelType` | `List<string>?` | benzina, diesel, gpl, metano, ibrida, elettrica | opzionale |
| `PriceMax` | `decimal?` | numero | opzionale |
| `PriceMin` | `decimal?` | numero | opzionale |
| `MinSeats` | `int?` | intero | "famiglia con due bambini" => almeno 5 |
| `Transmission` | `string?` | manuale, automatico | opzionale |
| `Intent` | `string` | acquisto, noleggio, qualsiasi | **required** |
| `Sort` | `string` | prezzo_asc, prezzo_desc, anno_desc, km_asc, rilevanza | **required** |

Lo schema dello strumento (`cerca_veicoli`) va **definito una sola volta** e riusato dai tre extractor. Le `description` dei campi fungono da istruzioni di mappatura (es. insegnare al modello che "famiglia con due bambini" implica ≥5 posti e carrozzerie suv/station_wagon/monovolume).

---

## 4. Provider AI: astrazione `ICriteriaExtractor`

Creare l'interfaccia e **tre** implementazioni. La logica di ricerca (servizio, repository, controller) deve dipendere **solo dall'interfaccia**, mai da un provider specifico.

```csharp
public interface ICriteriaExtractor
{
    // Restituisce i criteri estratti, oppure null se l'estrazione fallisce
    // (in tal caso il chiamante ricade sulla ricerca per parole chiave).
    Task<SearchCriteria?> ExtractAsync(string userQuery, CancellationToken ct);
}
```

Implementazioni:

- `AnthropicCriteriaExtractor`
- `GroqCriteriaExtractor`
- `OpenAiCriteriaExtractor`

### 4.1 Differenze tra i provider (da gestire dentro le implementazioni)

Groq e OpenAI condividono lo **stesso formato** (API compatibile OpenAI). Anthropic usa il proprio formato.

| Provider | Endpoint | Header auth | Campo schema | Argomenti nella risposta | tool_choice forzato |
|---|---|---|---|---|---|
| **Anthropic** | `/v1/messages` | `x-api-key` + `anthropic-version: 2023-06-01` | `input_schema` | `content[].type=="tool_use"` → `.input` (**oggetto**) | `{ type="tool", name="cerca_veicoli" }` |
| **Groq** | `/openai/v1/chat/completions` | `Authorization: Bearer <key>` | `function.parameters` | `choices[0].message.tool_calls[0].function.arguments` (**stringa JSON**) | `{ type="function", function={ name="cerca_veicoli" } }` |
| **OpenAI** | `/v1/chat/completions` | `Authorization: Bearer <key>` | `function.parameters` | `choices[0].message.tool_calls[0].function.arguments` (**stringa JSON**) | `{ type="function", function={ name="cerca_veicoli" } }` |

> Nota: per Groq/OpenAI gli argomenti arrivano come **stringa JSON** da deserializzare in `SearchCriteria` (un passaggio in più rispetto ad Anthropic, dove `input` è già un oggetto). Con il tool forzato, se il modello non chiama la funzione i provider restituiscono **400**: lasciare che l'eccezione faccia scattare il fallback per parole chiave.

### 4.2 Selezione del provider (config-driven)

La scelta del provider avviene **da configurazione**, senza modificare il codice. Approccio consigliato (adattalo allo stile DI già presente nel progetto):

- Registrare tutte e tre le implementazioni.
- Risolvere `ICriteriaExtractor` in base a `Ai:Provider` tramite **keyed services (.NET 8)** oppure un piccolo **factory/resolver**.
- Bindare la sotto-sezione `Ai:Providers:<nome>` su un options object per ogni implementazione.

---

## 5. Configurazione `appsettings.json`

Aggiungere una sezione `Ai` con un **selettore di provider** e una mappa di provider. I segreti restano **vuoti** nel file versionato (vedi §9).

```jsonc
{
  "Ai": {
    "Provider": "Groq",            // Anthropic | Groq | OpenAI
    "Providers": {
      "Anthropic": {
        "ApiKey": "",
        "Model": "claude-haiku-4-5-20251001",
        "BaseUrl": "https://api.anthropic.com/v1/messages",
        "MaxTokens": 1024
      },
      "Groq": {
        "ApiKey": "",
        "Model": "llama-3.3-70b-versatile",
        "BaseUrl": "https://api.groq.com/openai/v1/chat/completions",
        "MaxTokens": 1024
      },
      "OpenAI": {
        "ApiKey": "",
        "Model": "gpt-4o-mini",
        "BaseUrl": "https://api.openai.com/v1/chat/completions",
        "MaxTokens": 1024
      }
    }
  }
}
```

> Gli **ID dei modelli** qui sono **esempi da verificare** sulla documentazione/endpoint di ciascun provider (es. la lista modelli di Groq via `GET /openai/v1/models`). Non considerarli definitivi.

Cambiare provider deve richiedere **solo** la modifica di `Ai:Provider` (+ riavvio), senza ricompilare la logica.

---

## 6. Piano di lavoro per fasi

### Fase 0 — Analisi (NIENTE codice; produrre un report e fermarsi)

Esamina il repository e individua **tutti** i punti coinvolti dalla ricerca veicoli, sia backend che mobile:

- **Backend**: controller/endpoint di ricerca, metodi del repository, query SQL, DTO di richiesta/risposta della ricerca.
- **Mobile**: componenti/pagine Vue della ricerca, servizio HTTP che chiama l'endpoint, voci di `vue-router` (public/admin), UI dei filtri.

Restituisci una tabella **"Punti di intervento"** in questo formato:

| Area | File (path) | Righe | Cosa fa oggi | Azione prevista (sostituire / estendere / lasciare) |
|---|---|---|---|---|

Aggiungi anche:
- Il **contratto attuale** dell'endpoint di ricerca (rotta, verbo, parametri, DTO di risposta).
- Il confine **public vs admin** (backend e router mobile).

**Fermati qui e presenta l'analisi prima di procedere.**

### Fase 1 — Astrazione provider (backend)
- Crea `ICriteriaExtractor` e le tre implementazioni (§4).
- Centralizza lo schema `cerca_veicoli` (§3.1) e riusalo nei tre provider (adattando `input_schema` vs `function.parameters`).
- Registra in DI con selezione da `Ai:Provider` (§4.2).

### Fase 2 — Wiring del servizio di ricerca (backend)
- Il servizio di ricerca conversazionale dipende da `ICriteriaExtractor`. Mantieni **cache** + **fallback** per parole chiave.
- **Preserva la rotta e il DTO di risposta esistenti.** Introduci l'ingresso testo libero in modo **retrocompatibile** (es. parametro opzionale `q`): quando `q` è valorizzato, il flusso passa per `ICriteriaExtractor → SearchCriteria → repository esistente → stesso DTO di risposta`. Quando `q` è assente, il comportamento attuale resta invariato.

### Fase 3 — Configurazione
- Aggiungi la sezione `Ai` in `appsettings.json` (§5). Non inserire segreti nel file versionato.

### Fase 4 — App mobile (Ionic + Vue)
- Individua la UI di ricerca e sostituisci l'input principale con un **campo di ricerca conversazionale** (testo libero) che chiama lo **stesso endpoint** (stesso contratto).
- Mantieni il markup nei file template (`.html`/`.vue`); **non** spostare HTML in template literal JS.
- Aggiungi uno **stato di caricamento** (l'estrazione può richiedere ~1 secondo).
- **Non rimuovere** i filtri esistenti se ciò comporta modifiche estese: in tal caso, proponili come "ricerca avanzata" (toggle) o **chiedi conferma**. Mantieni invariati endpoint, contratti, nomi delle classi CSS e router.

---

## 7. Vincolo sul modello dati

- **Non** introdurre Entity Framework Core.
- Riusa il pattern esistente **entità / DTO / repository / controller**.
- `SearchCriteria` è un DTO semplice. Il `QueryBuilder` produce SQL **parametrizzato** per Npgsql. La ricerca conversazionale deve restituire **lo stesso DTO/lista veicoli** già consumato dal client.

---

## 8. Regole operative non negoziabili (hard constraints)

- Keep HTML structure in `.html` files unless strictly necessary or you have to suggest me more efficiently or better usability.
- Avoid large template literals containing HTML inside JavaScript files.
- Preserve existing CSS class names unless strictly necessary.
- Preserve existing API endpoints and DTO contracts.
- Preserve the current public/admin router architecture.
- Prefer small, incremental, high-confidence changes.
- Do not refactor unrelated files.
- Do not rename files unless explicitly requested.
- After each change, explain what was changed and which files were touched.

---

## 9. Gestione dei segreti

- API key dei provider e password DB **mai** committate.
- Sviluppo: `dotnet user-secrets`. Produzione: variabili d'ambiente (il doppio underscore mappa la gerarchia, es. `Ai__Providers__Groq__ApiKey`) o un secret manager.
- `appsettings.json` versionato deve avere i campi `ApiKey` **vuoti**.

---

## 10. Definition of Done

- Ricerca conversazionale funzionante **end-to-end**: mobile → endpoint esistente → estrazione → SQL parametrizzato → risultati, con lo **stesso DTO di risposta**.
- **Tre provider** (Groq, Anthropic, OpenAI) selezionabili da `appsettings.json` con la sola modifica di `Ai:Provider`, senza ricompilare la logica.
- Nessuna modifica a: contratti API/DTO esistenti, nomi delle classi CSS, architettura router public/admin.
- **Fallback** per parole chiave attivo in caso di errore/estrazione vuota.
- Nessun SQL generato dall'AI; valori sempre parametrizzati; `ORDER BY` da whitelist.
- Nessun file non correlato modificato; nessun file rinominato senza richiesta esplicita.
- Build verde; per ogni modifica, log chiaro di "cosa è cambiato / file toccati".

---

## 11. Note finali

- Verifica gli **ID dei modelli** sulla documentazione corrente di ciascun provider prima di considerarli definitivi.
- Per Groq/OpenAI ricorda il **doppio parsing** (la risposta contiene una stringa JSON da deserializzare in `SearchCriteria`).
- Valuta la **qualità della mappatura** in italiano con query reali (es. "monovolume 7 posti automatico per noleggio"): i modelli grandi sono più affidabili sulle inferenze rispetto ai piccoli.
