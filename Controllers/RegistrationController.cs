namespace MyCars.Controllers;

[ApiController]
[Route("api/register")]
public sealed class RegistrationController : ControllerBase
{
    private readonly IOperatorRegistrationRepository _registrations;
    private readonly ILogger<RegistrationController> _log;

    public RegistrationController(
        IOperatorRegistrationRepository registrations,
        ILogger<RegistrationController> log)
    {
        _registrations = registrations;
        _log           = log;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] RegistrationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.BusinessName) ||
            string.IsNullOrWhiteSpace(req.Email)        ||
            string.IsNullOrWhiteSpace(req.ContactPerson))
            return BadRequest(new { message = "Ragione sociale, email e nome referente sono obbligatori." });

        var reg = await _registrations.CreateAsync(new OperatorRegistration
        {
            BusinessName  = req.BusinessName.Trim(),
            VatNumber     = req.VatNumber?.Trim(),
            Email         = req.Email.Trim().ToLowerInvariant(),
            Phone         = req.Phone?.Trim(),
            ContactPerson = req.ContactPerson.Trim(),
            Address       = req.Address?.Trim(),
            City          = req.City?.Trim(),
            Province      = req.Province?.Trim(),
            Website       = req.Website?.Trim(),
            Notes         = req.Notes?.Trim(),
        });

        _log.LogInformation(
            "Nuova richiesta iscrizione: {BusinessName} ({Email})", reg.BusinessName, reg.Email);

        return Ok(new { id = reg.Id, message = "Richiesta inviata. Ti contatteremo appena verificati i dati." });
    }
}

public sealed class RegistrationRequest
{
    public string  BusinessName  { get; set; } = "";
    public string? VatNumber     { get; set; }
    public string  Email         { get; set; } = "";
    public string? Phone         { get; set; }
    public string  ContactPerson { get; set; } = "";
    public string? Address       { get; set; }
    public string? City          { get; set; }
    public string? Province      { get; set; }
    public string? Website       { get; set; }
    public string? Notes         { get; set; }
}
