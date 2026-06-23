using System.Net;
using System.Net.Mail;

namespace MyCars.Controllers;

/// <summary>
/// Endpoint pubblici per l'app mobile. Non richiedono autenticazione.
/// Tutti i path sono prefissati con lo slug del concessionario.
/// </summary>
[ApiController]
[Route("api/public/{slug}")]
public sealed class PublicController : ControllerBase
{
    private readonly IOperatorRepository    _operators;
    private readonly IVehicleRepository     _vehicles;
    private readonly INewsRepository        _news;
    private readonly IBranchRepository      _branches;
    private readonly IDepartmentRepository  _departments;
    private readonly ILeadRepository        _leads;
    private readonly IEmailService          _email;
    private readonly SmtpOptions            _globalSmtp;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IOperatorRepository       operators,
        IVehicleRepository        vehicles,
        INewsRepository           news,
        IBranchRepository         branches,
        IDepartmentRepository     departments,
        ILeadRepository           leads,
        IEmailService             email,
        IOptions<SmtpOptions>     smtpOptions,
        ILogger<PublicController> logger)
    {
        _operators  = operators;
        _vehicles   = vehicles;
        _news       = news;
        _branches   = branches;
        _departments = departments;
        _leads      = leads;
        _email      = email;
        _globalSmtp = smtpOptions.Value;
        _logger     = logger;
    }

    private static string GenerateTrackingCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        return "NLG-" + new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private async Task TrySendTrackingEmailAsync(OperatorProfile op, string to, string customerName, string trackingCode)
    {
        var subject = $"Richiesta noleggio ricevuta — {op.BusinessName}";
        var html    = BuildTrackingEmail(op, customerName, trackingCode);

        try
        {
            if (!string.IsNullOrEmpty(op.SmtpHost))
            {
                using var client = new SmtpClient(op.SmtpHost, op.SmtpPort ?? 587)
                {
                    EnableSsl   = op.SmtpUseSsl,
                    Credentials = new NetworkCredential(op.SmtpUsername, op.SmtpPassword),
                };
                var fromAddress = op.SmtpFromEmail ?? op.SmtpUsername ?? _globalSmtp.FromEmail;
                using var msg = new MailMessage
                {
                    From       = new MailAddress(fromAddress!, op.SmtpFromName ?? op.BusinessName),
                    Subject    = subject,
                    Body       = html,
                    IsBodyHtml = true,
                };
                msg.To.Add(to);
                await client.SendMailAsync(msg);
            }
            else
            {
                await _email.SendAsync(to, subject, html);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile inviare email di conferma noleggio a {Email}", to);
        }
    }

    private static string BuildTrackingEmail(OperatorProfile op, string customerName, string code) => $"""
        <!DOCTYPE html>
        <html lang="it">
        <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width" /></head>
        <body style="margin:0;padding:0;background:#f4f6f9;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f9;padding:32px 16px;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);">
                <tr>
                  <td style="background:#1E3A5F;padding:24px 32px;">
                    <p style="margin:0;font-size:22px;font-weight:700;color:#fff;">{op.BusinessName}</p>
                    <p style="margin:4px 0 0;font-size:13px;color:rgba(255,255,255,.7);">Noleggio Veicoli</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;">
                    <p style="margin:0 0 8px;font-size:16px;color:#1a1a2e;">Ciao <strong>{customerName}</strong>,</p>
                    <p style="margin:0 0 24px;font-size:14px;color:#555;line-height:1.6;">
                      La tua richiesta di noleggio è stata ricevuta con successo.<br>
                      Ti contatteremo al più presto per confermare disponibilità e condizioni.
                    </p>

                    <p style="margin:0 0 8px;font-size:13px;font-weight:600;color:#888;text-transform:uppercase;letter-spacing:.06em;">Il tuo codice di tracciamento</p>
                    <div style="background:#f0f4ff;border:2px dashed #2E75B6;border-radius:10px;padding:20px;text-align:center;margin-bottom:24px;">
                      <span style="font-family:monospace;font-size:28px;font-weight:700;letter-spacing:6px;color:#1E3A5F;">{code}</span>
                    </div>
                    <p style="margin:0 0 24px;font-size:13px;color:#777;line-height:1.6;">
                      Conserva questo codice: puoi usarlo nell'app per verificare lo stato della tua richiesta in qualsiasi momento, anche se cambi dispositivo.
                    </p>

                    <hr style="border:none;border-top:1px solid #eee;margin:0 0 24px;" />

                    <table width="100%" cellpadding="0" cellspacing="0">
                      <tr>
                        <td style="font-size:13px;color:#555;">
                          <strong>{op.BusinessName}</strong><br>
                          {(op.Phone is not null ? $"Tel: {op.Phone}<br>" : "")}
                          {(op.Email is not null ? $"Email: {op.Email}<br>" : "")}
                          {(op.City  is not null ? $"{op.City}{(op.Province is not null ? $" ({op.Province})" : "")}" : "")}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 32px;background:#f8fafc;border-top:1px solid #eee;">
                    <p style="margin:0;font-size:11px;color:#aaa;text-align:center;">
                      Questa email è stata inviata automaticamente. Non rispondere a questa email.
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;


    private async Task<OperatorProfile?> ResolveAsync(string slug) =>
        await _operators.GetBySlugAsync(slug);

    // ── Veicoli ───────────────────────────────────────────────────────────────

    /// <summary>Lista veicoli pubblica con filtri e paginazione.</summary>
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles(
        string slug,
        [FromQuery] int      page           = 0,
        [FromQuery] int      pageSize       = 20,
        [FromQuery] string?  vehicleType    = null,
        [FromQuery] string?  condition      = null,
        [FromQuery] string?  fuel           = null,
        [FromQuery] bool?    prontaConsegna = null,
        [FromQuery] bool?    isNuovoArrivo  = null,
        [FromQuery] decimal? minPrice       = null,
        [FromQuery] decimal? maxPrice       = null,
        [FromQuery] int?     maxMileageKm   = null,
        [FromQuery] int?     minYear        = null,
        [FromQuery] int?     maxYear        = null,
        [FromQuery] Guid?    branchId           = null,
        [FromQuery] string?  search             = null,
        [FromQuery] string?  transmission       = null,
        [FromQuery] bool?    vatDeductible      = null,
        [FromQuery] bool?    handicapAccessible = null,
        [FromQuery] bool?    imported           = null,
        [FromQuery] bool?    forSale            = null,
        [FromQuery] bool?    forRental          = null)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        var filter = new VehicleFilter(
            vehicleType, condition, fuel,
            prontaConsegna, isNuovoArrivo,
            minPrice, maxPrice, maxMileageKm,
            minYear, maxYear, branchId,
            string.IsNullOrWhiteSpace(search)       ? null : search.Trim(),
            string.IsNullOrWhiteSpace(transmission) ? null : transmission.Trim(),
            vatDeductible, handicapAccessible, imported, forSale, forRental);

        var result = await _vehicles.GetPublicCardsAsync(
            op.Id,
            new PageRequest(page, Math.Clamp(pageSize, 1, 50)),
            filter);

        return Ok(result);
    }

    /// <summary>Scheda veicolo con galleria immagini.</summary>
    [HttpGet("vehicles/{id:guid}")]
    public async Task<IActionResult> GetVehicle(string slug, Guid id)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        var card = await _vehicles.GetCardByIdAsync(id, op.Id);
        if (card is null) return NotFound();

        var images = await _vehicles.GetImagesAsync(id, op.Id);

        return Ok(new { vehicle = card, images });
    }

    // ── News ──────────────────────────────────────────────────────────────────

    /// <summary>Lista news/comunicazioni pubblicate.</summary>
    [HttpGet("news")]
    public async Task<IActionResult> GetNews(
        string slug,
        [FromQuery] int     page     = 0,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? newsType = null)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        try
        {
            var result = await _news.GetPublishedAsync(
                op.Id,
                newsType,
                new PageRequest(page, Math.Clamp(pageSize, 1, 50)));

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    /// <summary>Dettaglio singola news.</summary>
    [HttpGet("news/{id:guid}")]
    public async Task<IActionResult> GetNewsById(string slug, Guid id)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        var item = await _news.GetByIdAsync(id, op.Id);
        if (item is null || !item.IsPublished) return NotFound();

        return Ok(item);
    }

    // ── Sedi ─────────────────────────────────────────────────────────────────

    /// <summary>Elenco sedi attive del concessionario.</summary>
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches(string slug)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        var items = await _branches.GetByOperatorAsync(op.Id);
        return Ok(items.Where(b => b.IsActive));
    }

    // ── Reparti ───────────────────────────────────────────────────────────────

    /// <summary>Elenco reparti attivi, opzionalmente filtrati per sede.</summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(
        string slug,
        [FromQuery] Guid? branchId = null)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        IReadOnlyList<Department> items = branchId.HasValue
            ? await _departments.GetByBranchAsync(branchId.Value, op.Id)
            : await _departments.GetByOperatorAsync(op.Id);

        return Ok(items.Where(d => d.IsActive));
    }

    // ── Privacy Policy ────────────────────────────────────────────────────────

    [HttpGet("privacy-policy")]
    public async Task<IActionResult> GetPrivacyPolicy(string slug)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();
        return Ok(new { html = op.PrivacyPolicyHtml ?? "" });
    }

    // ── Lead / Richieste ──────────────────────────────────────────────────────

    /// <summary>
    /// Invia una richiesta dal cliente.
    /// LeadType: "info" | "test_drive" | "price_update" | "contact"
    /// </summary>
    [HttpPost("leads")]
    public async Task<IActionResult> SubmitLead(string slug, [FromBody] PublicLeadRequest req)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { message = "Il nome è obbligatorio." });
        if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { message = "Email o telefono sono obbligatori." });
        if (!req.PrivacyAccepted)
            return BadRequest(new { message = "È necessario accettare la privacy policy." });

        var lead = new VehicleLead
        {
            OperatorId        = op.Id,
            VehicleId         = req.VehicleId,
            BranchId          = req.BranchId,
            FullName          = req.FullName.Trim(),
            Email             = req.Email?.Trim().ToLowerInvariant(),
            Phone             = req.Phone?.Trim(),
            Message           = req.Message?.Trim(),
            PrivacyAccepted   = true,
            MarketingAccepted = req.MarketingAccepted,
            Source            = "app",
            Status            = "new",
            LeadType          = req.LeadType ?? "info",
            PreferredDate     = req.PreferredDate,
            PreferredTime     = req.PreferredTime,
        };

        var trackingCode = (lead.LeadType == "rental") ? GenerateTrackingCode() : null;
        lead.TrackingCode = trackingCode;

        var created = await _leads.CreateAsync(lead);

        if (trackingCode is not null && !string.IsNullOrEmpty(created.Email))
            _ = TrySendTrackingEmailAsync(op, created.Email, created.FullName, trackingCode);

        return Ok(new { id = created.Id, trackingCode, message = "Richiesta inviata con successo." });
    }

    /// <summary>Stato di una richiesta noleggio tramite codice di tracciamento.</summary>
    [HttpGet("rental-requests/{code}")]
    public async Task<IActionResult> GetRentalRequestStatus(string slug, string code)
    {
        var op = await ResolveAsync(slug);
        if (op is null) return NotFound();

        if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
            return BadRequest(new { message = "Codice non valido." });

        var lead = await _leads.GetByTrackingCodeAsync(op.Id, code.ToUpperInvariant().Trim());
        if (lead is null) return NotFound(new { message = "Codice non trovato." });

        var statusLabel = lead.Status switch
        {
            "new"       => "In attesa di risposta",
            "contacted" => "Contattato",
            "closed"    => "Gestita",
            "lost"      => "Non disponibile",
            "spam"      => "Annullata",
            _           => lead.Status,
        };

        return Ok(new
        {
            trackingCode  = lead.TrackingCode,
            status        = lead.Status,
            statusLabel,
            preferredDate = lead.PreferredDate,
            createdAt     = lead.CreatedAt,
        });
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed class PublicLeadRequest
{
    public string    FullName          { get; set; } = "";
    public string?   Email             { get; set; }
    public string?   Phone             { get; set; }
    public string?   Message           { get; set; }
    public bool      PrivacyAccepted   { get; set; }
    public bool      MarketingAccepted { get; set; }
    public string?   LeadType          { get; set; }
    public Guid?     VehicleId         { get; set; }
    public Guid?     BranchId          { get; set; }
    public DateOnly? PreferredDate     { get; set; }
    public string?   PreferredTime     { get; set; }
}
