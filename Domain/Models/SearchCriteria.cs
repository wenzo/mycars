namespace MyCars.Domain.Models;

// DTO compilato dall'LLM tramite function calling; mai generato da SQL diretto.
public sealed class SearchCriteria
{
    // berlina | suv | station_wagon | city_car | monovolume | coupe | cabrio | pickup
    public List<string>? BodyType     { get; set; }

    // benzina | diesel | gpl | metano | ibrida | elettrica
    public List<string>? FuelType     { get; set; }

    public decimal? PriceMax     { get; set; }
    public decimal? PriceMin     { get; set; }

    // "famiglia con due bambini" => almeno 5
    public int?     MinSeats     { get; set; }

    // manuale | automatico
    public string?  Transmission { get; set; }

    // marca del veicolo (es. BMW, Volkswagen, Fiat) — null se non menzionata
    public string?  Brand        { get; set; }

    // modello specifico (es. Golf, Panda, Serie 3, Yaris) — null se non menzionato
    public string?  Model        { get; set; }

    // potenza in CV — per traino roulotte, prestazioni, consumi
    public int?     MinHorsepowerCv { get; set; }
    public int?     MaxHorsepowerCv { get; set; }

    // cilindrata in cc — piccola (<1200), media (1400-2000), grande (>2000)
    public int?     MinEngineCc  { get; set; }
    public int?     MaxEngineCc  { get; set; }

    // anno di immatricolazione — calcolato dall'AI in base all'anno corrente
    public int?     MinYear      { get; set; }
    public int?     MaxYear      { get; set; }

    // colore (es. "rosso", "bianco", "nero", "grigio", "blu")
    public string?  Color        { get; set; }

    // classe di emissioni (es. "Euro 6", "Euro 5", "Euro 4")
    public string?  EmissionClass { get; set; }

    // parola chiave libera da cercare nel campo descrizione del dealer
    // es. "revisione effettuata", "tagliando", "tetto apribile", "cerchi in lega"
    public string?  DescriptionKeyword { get; set; }

    // stato del veicolo: "nuovo" | "usato" | "km0" — null = qualsiasi
    public string?  Condition    { get; set; }

    // max chilometri percorsi — "pochi km", "meno di 50.000 km"
    public int?     MaxMileageKm { get; set; }

    // IVA esposta/detraibile — "per aziende", "IVA detraibile", "partita IVA"
    public bool?    VatDeductible { get; set; }

    // accessibile portatori di handicap
    public bool?    HandicapAccessible { get; set; }

    // veicolo di importazione — "importata", "provenienza estera"
    public bool?    Imported     { get; set; }

    // veicolo incidentato — "non incidentata" → false, "accidentata" → true
    public bool?    Damaged      { get; set; }

    // acquisto | noleggio | qualsiasi  (required dal modello)
    public string   Intent       { get; set; } = "qualsiasi";

    // prezzo_asc | prezzo_desc | anno_desc | km_asc | rilevanza  (required dal modello)
    public string   Sort         { get; set; } = "rilevanza";
}
