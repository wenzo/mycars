using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MyCars.Infrastructure.Email;

namespace MyCars.Controllers;

[ApiController]
[Route("api/superadmin")]
public sealed class SuperAdminController : ControllerBase
{
    private readonly SuperAdminOptions               _opts;
    private readonly IOperatorRegistrationRepository _registrations;
    private readonly IOperatorRepository             _operators;
    private readonly IOperatorUserRepository         _users;
    private readonly IEmailService                   _email;
    private readonly IVehicleRepository              _vehicles;
    private readonly ILogger<SuperAdminController>   _log;

    public SuperAdminController(
        IOptions<SuperAdminOptions>       opts,
        IOperatorRegistrationRepository   registrations,
        IOperatorRepository               operators,
        IOperatorUserRepository           users,
        IEmailService                     email,
        IVehicleRepository                vehicles,
        ILogger<SuperAdminController>     log)
    {
        _opts          = opts.Value;
        _registrations = registrations;
        _operators     = operators;
        _users         = users;
        _email         = email;
        _vehicles      = vehicles;
        _log           = log;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [HttpPost("login")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Login([FromForm] SuperAdminLoginRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";

        if (string.IsNullOrWhiteSpace(req.Username)
            || string.IsNullOrWhiteSpace(req.Password)
            || !string.Equals(req.Username.Trim(), _opts.Username, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(_opts.PasswordHash)
            || !BCrypt.Net.BCrypt.Verify(req.Password, _opts.PasswordHash))
        {
            _log.LogWarning("SuperAdmin login fallito: utente={User} IP={IP}", req.Username, ip);
            return Unauthorized(new { message = "Credenziali non valide." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _opts.Username),
            new(ClaimTypes.Role, "SuperAdmin"),
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });

        _log.LogInformation("SuperAdmin login: IP={IP}", ip);
        return Ok(new { message = "ok" });
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/accesso.html");
    }

    // ── Registrations list ────────────────────────────────────────────────────

    [HttpGet("registrations")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetRegistrations([FromQuery] string? status = null)
    {
        var list = await _registrations.GetAllAsync(status);
        return Ok(list);
    }

    // ── Registration detail ───────────────────────────────────────────────────

    [HttpGet("registrations/{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetRegistration(Guid id)
    {
        var reg = await _registrations.GetByIdAsync(id);
        return reg is null ? NotFound() : Ok(reg);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [HttpPost("registrations/{id:guid}/approve")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewRequest req)
    {
        var reg = await _registrations.GetByIdAsync(id);
        if (reg is null)          return NotFound();
        if (reg.Status != "pending") return Conflict(new { message = "La richiesta non è in stato pending." });

        // crea operatore
        var slug       = GenerateSlug(reg.BusinessName);
        var publicCode = GenerateCode(6);

        var profile = await _operators.CreateAsync(new OperatorProfile
        {
            BusinessName = reg.BusinessName,
            Slug         = slug,
            PublicCode   = publicCode,
            Email        = reg.Email,
            Phone        = reg.Phone,
            WebsiteUrl   = reg.Website,
        });

        // crea utente admin del concessionario
        var plainPassword = GeneratePassword(12);
        var user = await _users.CreateAsync(new OperatorUser
        {
            OperatorId    = profile.Id,
            Email         = reg.Email,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12),
            FullName      = reg.ContactPerson,
        });

        // segna registrazione come approvata
        await _registrations.UpdateStatusAsync(id, "approved", req.Notes);

        // invia email con credenziali
        var loginUrl = $"{Request.Scheme}://{Request.Host}/admin/login.html";
        await _email.SendAsync(
            reg.Email,
            "Accesso a MyCars Admin — Benvenuto!",
            BuildWelcomeEmail(reg.BusinessName, reg.ContactPerson, reg.Email, plainPassword, loginUrl));

        _log.LogInformation(
            "Operatore approvato: {BusinessName} ({OperatorId}) utente={UserId}",
            profile.BusinessName, profile.Id, user.Id);

        return Ok(new { operatorId = profile.Id, userId = user.Id });
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [HttpPost("registrations/{id:guid}/reject")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewRequest req)
    {
        var reg = await _registrations.GetByIdAsync(id);
        if (reg is null)          return NotFound();
        if (reg.Status != "pending") return Conflict(new { message = "La richiesta non è in stato pending." });

        await _registrations.UpdateStatusAsync(id, "rejected", req.Notes);

        _log.LogInformation("Registrazione rifiutata: {Id} ({BusinessName})", id, reg.BusinessName);
        return NoContent();
    }

    // ── Operators overview ────────────────────────────────────────────────────

    [HttpGet("operators")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetOperators()
    {
        var list = await _operators.GetAllAsync();
        return Ok(list);
    }

    [HttpPost("operators/{id:guid}/set-active")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest req)
    {
        var ok = await _operators.SetActiveAsync(id, req.IsActive);
        if (!ok) return NotFound();
        _log.LogInformation("Operatore {Id} {State}", id, req.IsActive ? "abilitato" : "disabilitato");
        return Ok(new { isActive = req.IsActive });
    }

    // ── Brand management ──────────────────────────────────────────────────────

    [HttpGet("brands")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetBrands()
        => Ok(await _vehicles.GetBrandsWithTypesAsync());

    [HttpPost("brands")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateBrand([FromBody] BrandUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });
        try
        {
            var brand = await _vehicles.CreateBrandAsync(req.Name.Trim(), req.VehicleTypes ?? []);
            return Ok(brand);
        }
        catch (Exception ex) when (
            ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("23505"))
        {
            return Conflict(new { message = "Esiste già un marchio con questo nome." });
        }
    }

    [HttpPut("brands/{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] BrandUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Il nome è obbligatorio." });
        var brand = await _vehicles.UpdateBrandAsync(id, req.Name.Trim(), req.VehicleTypes ?? []);
        if (brand is null) return NotFound();
        return Ok(brand);
    }

    [HttpDelete("brands/{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteBrand(Guid id)
    {
        var ok = await _vehicles.DeleteBrandAsync(id);
        if (!ok) return NotFound();
        return Ok(new { deleted = true });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateSlug(string name)
    {
        var s = name.ToLowerInvariant().Normalize();
        s = Regex.Replace(s, @"[àáâã]", "a");
        s = Regex.Replace(s, @"[èéêë]", "e");
        s = Regex.Replace(s, @"[ìíîï]", "i");
        s = Regex.Replace(s, @"[òóôõ]", "o");
        s = Regex.Replace(s, @"[ùúûü]", "u");
        s = Regex.Replace(s, @"[^a-z0-9]+", "-");
        return s.Trim('-');
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    private static string GeneratePassword(int length)
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    private static string BuildWelcomeEmail(
        string businessName, string contactPerson,
        string email, string password, string loginUrl) => $"""
        <!DOCTYPE html>
        <html lang="it">
        <body style="font-family:sans-serif;background:#f4f4f4;padding:32px">
          <div style="max-width:560px;margin:auto;background:#fff;border-radius:8px;padding:32px">
            <h2 style="color:#1a2b4a;margin-top:0">Benvenuto su MyCars, {contactPerson}!</h2>
            <p>La tua richiesta di iscrizione per <strong>{businessName}</strong> è stata approvata.</p>
            <p>Puoi ora accedere al pannello di gestione con le seguenti credenziali:</p>
            <table style="background:#f8f8f8;border-radius:6px;padding:16px;width:100%;margin:16px 0">
              <tr><td style="color:#666;padding:4px 8px">Email</td>
                  <td style="font-weight:600;padding:4px 8px">{email}</td></tr>
              <tr><td style="color:#666;padding:4px 8px">Password</td>
                  <td style="font-weight:600;padding:4px 8px;font-family:monospace">{password}</td></tr>
            </table>
            <p><a href="{loginUrl}" style="background:#c0392b;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block">Accedi al pannello</a></p>
            <p style="color:#888;font-size:13px;margin-top:24px">Ti consigliamo di cambiare la password al primo accesso.<br>Per assistenza rispondi a questa email.</p>
          </div>
        </body>
        </html>
        """;
}

public sealed class SuperAdminLoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class ReviewRequest
{
    public string? Notes { get; set; }
}

public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class BrandUpsertRequest
{
    public string    Name         { get; set; } = "";
    public string[]? VehicleTypes { get; set; }
}
