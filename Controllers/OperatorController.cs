namespace MyCars.Controllers;

/// <summary>
/// API pubblica usata dall'app mobile per agganciare un concessionario tramite codice.
/// Non richiede autenticazione.
/// </summary>
[ApiController]
[Route("api/operator")]
public sealed class OperatorController : ControllerBase
{
    private readonly IOperatorRepository _operators;

    public OperatorController(IOperatorRepository operators) => _operators = operators;

    /// <summary>
    /// Risolve un codice app e restituisce il profilo del concessionario.
    /// Incrementa il contatore utilizzi del codice.
    ///
    /// Esempio (app mobile): GET /api/operator/connect?code=PIRRO
    /// </summary>
    [HttpGet("connect")]
    public async Task<IActionResult> Connect([FromQuery] string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Parametro 'code' obbligatorio." });

        var profile = await _operators.ResolveCodeAsync(code.Trim().ToUpperInvariant());
        if (profile is null)
            return NotFound(new { message = "Codice non valido, scaduto o esaurito." });

        return Ok(new
        {
            operatorId     = profile.Id,
            businessName   = profile.BusinessName,
            publicCode     = profile.PublicCode,
            slug           = profile.Slug,
            phone          = profile.Phone,
            email          = profile.Email,
            websiteUrl     = profile.WebsiteUrl,
            whatsappNumber = profile.WhatsappNumber,
            primaryColor   = profile.PrimaryColor,
            secondaryColor = profile.SecondaryColor,
            accentColor    = profile.AccentColor,
            logoUrl        = profile.LogoUrl,
            coverImageUrl  = profile.CoverImageUrl,
        });
    }

    /// <summary>
    /// Restituisce il profilo pubblico di un operatore tramite slug.
    /// Usato dall'app per aggiornare i dati del concessionario già agganciato.
    /// </summary>
    [HttpGet("profile/{slug}")]
    public async Task<IActionResult> GetProfile(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "Slug obbligatorio." });

        var profile = await _operators.GetBySlugAsync(slug);
        if (profile is null) return NotFound(new { message = "Concessionario non trovato." });

        return Ok(new
        {
            operatorId     = profile.Id,
            businessName   = profile.BusinessName,
            publicCode     = profile.PublicCode,
            slug           = profile.Slug,
            phone          = profile.Phone,
            email          = profile.Email,
            websiteUrl     = profile.WebsiteUrl,
            whatsappNumber = profile.WhatsappNumber,
            primaryColor   = profile.PrimaryColor,
            secondaryColor = profile.SecondaryColor,
            accentColor    = profile.AccentColor,
            logoUrl        = profile.LogoUrl,
            coverImageUrl  = profile.CoverImageUrl,
        });
    }
}
