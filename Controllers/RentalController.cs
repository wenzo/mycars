using System.Net;
using MyCars.Domain.Models;
using MyCars.Domain.Repositories;
using MyCars.Infrastructure.Push;

namespace MyCars.Controllers;

[ApiController]
[Route("api/admin/rentals")]
[Authorize(Roles = "Admin")]
public sealed class RentalController : ControllerBase
{
    private readonly IRentalRepository    _rentals;
    private readonly IVehicleRepository   _vehicles;
    private readonly IOperatorRepository  _operators;
    private readonly IFileStorageService  _storage;
    private readonly IPushRepository      _push;
    private readonly IWebPushService      _webPush;
    private readonly ILogger<RentalController> _log;

    public RentalController(
        IRentalRepository       rentals,
        IVehicleRepository      vehicles,
        IOperatorRepository     operators,
        IFileStorageService     storage,
        IPushRepository         push,
        IWebPushService         webPush,
        ILogger<RentalController> log)
    {
        _rentals   = rentals;
        _vehicles  = vehicles;
        _operators = operators;
        _storage   = storage;
        _push      = push;
        _webPush   = webPush;
        _log       = log;
    }

    private Guid GetOperatorId() =>
        Guid.Parse(User.FindFirstValue("operator_id")!);

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var opId = GetOperatorId();
        var active         = await _rentals.CountByStatusAsync(opId, "active");
        var booked         = await _rentals.CountByStatusAsync(opId, "booked");
        var returningToday = await _rentals.GetReturningTodayAsync(opId);

