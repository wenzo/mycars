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

    public PublicController(
        IOperatorRepository   operators,
        IVehicleRepository    vehicles,
        INewsRepository       news,
        IBranchRepository     branches,
        IDepartmentRepository departments,
        ILeadRepository       leads)
    {
        _operators   = operators;
        _vehicles    = vehicles;
        _news        = news;
        _branches    = branches;
        _departments = departments;
        _leads       = leads;
    }

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

        var created = await _leads.CreateAsync(lead);
        return Ok(new { id = created.Id, message = "Richiesta inviata con successo." });
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
