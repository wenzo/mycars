using MyCars.Infrastructure.Push;
using MyCars.Repositories.Postgres;
using MyCars.Repositories.Rest;

namespace MyCars.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IOperatorUserRepository     _users;
    private readonly IOperatorRepository         _operators;
    private readonly IVehicleRepository          _vehicles;
    private readonly ILeadRepository             _leads;
    private readonly INewsRepository             _news;
    private readonly IPushRepository             _push;
    private readonly IWebPushService             _webPush;
    private readonly IFileStorageService         _storage;
    private readonly IWebHostEnvironment         _env;
    private readonly ILogger<AdminController>    _log;
    private readonly IBranchRepository           _branches;
    private readonly IDepartmentRepository       _departments;
    private readonly IScheduledPushRepository    _scheduledPush;

    public AdminController(
        IOperatorUserRepository      users,
        IOperatorRepository          operators,
        IVehicleRepository           vehicles,
        ILeadRepository              leads,
        INewsRepository              news,
        IPushRepository              push,
        IWebPushService              webPush,
        IFileStorageService          storage,
        IWebHostEnvironment          env,
        ILogger<AdminController>     log,
        IBranchRepository            branches,
        IDepartmentRepository        departments,
        IScheduledPushRepository     scheduledPush)
    {
        _users         = users;
        _operators     = operators;
        _vehicles      = vehicles;
        _leads         = leads;
        _news          = news;
        _push          = push;
        _webPush       = webPush;
        _storage       = storage;
        _env           = env;
        _log           = log;
        _branches      = branches;
        _departments   = departments;
        _scheduledPush = scheduledPush;
    }

    private Guid GetOperatorId() =>
        Guid.Parse(User.FindFirstValue("operator_id")!);

    // ── Login ─────────────────────────────────────────────────────────────────

    [HttpPost("login")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Login([FromForm] LoginRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";

        OperatorUser? user = null;
        if (!string.IsNullOrWhiteSpace(req.Email) && !string.IsNullOrWhiteSpace(req.Password))
            user = await _users.GetByEmailAsync(req.Email.Trim().ToLowerInvariant());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            _log.LogWarning("Login fallito: email={Email} IP={IP}", req.Email, ip);
            return Unauthorized(new { message = "Credenziali non valide." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,                 user.Email),
            new(ClaimTypes.Role,                 "Admin"),
            new("operator_id",                   user.OperatorId.ToString()),
            new("user_id",                       user.Id.ToString()),
            new("full_name",                     user.FullName),
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });

        await _users.UpdateLastLoginAsync(user.Id);

        _log.LogInformation("Login admin: email={Email} operatorId={OperatorId} IP={IP}",
            user.Email, user.OperatorId, ip);
        return Ok(new { message = "ok" });
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/admin/login.html");
    }

    // ── Stats (dashboard) ─────────────────────────────────────────────────────

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats()
    {
        var opId = GetOperatorId();
        var veicoli     = await _vehicles.CountActiveAsync(opId);
        var nuoviArrivi = await _vehicles.CountNuoviArriviAsync(opId);
        var prontaConse = await _vehicles.CountProntaConsegnaAsync(opId);
        var leadAperti  = await _leads.CountOpenAsync(opId);
        var testDrive   = await _leads.CountTestDrivePendingAsync(opId);
        var news        = await _news.CountPublishedAsync(opId);

        return Ok(new
        {
            veicoli_attivi   = veicoli,
            nuovi_arrivi     = nuoviArrivi,
            pronta_consegna  = prontaConse,
            lead_aperti      = leadAperti,
            test_drive       = testDrive,
            news_pubblicate  = news,
        });
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpGet("profile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _operators.GetByIdAsync(GetOperatorId());
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var profile = await _operators.GetByIdAsync(GetOperatorId());
        if (profile is null) return NotFound();

        profile.BusinessName    = req.BusinessName?.Trim() ?? profile.BusinessName;
        profile.VatNumber       = req.VatNumber?.Trim();
        profile.FiscalCode      = req.FiscalCode?.Trim();
        profile.Phone           = req.Phone?.Trim();
        profile.Email           = req.Email?.Trim()?.ToLowerInvariant();
        profile.WebsiteUrl      = req.WebsiteUrl?.Trim();
        profile.WhatsappNumber  = req.WhatsappNumber?.Trim();
        profile.PrimaryColor    = req.PrimaryColor?.Trim();
        profile.SecondaryColor  = req.SecondaryColor?.Trim();
        profile.AccentColor     = req.AccentColor?.Trim();

        var updated = await _operators.UpdateAsync(profile);
        return updated is null ? StatusCode(500) : Ok(updated);
    }

    [HttpPost("profile/logo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File obbligatorio." });
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { message = "Il file non può superare 2 MB." });

        var opId = GetOperatorId();
        try
        {
            var url = await _storage.SaveAsync(file, $"operators/{opId}", "logo");
            var profile = await _operators.GetByIdAsync(opId);
            if (profile is not null)
            {
                if (!string.IsNullOrEmpty(profile.LogoUrl)) _storage.Delete(profile.LogoUrl);
                profile.LogoUrl = url;
                await _operators.UpdateAsync(profile);
            }
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("profile/cover")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadCover(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File obbligatorio." });
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Il file non può superare 5 MB." });

        var opId = GetOperatorId();
        try
        {
            var url = await _storage.SaveAsync(file, $"operators/{opId}", "cover");
            var profile = await _operators.GetByIdAsync(opId);
            if (profile is not null)
            {
                if (!string.IsNullOrEmpty(profile.CoverImageUrl)) _storage.Delete(profile.CoverImageUrl);
                profile.CoverImageUrl = url;
                await _operators.UpdateAsync(profile);
            }
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Recent leads ──────────────────────────────────────────────────────────

    [HttpGet("leads/recent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRecentLeads([FromQuery] int count = 5)
    {
        var leads = await _leads.GetRecentAsync(Math.Clamp(count, 1, 20), GetOperatorId());
        return Ok(leads);
    }

    // ── Recent vehicles ───────────────────────────────────────────────────────

    [HttpGet("vehicles/recent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRecentVehicles([FromQuery] int count = 5)
    {
        var vehicles = await _vehicles.GetRecentAsync(Math.Clamp(count, 1, 20), GetOperatorId());
        return Ok(vehicles);
    }

    // ── Vehicles list (paginated) ─────────────────────────────────────────────

    [HttpGet("vehicles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetVehicles(
        [FromQuery] int     page           = 0,
        [FromQuery] int     pageSize       = 20,
        [FromQuery] string? condition      = null,
        [FromQuery] bool?   isPublished    = null,
        [FromQuery] bool?   isNuovoArrivo  = null,
        [FromQuery] bool?   prontaConsegna = null)
    {
        var result = await _vehicles.GetAllAsync(
            GetOperatorId(),
            new PageRequest(page, Math.Clamp(pageSize, 1, 100)),
            condition, isPublished, isNuovoArrivo, prontaConsegna);
        return Ok(result);
    }

    // ── Leads list (paginated) ────────────────────────────────────────────────

    [HttpGet("leads")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLeads(
        [FromQuery] int     page     = 0,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? status   = null,
        [FromQuery] string? leadType = null)
    {
        var result = await _leads.GetAllAsync(
            GetOperatorId(),
            new PageRequest(page, Math.Clamp(pageSize, 1, 100)),
            status, leadType);
        return Ok(result);
    }

    // ── News list (paginated) ─────────────────────────────────────────────────

    [HttpGet("news")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetNews(
        [FromQuery] int     page        = 0,
        [FromQuery] int     pageSize    = 20,
        [FromQuery] string? newsType    = null,
        [FromQuery] bool?   isPublished = null)
    {
        var result = await _news.GetAllAsync(
            GetOperatorId(),
            new PageRequest(page, Math.Clamp(pageSize, 1, 100)),
            newsType, isPublished);
        return Ok(result);
    }

    // ── Vehicles: CRUD ───────────────────────────────────────────────────────

    [HttpGet("brands")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBrands()
        => Ok(await _vehicles.GetBrandsAsync());

    [HttpGet("vehicles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetVehicleById(Guid id)
    {
        var v = await _vehicles.GetByIdAsync(id, GetOperatorId());
        if (v is null) return NotFound();
        return Ok(v);
    }

    [HttpPost("vehicles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateVehicle([FromBody] VehicleUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Model))
            return BadRequest(new { message = "Il modello è obbligatorio." });
        if (string.IsNullOrWhiteSpace(req.BrandName))
            return BadRequest(new { message = "La marca è obbligatoria." });
        if (string.IsNullOrWhiteSpace(req.InternalCode))
            return BadRequest(new { message = "Il codice interno è obbligatorio." });
        if (req.BranchId == Guid.Empty)
            return BadRequest(new { message = "La sede è obbligatoria." });

        var vehicle = BuildVehicle(req, GetOperatorId());
        var created = await _vehicles.CreateAsync(vehicle, req.BrandName.Trim());
        return Ok(created);
    }

    [HttpPut("vehicles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] VehicleUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Model))
            return BadRequest(new { message = "Il modello è obbligatorio." });
        if (string.IsNullOrWhiteSpace(req.BrandName))
            return BadRequest(new { message = "La marca è obbligatoria." });
        if (req.BranchId == Guid.Empty)
            return BadRequest(new { message = "La sede è obbligatoria." });

        var opId     = GetOperatorId();
        var existing = await _vehicles.GetByIdAsync(id, opId);
        if (existing is null) return NotFound();

        existing.BranchId        = req.BranchId;
        existing.InternalCode    = req.InternalCode.Trim();
        existing.Targa           = req.Targa?.Trim().ToUpperInvariant();
        existing.VehicleType     = req.VehicleType;
        existing.Model           = req.Model.Trim();
        existing.Version         = req.Version;
        existing.Condition       = req.Condition;
        existing.Fuel            = string.IsNullOrEmpty(req.Fuel) ? null : req.Fuel;
        existing.Transmission    = string.IsNullOrEmpty(req.Transmission) ? null : req.Transmission;
        existing.HorsepowerCv    = req.HorsepowerCv;
        existing.RegistrationYear = req.RegistrationYear;
        existing.MileageKm       = req.MileageKm;
        existing.Color           = req.Color;
        existing.Price           = req.Price;
        existing.PreviousPrice   = req.PreviousPrice;
        existing.Negotiable      = req.Negotiable;
        existing.IsPublished     = req.IsPublished;
        existing.ProntaConsegna  = req.ProntaConsegna;
        existing.IsNuovoArrivo   = req.IsNuovoArrivo;
        existing.Description     = req.Description;

        var updated = await _vehicles.UpdateAsync(existing, req.BrandName.Trim());
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("vehicles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var ok = await _vehicles.DeleteAsync(id, GetOperatorId());
        if (!ok) return NotFound();
        return Ok(new { deleted = true });
    }

    private static Vehicle BuildVehicle(VehicleUpsertRequest req, Guid operatorId) => new()
    {
        OperatorId       = operatorId,
        BranchId         = req.BranchId,
        InternalCode     = req.InternalCode.Trim(),
        Targa            = req.Targa?.Trim().ToUpperInvariant(),
        VehicleType      = req.VehicleType,
        Model            = req.Model.Trim(),
        Version          = req.Version,
        Condition        = req.Condition,
        Fuel             = string.IsNullOrEmpty(req.Fuel) ? null : req.Fuel,
        Transmission     = string.IsNullOrEmpty(req.Transmission) ? null : req.Transmission,
        HorsepowerCv     = req.HorsepowerCv,
        RegistrationYear = req.RegistrationYear,
        MileageKm        = req.MileageKm,
        Color            = req.Color,
        Price            = req.Price,
        PreviousPrice    = req.PreviousPrice,
        Negotiable       = req.Negotiable,
        IsPublished      = req.IsPublished,
        PublishedAt      = req.IsPublished ? DateTimeOffset.UtcNow : null,
        ProntaConsegna   = req.ProntaConsegna,
        IsNuovoArrivo    = req.IsNuovoArrivo,
        Description      = req.Description,
    };

    // ── App Codes ─────────────────────────────────────────────────────────────

    [HttpGet("app-codes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAppCodes()
        => Ok(await _operators.GetAppCodesAsync(GetOperatorId()));

    [HttpPost("app-codes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAppCode([FromBody] CreateAppCodeRequest req)
    {
        var raw = req.Code.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(raw, @"^[A-Z0-9][A-Z0-9_-]{2,31}$"))
            return BadRequest(new { message = "Formato non valido: usa lettere maiuscole, numeri, _ e – (min 3, max 32 caratteri)." });

        var code = new AppCode
        {
            OperatorId = GetOperatorId(),
            Code       = raw,
            Label      = req.Label?.Trim(),
            IsPrimary  = false,
            IsActive   = true,
            ExpiresAt  = req.ExpiresAt,
            MaxUses    = req.MaxUses,
        };

        try
        {
            var created = await _operators.CreateAppCodeAsync(code);
            return Ok(created);
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate") || ex.Message.Contains("23505"))
        {
            return Conflict(new { message = "Questo codice esiste già." });
        }
    }

    [HttpDelete("app-codes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAppCode(Guid id)
    {
        var ok = await _operators.DeleteAppCodeAsync(id, GetOperatorId());
        if (!ok) return NotFound(new { message = "Codice non trovato o codice principale (non eliminabile)." });
        return Ok(new { deleted = true });
    }

    // ── Notifiche push pianificate per news ──────────────────────────────────

    [HttpPost("news/{id:guid}/notify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NotifyNews(Guid id, [FromBody] PushNotifyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { message = "Titolo e corpo sono obbligatori." });

        if (!_webPush.IsEnabled)
            return StatusCode(503, new { message = "VAPID non configurato. Attiva le notifiche push in Impostazioni." });

        var opId = GetOperatorId();

        if (req.SendAt.HasValue && req.SendAt.Value > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            await _scheduledPush.CreateAsync(new ScheduledPushNotification
            {
                OperatorId  = opId,
                NewsId      = id,
                Topic       = "news",
                Title       = req.Title.Trim(),
                Body        = req.Body.Trim(),
                ImageUrl    = req.ImageUrl,
                ScheduledAt = req.SendAt.Value,
            });
            return Ok(new { scheduled = true, sendAt = req.SendAt });
        }

        var subs = await _push.GetByTopicAsync("news", opId);
        if (subs.Count == 0)
            return Ok(new { sent = 0, total = 0, scheduled = false });

        var sent = await _webPush.SendAsync(subs, req.Title.Trim(), req.Body.Trim(), req.ImageUrl);
        return Ok(new { sent, total = subs.Count, scheduled = false });
    }

    // ── Notifiche push pianificate per veicoli ────────────────────────────────

    [HttpPost("vehicles/{id:guid}/notify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NotifyVehicle(Guid id, [FromBody] PushNotifyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { message = "Titolo e corpo sono obbligatori." });

        if (!_webPush.IsEnabled)
            return StatusCode(503, new { message = "VAPID non configurato. Attiva le notifiche push in Impostazioni." });

        var opId    = GetOperatorId();
        var vehicle = await _vehicles.GetByIdAsync(id, opId);
        if (vehicle is null) return NotFound();

        if (req.SendAt.HasValue && req.SendAt.Value > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            await _scheduledPush.CreateAsync(new ScheduledPushNotification
            {
                OperatorId  = opId,
                Topic       = "vehicles",
                Title       = req.Title.Trim(),
                Body        = req.Body.Trim(),
                ImageUrl    = req.ImageUrl,
                ScheduledAt = req.SendAt.Value,
            });
            return Ok(new { scheduled = true, sendAt = req.SendAt });
        }

        var subs = await _push.GetByTopicAsync("vehicles", opId);
        if (subs.Count == 0)
            return Ok(new { sent = 0, total = 0, scheduled = false });

        var sent = await _webPush.SendAsync(subs, req.Title.Trim(), req.Body.Trim(), req.ImageUrl);
        return Ok(new { sent, total = subs.Count, scheduled = false });
    }

    // ── Branches: CRUD ───────────────────────────────────────────────────────

    [HttpGet("branches")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBranches()
    {
        var items = await _branches.GetByOperatorAsync(GetOperatorId());
        return Ok(items);
    }

    [HttpPost("branches")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBranch([FromBody] BranchUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });

        var branch = new Branch
        {
            OperatorId     = GetOperatorId(),
            Name           = req.Name.Trim(),
            Slug           = Slugify(req.Name),
            LegalName      = req.LegalName,
            Address        = req.Address,
            ZipCode        = req.ZipCode,
            City           = req.City,
            Province       = req.Province,
            Phone          = req.Phone,
            Email          = req.Email,
            WhatsappNumber = req.WhatsappNumber,
            IsActive       = req.IsActive,
            SortOrder      = req.SortOrder,
        };
        var created = await _branches.CreateAsync(branch);
        return Ok(created);
    }

    [HttpPut("branches/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] BranchUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });

        var opId     = GetOperatorId();
        var existing = await _branches.GetByIdAsync(id, opId);
        if (existing is null) return NotFound();

        existing.Name           = req.Name.Trim();
        existing.LegalName      = req.LegalName;
        existing.Address        = req.Address;
        existing.ZipCode        = req.ZipCode;
        existing.City           = req.City;
        existing.Province       = req.Province;
        existing.Phone          = req.Phone;
        existing.Email          = req.Email;
        existing.WhatsappNumber = req.WhatsappNumber;
        existing.IsActive       = req.IsActive;
        existing.SortOrder      = req.SortOrder;

        var updated = await _branches.UpdateAsync(existing);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("branches/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        var ok = await _branches.DeleteAsync(id, GetOperatorId());
        if (!ok) return NotFound();
        return Ok(new { deleted = true });
    }

    // ── Departments: CRUD ─────────────────────────────────────────────────────

    [HttpGet("departments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDepartments([FromQuery] Guid? branchId = null)
    {
        var opId = GetOperatorId();
        IReadOnlyList<Department> items = branchId.HasValue
            ? await _departments.GetByBranchAsync(branchId.Value, opId)
            : await _departments.GetByOperatorAsync(opId);
        return Ok(items);
    }

    [HttpPost("departments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });

        var dept = new Department
        {
            OperatorId  = GetOperatorId(),
            BranchId    = req.BranchId,
            Name        = req.Name.Trim(),
            Description = req.Description,
            SortOrder   = req.SortOrder,
            IsActive    = req.IsActive,
        };
        var created = await _departments.CreateAsync(dept);
        return Ok(created);
    }

    [HttpPut("departments/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] DepartmentUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });

        var opId = GetOperatorId();
        var depts = await _departments.GetByOperatorAsync(opId);
        var existing = depts.FirstOrDefault(d => d.Id == id);
        if (existing is null) return NotFound();

        existing.Name        = req.Name.Trim();
        existing.Description = req.Description;
        existing.BranchId    = req.BranchId;
        existing.SortOrder   = req.SortOrder;
        existing.IsActive    = req.IsActive;

        var updated = await _departments.UpdateAsync(existing);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("departments/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var ok = await _departments.DeleteAsync(id, GetOperatorId());
        if (!ok) return NotFound();
        return Ok(new { deleted = true });
    }

    // ── News: singola + CRUD ──────────────────────────────────────────────────

    [HttpGet("news/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetNewsById(Guid id)
    {
        var item = await _news.GetByIdAsync(id, GetOperatorId());
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpPost("news")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateNews([FromBody] NewsUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Il titolo è obbligatorio." });

        var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Title) : req.Slug;
        var item = new NewsItem
        {
            OperatorId  = GetOperatorId(),
            NewsType    = req.NewsType ?? "generic",
            Code        = req.Code,
            Title       = req.Title.Trim(),
            Slug        = slug,
            Excerpt     = req.Excerpt,
            Body        = req.Body,
            LinkUrl     = req.LinkUrl,
            StartsAt    = req.StartsAt,
            ExpiresAt   = req.ExpiresAt,
            IsPublished = req.IsPublished,
            PublishedAt = req.IsPublished ? DateTimeOffset.UtcNow : null,
        };
        var created = await _news.CreateAsync(item);
        return CreatedAtAction(nameof(GetNewsById), new { id = created.Id }, created);
    }

    [HttpPut("news/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateNews(Guid id, [FromBody] NewsUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Il titolo è obbligatorio." });

        var opId     = GetOperatorId();
        var existing = await _news.GetByIdAsync(id, opId);
        if (existing is null) return NotFound();

        existing.NewsType    = req.NewsType ?? existing.NewsType;
        existing.Code        = req.Code;
        existing.Title       = req.Title.Trim();
        existing.Slug        = string.IsNullOrWhiteSpace(req.Slug) ? existing.Slug : req.Slug;
        existing.Excerpt     = req.Excerpt;
        existing.Body        = req.Body;
        existing.LinkUrl     = req.LinkUrl;
        existing.StartsAt    = req.StartsAt;
        existing.ExpiresAt   = req.ExpiresAt;
        if (req.IsPublished && !existing.IsPublished) existing.PublishedAt = DateTimeOffset.UtcNow;
        existing.IsPublished = req.IsPublished;

        var updated = await _news.UpdateAsync(existing);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("news/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteNews(Guid id)
    {
        var ok = await _news.DeleteAsync(id, GetOperatorId());
        if (!ok) return NotFound();
        return Ok(new { deleted = true });
    }

    private static string Slugify(string text)
    {
        var s = text.ToLowerInvariant().Trim();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\s-àáâèéêìíîòóôùúû]", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"-+", "-");
        return s.Trim('-');
    }

    // ── Push: stats ──────────────────────────────────────────────────────────

    [HttpGet("push/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPushStats()
    {
        var total = await _push.CountAsync(GetOperatorId());
        return Ok(new
        {
            total_subscribers = total,
            vapid_enabled     = _webPush.IsEnabled,
            vapid_public_key  = _webPush.PublicKey,
        });
    }

    // ── Push: VAPID public key (per il frontend del sito) ────────────────────

    [HttpGet("push/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
        => Ok(new { publicKey = _webPush.PublicKey });

    // ── Push: ricerca veicolo per targa ───────────────────────────────────────

    [HttpGet("vehicles/by-targa")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FindByTarga([FromQuery] string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BadRequest(new { message = "value obbligatorio." });

        var v = await _vehicles.FindByTargaAsync(value.Trim().ToUpperInvariant(), GetOperatorId());
        if (v is null) return NotFound(new { message = "Nessun veicolo trovato con questa targa." });

        return Ok(new
        {
            id           = v.Id,
            model        = v.Model,
            version      = v.Version,
            internalCode = v.InternalCode,
            targa        = v.Targa,
        });
    }

    // ── Push: send notification ───────────────────────────────────────────────

    [HttpPost("push/send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendPush([FromBody] SendPushRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { message = "title e body sono obbligatori." });

        if (!_webPush.IsEnabled)
            return StatusCode(503, new { message = "VAPID non configurato. Genera le chiavi con: dotnet run -- generate-vapid-keys" });

        var operatorId = GetOperatorId();

        IReadOnlyList<PushSubscription> subs;
        if (req.Target == "vehicle" && req.VehicleId.HasValue)
            subs = await _push.GetByVehicleAsync(req.VehicleId.Value);
        else if (req.Target == "email" && !string.IsNullOrWhiteSpace(req.UserEmail))
            subs = await _push.GetByEmailAsync(req.UserEmail.Trim().ToLowerInvariant(), operatorId);
        else
            subs = await _push.GetAllAsync(operatorId);

        if (subs.Count == 0)
            return Ok(new { sent = 0, total = 0, message = "Nessun dispositivo registrato per questo operatore." });

        var sent = await _webPush.SendAsync(subs, req.Title, req.Body, req.ImageUrl);
        return Ok(new { sent, total = subs.Count });
    }

    // ── Dev utility: genera hash BCrypt ───────────────────────────────────────
    // Solo ambiente Development. Non esporre in produzione.

    [HttpGet("hash")]
    public IActionResult GenerateHash([FromQuery] string? password)
    {
        if (!_env.IsDevelopment()) return NotFound();
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Parametro 'password' obbligatorio." });

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        return Ok(new
        {
            hash,
            command = $"dotnet user-secrets set \"Admin:PasswordHash\" \"{hash}\"",
        });
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed class LoginRequest
{
    public string? Email    { get; set; }
    public string? Password { get; set; }
}

public sealed class UpdateProfileRequest
{
    public string? BusinessName   { get; set; }
    public string? VatNumber      { get; set; }
    public string? FiscalCode     { get; set; }
    public string? Phone          { get; set; }
    public string? Email          { get; set; }
    public string? WebsiteUrl     { get; set; }
    public string? WhatsappNumber { get; set; }
    public string? PrimaryColor   { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor    { get; set; }
}

public sealed class CreateAppCodeRequest
{
    public string  Code      { get; set; } = "";
    public string? Label     { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int?    MaxUses   { get; set; }
}

public sealed class SendPushRequest
{
    public string  Title     { get; set; } = "";
    public string  Body      { get; set; } = "";
    public string? ImageUrl  { get; set; }
    public string  Target    { get; set; } = "all"; // "all" | "vehicle" | "email"
    public Guid?   VehicleId { get; set; }
    public string? UserEmail { get; set; }
}

public sealed class PushNotifyRequest
{
    public string          Title    { get; set; } = "";
    public string          Body     { get; set; } = "";
    public string?         ImageUrl { get; set; }
    public DateTimeOffset? SendAt   { get; set; }
}

public sealed class VehicleUpsertRequest
{
    public string   VehicleType      { get; set; } = "autovettura";
    public string   BrandName        { get; set; } = "";
    public Guid     BranchId         { get; set; }
    public string   InternalCode     { get; set; } = "";
    public string?  Targa            { get; set; }
    public string   Model            { get; set; } = "";
    public string?  Version          { get; set; }
    public string   Condition        { get; set; } = "usato";
    public string?  Fuel             { get; set; }
    public string?  Transmission     { get; set; }
    public int?     HorsepowerCv     { get; set; }
    public short?   RegistrationYear { get; set; }
    public int      MileageKm        { get; set; }
    public string?  Color            { get; set; }
    public decimal? Price            { get; set; }
    public decimal? PreviousPrice    { get; set; }
    public bool     Negotiable       { get; set; }
    public bool     IsPublished      { get; set; } = true;
    public bool     ProntaConsegna   { get; set; }
    public bool     IsNuovoArrivo    { get; set; }
    public string?  Description      { get; set; }
}

public sealed class BranchUpsertRequest
{
    public string  Name            { get; set; } = "";
    public string? LegalName       { get; set; }
    public string? Address         { get; set; }
    public string? ZipCode         { get; set; }
    public string? City            { get; set; }
    public string? Province        { get; set; }
    public string? Phone           { get; set; }
    public string? Email           { get; set; }
    public string? WhatsappNumber  { get; set; }
    public bool    IsActive        { get; set; } = true;
    public int     SortOrder       { get; set; }
}

public sealed class DepartmentUpsertRequest
{
    public string  Name        { get; set; } = "";
    public string? Description { get; set; }
    public Guid?   BranchId    { get; set; }
    public int     SortOrder   { get; set; }
    public bool    IsActive    { get; set; } = true;
}

public sealed class NewsUpsertRequest
{
    public string?          NewsType    { get; set; }
    public string?          Code        { get; set; }
    public string           Title       { get; set; } = "";
    public string?          Slug        { get; set; }
    public string?          Excerpt     { get; set; }
    public string?          Body        { get; set; }
    public string?          LinkUrl     { get; set; }
    public DateTimeOffset?  StartsAt    { get; set; }
    public DateTimeOffset?  ExpiresAt   { get; set; }
    public bool             IsPublished { get; set; }
}