        return Ok(new
        {
            active_count          = active,
            booked_count          = booked,
            returning_today_count = returningToday.Count,
            returning_today       = returningToday,
        });
    }

    // ── Lista ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var opId   = GetOperatorId();
        var result = await _rentals.GetByOperatorAsync(opId,
            new PageRequest(page, Math.Clamp(pageSize, 1, 100)), status);
        return Ok(result);
    }

    // ── Dettaglio ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var rental = await _rentals.GetByIdAsync(id, GetOperatorId());
        return rental is null ? NotFound() : Ok(rental);
    }

    // ── Disponibilità ─────────────────────────────────────────────────────────

    [HttpGet("availability")]
    public async Task<IActionResult> CheckAvailability(
        [FromQuery] Guid vehicleId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? excludeRentalId = null)
    {
        if (endDate < startDate)
            return BadRequest(new { message = "La data fine deve essere >= data inizio." });

        var available = await _rentals.IsAvailableAsync(vehicleId, startDate, endDate, excludeRentalId);
        return Ok(new { available });
    }

    // ── Crea ──────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRentalRequest req)
    {
        var opId = GetOperatorId();

        if (req.PlannedEndDate < req.StartDate)
            return BadRequest(new { message = "La data fine deve essere >= data inizio." });

        var vehicle = await _vehicles.GetByIdAsync(req.VehicleId, opId);
        if (vehicle is null) return NotFound(new { message = "Veicolo non trovato." });
        if (!vehicle.ForRental) return BadRequest(new { message = "Il veicolo non è disponibile per il noleggio." });

        var available = await _rentals.IsAvailableAsync(req.VehicleId, req.StartDate, req.PlannedEndDate);
        if (!available)
            return Conflict(new { message = "Il veicolo non è disponibile nel periodo richiesto." });

        var rental = await _rentals.CreateAsync(new Rental
        {
            OperatorId          = opId,
            VehicleId           = req.VehicleId,
            CustomerName        = req.CustomerName.Trim(),
            CustomerPhone       = req.CustomerPhone?.Trim(),
            CustomerLicense     = req.CustomerLicense?.Trim(),
            CustomerFiscalCode  = req.CustomerFiscalCode?.Trim(),
            StartDate           = req.StartDate,
            PlannedEndDate      = req.PlannedEndDate,
            AgreedPrice         = req.AgreedPrice,
            DepositAmount       = req.DepositAmount,
            PaymentMethod       = req.PaymentMethod,
            Notes               = req.Notes?.Trim(),
        });

        _log.LogInformation("Noleggio creato: id={Id} operatorId={OpId} vehicleId={VId}",
            rental.Id, opId, req.VehicleId);

        return CreatedAtAction(nameof(GetById), new { id = rental.Id }, rental);
    }

    // ── Aggiorna ──────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRentalRequest req)
    {
        var opId   = GetOperatorId();
        var rental = await _rentals.GetByIdAsync(id, opId);
        if (rental is null) return NotFound();
        if (rental.Status is "closed" or "cancelled")
            return BadRequest(new { message = "Impossibile modificare un noleggio concluso o annullato." });

        if (req.PlannedEndDate < req.StartDate)
            return BadRequest(new { message = "La data fine deve essere >= data inizio." });

        if (req.StartDate != rental.StartDate || req.PlannedEndDate != rental.PlannedEndDate)
        {
            var available = await _rentals.IsAvailableAsync(
                rental.VehicleId, req.StartDate, req.PlannedEndDate, id);
            if (!available)
                return Conflict(new { message = "Il veicolo non è disponibile nel nuovo periodo." });
        }

        rental.CustomerName       = req.CustomerName.Trim();
        rental.CustomerPhone      = req.CustomerPhone?.Trim();
        rental.CustomerLicense    = req.CustomerLicense?.Trim();
        rental.CustomerFiscalCode = req.CustomerFiscalCode?.Trim();
        rental.StartDate          = req.StartDate;
        rental.PlannedEndDate     = req.PlannedEndDate;
        rental.AgreedPrice        = req.AgreedPrice;
        rental.DepositAmount      = req.DepositAmount;
        rental.DepositReturned    = req.DepositReturned;
        rental.PaymentMethod      = req.PaymentMethod;
        rental.IsPaid             = req.IsPaid;
        rental.Notes              = req.Notes?.Trim();

        var updated = await _rentals.UpdateAsync(rental);
        return Ok(updated);
    }

    // ── Attiva (consegna) ─────────────────────────────────────────────────────

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateRentalRequest req)
    {
        var ok = await _rentals.ActivateAsync(id, GetOperatorId(), req.KmDeparture, req.FuelDeparture);
        return ok ? Ok(new { message = "Noleggio attivato." }) : NotFound();
    }

    // ── Chiudi (rientro) ──────────────────────────────────────────────────────

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseRentalRequest req)
    {
        var actualEnd = req.ActualEndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var ok = await _rentals.CloseAsync(id, GetOperatorId(), actualEnd, req.KmReturn, req.FuelReturn);
        return ok ? Ok(new { message = "Noleggio chiuso." }) : NotFound();
    }

    // ── Annulla ───────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _rentals.CancelAsync(id, GetOperatorId());
        return ok ? Ok(new { message = "Noleggio annullato." }) : NotFound();
    }

    // ── Foto ──────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/photos")]
    public async Task<IActionResult> GetPhotos(Guid id)
    {
        var opId   = GetOperatorId();
        var rental = await _rentals.GetByIdAsync(id, opId);
        if (rental is null) return NotFound();
        var photos = await _rentals.GetPhotosAsync(id, opId);
        return Ok(photos);
    }

    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> UploadPhoto(
        Guid id, IFormFile file, [FromQuery] string phase = "departure")
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File obbligatorio." });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "Il file non può superare 10 MB." });
        if (phase is not ("departure" or "return"))
            return BadRequest(new { message = "Phase deve essere 'departure' o 'return'." });

        var opId   = GetOperatorId();
        var rental = await _rentals.GetByIdAsync(id, opId);
        if (rental is null) return NotFound();

        try
        {
            var fileName = Guid.NewGuid().ToString("N")[..16];
            var url      = await _storage.SaveAsync(file, $"rentals/{opId}/{id}/{phase}", fileName);
            var photo    = await _rentals.AddPhotoAsync(new RentalPhoto
            {
                RentalId   = id,
                OperatorId = opId,
                Phase      = phase,
                Url        = url,
            });
            return Ok(photo);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Errore upload foto noleggio {Id}", id);
            return StatusCode(500, new { message = "Errore durante l'upload." });
        }
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId)
    {
        var ok = await _rentals.DeletePhotoAsync(photoId, id, GetOperatorId());
        return ok ? NoContent() : NotFound();
    }

    // ── Contratto HTML (per stampa / salvataggio PDF) ─────────────────────────

    [HttpGet("{id:guid}/contract")]
    public async Task<IActionResult> GetContract(Guid id)
    {
        var opId   = GetOperatorId();
        var rental = await _rentals.GetByIdAsync(id, opId);
        if (rental is null) return NotFound();

        var profile = await _operators.GetByIdAsync(opId);
        var vehicle = await _vehicles.GetByIdAsync(rental.VehicleId, opId);
        var photos  = await _rentals.GetPhotosAsync(id, opId);

        var html = BuildContractHtml(rental, profile, vehicle, photos);
        return Content(html, "text/html; charset=utf-8");
    }

    // ── Notifica push promemoria rientro ──────────────────────────────────────

    [HttpPost("{id:guid}/remind")]
    public async Task<IActionResult> SendReturnReminder(Guid id)
    {
        var opId   = GetOperatorId();
        var rental = await _rentals.GetByIdAsync(id, opId);
        if (rental is null) return NotFound();
        if (rental.Status != "active")
            return BadRequest(new { message = "Il promemoria può essere inviato solo a noleggi attivi." });

        var subscriptions = await _push.GetAllAsync(opId);
        var vehicle = $"{rental.VehicleBrand} {rental.VehicleModel}".Trim();
        var title   = "Rientro previsto";
        var body    = $"{rental.CustomerName} — {vehicle} previsto il {rental.PlannedEndDate:dd/MM/yyyy}";

        int sent = 0;
        try
        {
            sent = await _webPush.SendAsync(subscriptions, title, body);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Push noleggio {Id}: invio parziale o fallito", id);
        }

        return Ok(new { sent });
    }

    // ── HTML contratto ────────────────────────────────────────────────────────

    private static string BuildContractHtml(
        Rental rental,
        OperatorProfile? profile,
        Vehicle? vehicle,
        IReadOnlyList<RentalPhoto> photos)
    {
        // Pre-calcola tutti i valori per evitare espressioni complesse nel template
        var op             = profile?.BusinessName ?? "—";
        var opPhone        = profile?.Phone ?? "";
        var opEmail        = profile?.Email ?? "";
        var opAddr         = $"{profile?.Address}, {profile?.City} ({profile?.Province})".Trim(',', ' ');
        var customerName   = rental.CustomerName;
        var customerPhone  = rental.CustomerPhone ?? "—";
        var customerLic    = rental.CustomerLicense ?? "—";
        var customerCf     = rental.CustomerFiscalCode ?? "—";
        var vDesc          = vehicle is not null
            ? $"{rental.VehicleBrand} {rental.VehicleModel} — {vehicle.Targa}"
            : $"{rental.VehicleBrand} {rental.VehicleModel}".Trim();
        var days           = rental.PlannedEndDate.DayNumber - rental.StartDate.DayNumber + 1;
        var daysLabel      = days == 1 ? "giorno" : "giorni";
        var startDateStr   = rental.StartDate.ToString("dd/MM/yyyy");
        var endDateStr     = rental.PlannedEndDate.ToString("dd/MM/yyyy");
        var actualEndStr   = rental.ActualEndDate.HasValue
            ? rental.ActualEndDate.Value.ToString("dd/MM/yyyy") : "—";
        var kmDep          = rental.KmDeparture.HasValue
            ? rental.KmDeparture.Value.ToString("N0") + " km" : "—";
        var kmRet          = rental.KmReturn.HasValue
            ? rental.KmReturn.Value.ToString("N0") + " km" : "—";
        var fuelDep        = FuelLabel(rental.FuelDeparture);
        var fuelRet        = FuelLabel(rental.FuelReturn);
        var total          = rental.AgreedPrice.HasValue
            ? "€ " + rental.AgreedPrice.Value.ToString("N2") : "—";
        var deposit        = rental.DepositAmount.HasValue
            ? "€ " + rental.DepositAmount.Value.ToString("N2") : "—";
        var depReturned    = rental.DepositReturned ? "Sì" : "No";
        var isPaid         = rental.IsPaid ? "Sì" : "No";
        var pm             = rental.PaymentMethod switch
        {
            "cash"     => "Contanti",
            "pos"      => "POS / Carta",
            "transfer" => "Bonifico",
            _          => "—"
        };
        var contractNum    = rental.Id.ToString()[..8].ToUpper();
        var now            = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var notesHtml      = WebUtility.HtmlEncode(rental.Notes ?? "");

        var sb = new System.Text.StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"it\">\n<head>\n");
        sb.Append("<meta charset=\"UTF-8\">\n");
        sb.Append($"<title>Contratto di Noleggio — {customerName}</title>\n");
        sb.Append("<style>\n");
        sb.Append("@media print { @page { margin: 18mm; } button { display:none; } }\n");
        sb.Append("* { box-sizing: border-box; }\n");
        sb.Append("body { font-family: Arial, sans-serif; font-size: 11pt; color: #111; margin: 0; padding: 20px; }\n");
        sb.Append("h1 { font-size: 16pt; text-align: center; margin-bottom: 4px; }\n");
        sb.Append(".subtitle { text-align: center; color: #555; font-size: 10pt; margin-bottom: 24px; }\n");
        sb.Append(".section { border: 1px solid #ccc; border-radius: 6px; padding: 12px 16px; margin-bottom: 16px; }\n");
        sb.Append(".section h2 { font-size: 11pt; text-transform: uppercase; letter-spacing: .05em; color: #333; margin: 0 0 10px; border-bottom: 1px solid #ddd; padding-bottom: 6px; }\n");
        sb.Append("table.info { width: 100%; border-collapse: collapse; }\n");
        sb.Append("table.info td { padding: 4px 6px; vertical-align: top; }\n");
        sb.Append("table.info td:first-child { color: #555; width: 38%; }\n");
        sb.Append(".highlight { background: #f5f5f5; border-radius: 4px; padding: 8px 12px; font-weight: bold; }\n");
        sb.Append(".photos { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 8px; }\n");
        sb.Append(".photos img { width: 120px; height: 90px; object-fit: cover; border-radius: 4px; border: 1px solid #ddd; }\n");
        sb.Append(".sign-box { border: 1px dashed #aaa; height: 70px; border-radius: 4px; margin-top: 8px; }\n");
        sb.Append(".footer { text-align: center; font-size: 9pt; color: #999; margin-top: 24px; }\n");
        sb.Append(".btn-print { display: block; margin: 0 auto 20px; padding: 10px 24px; background: #1E3A5F; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-size: 11pt; }\n");
        sb.Append("</style>\n</head>\n<body>\n");
        sb.Append("<button class=\"btn-print\" onclick=\"window.print()\">Stampa / Salva PDF</button>\n");
        sb.Append("<h1>Contratto di Noleggio</h1>\n");
        sb.Append($"<div class=\"subtitle\">N. {contractNum} — Emesso il {now}</div>\n");

        sb.Append("<div class=\"section\"><h2>Locatore</h2><table class=\"info\">\n");
        sb.Append($"<tr><td>Ragione sociale</td><td><strong>{op}</strong></td></tr>\n");
        sb.Append($"<tr><td>Indirizzo</td><td>{opAddr}</td></tr>\n");
        sb.Append($"<tr><td>Telefono</td><td>{opPhone}</td></tr>\n");
        sb.Append($"<tr><td>Email</td><td>{opEmail}</td></tr>\n");
        sb.Append("</table></div>\n");

        sb.Append("<div class=\"section\"><h2>Conduttore</h2><table class=\"info\">\n");
        sb.Append($"<tr><td>Nome e Cognome</td><td><strong>{customerName}</strong></td></tr>\n");
        sb.Append($"<tr><td>Telefono</td><td>{customerPhone}</td></tr>\n");
        sb.Append($"<tr><td>N. Patente</td><td>{customerLic}</td></tr>\n");
        sb.Append($"<tr><td>Codice Fiscale</td><td>{customerCf}</td></tr>\n");
        sb.Append("</table></div>\n");

        sb.Append("<div class=\"section\"><h2>Veicolo</h2><table class=\"info\">\n");
        sb.Append($"<tr><td>Descrizione</td><td><strong>{vDesc}</strong></td></tr>\n");
        sb.Append("</table></div>\n");

        sb.Append("<div class=\"section\"><h2>Periodo di Noleggio</h2><table class=\"info\">\n");
        sb.Append($"<tr><td>Data inizio</td><td><strong>{startDateStr}</strong></td></tr>\n");
        sb.Append($"<tr><td>Data fine prevista</td><td><strong>{endDateStr}</strong></td></tr>\n");
        sb.Append($"<tr><td>Durata</td><td>{days} {daysLabel}</td></tr>\n");
        sb.Append($"<tr><td>Data rientro effettivo</td><td>{actualEndStr}</td></tr>\n");
        sb.Append("</table></div>\n");

        sb.Append("<div class=\"section\"><h2>Condizioni alla Consegna</h2><table class=\"info\">\n");
        sb.Append($"<tr><td>Km alla partenza</td><td>{kmDep}</td></tr>\n");
        sb.Append($"<tr><td>Carburante partenza</td><td>{fuelDep}</td></tr>\n");
        sb.Append($"<tr><td>Km al rientro</td><td>{kmRet}</td></tr>\n");
        sb.Append($"<tr><td>Carburante rientro</td><td>{fuelRet}</td></tr>\n");
        sb.Append("</table></div>\n");

        if (rental.AgreedPrice.HasValue || rental.DepositAmount.HasValue)
        {
            sb.Append("<div class=\"section\"><h2>Condizioni Economiche</h2><table class=\"info\">\n");
            sb.Append($"<tr><td>Importo concordato</td><td class=\"highlight\">{total}</td></tr>\n");
            sb.Append($"<tr><td>Deposito cauzionale</td><td>{deposit}</td></tr>\n");
            sb.Append($"<tr><td>Deposito restituito</td><td>{depReturned}</td></tr>\n");
            sb.Append($"<tr><td>Modalità pagamento</td><td>{pm}</td></tr>\n");
            sb.Append($"<tr><td>Pagato</td><td>{isPaid}</td></tr>\n");
            sb.Append("</table></div>\n");
        }

        if (!string.IsNullOrWhiteSpace(rental.Notes))
        {
            sb.Append("<div class=\"section\"><h2>Note</h2>\n");
            sb.Append($"<p style=\"margin:0\">{notesHtml}</p>\n");
            sb.Append("</div>\n");
        }

        var depPhotos = photos.Where(p => p.Phase == "departure").ToList();
        var retPhotos = photos.Where(p => p.Phase == "return").ToList();

        if (depPhotos.Count > 0 || retPhotos.Count > 0)
        {
            sb.Append("<div class=\"section\"><h2>Foto Veicolo</h2>\n");
            if (depPhotos.Count > 0)
            {
                sb.Append("<p style=\"margin:4px 0 2px\"><strong>Alla consegna</strong></p><div class=\"photos\">\n");
                foreach (var ph in depPhotos)
                    sb.Append($"<img src=\"{ph.Url}\" alt=\"foto partenza\">\n");
                sb.Append("</div>\n");
            }
            if (retPhotos.Count > 0)
            {
                sb.Append("<p style=\"margin:12px 0 2px\"><strong>Al rientro</strong></p><div class=\"photos\">\n");
                foreach (var ph in retPhotos)
                    sb.Append($"<img src=\"{ph.Url}\" alt=\"foto rientro\">\n");
                sb.Append("</div>\n");
            }
            sb.Append("</div>\n");
        }

        sb.Append("<div class=\"section\" style=\"margin-top:32px\"><h2>Firme</h2>\n");
        sb.Append("<table style=\"width:100%;border-collapse:collapse\"><tr>\n");
        sb.Append("<td style=\"width:48%;padding-right:8px\"><div style=\"font-size:10pt;margin-bottom:4px\">Locatore</div><div class=\"sign-box\"></div></td>\n");
        sb.Append("<td style=\"width:4%\"></td>\n");
        sb.Append("<td style=\"width:48%\"><div style=\"font-size:10pt;margin-bottom:4px\">Conduttore</div><div class=\"sign-box\"></div></td>\n");
        sb.Append("</tr></table>\n");
        sb.Append("<p style=\"font-size:9pt;color:#555;margin-top:12px\">Il conduttore dichiara di aver ricevuto il veicolo nelle condizioni indicate, di aver preso visione del contratto e di accettarne integralmente le condizioni.</p>\n");
        sb.Append("</div>\n");

        sb.Append($"<div class=\"footer\">{op} · Contratto generato da MyCars il {now}</div>\n");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string FuelLabel(string? level) => level switch
    {
        "full"           => "Pieno",
        "three_quarters" => "3/4",
        "half"           => "Metà",
        "quarter"        => "1/4",
        "empty"          => "Vuoto",
        _                => "—",
    };
}

// ── Request / Response DTO ────────────────────────────────────────────────────

public sealed record CreateRentalRequest(
    Guid     VehicleId,
    string   CustomerName,
    string?  CustomerPhone,
    string?  CustomerLicense,
    string?  CustomerFiscalCode,
    DateOnly StartDate,
    DateOnly PlannedEndDate,
    decimal? AgreedPrice,
    decimal? DepositAmount,
    string?  PaymentMethod,
    string?  Notes);

public sealed record UpdateRentalRequest(
    string   CustomerName,
    string?  CustomerPhone,
    string?  CustomerLicense,
    string?  CustomerFiscalCode,
    DateOnly StartDate,
    DateOnly PlannedEndDate,
    decimal? AgreedPrice,
    decimal? DepositAmount,
    bool     DepositReturned,
    string?  PaymentMethod,
    bool     IsPaid,
    string?  Notes);

public sealed record ActivateRentalRequest(
    int?    KmDeparture,
    string? FuelDeparture);

public sealed record CloseRentalRequest(
    DateOnly? ActualEndDate,
    int?      KmReturn,
    string?   FuelReturn);
