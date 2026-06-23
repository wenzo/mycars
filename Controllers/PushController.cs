using MyCars.Infrastructure.Push;

namespace MyCars.Controllers;

[ApiController]
[Route("api/push")]
public sealed class PushController : ControllerBase
{
    private readonly IPushRepository  _push;
    private readonly IWebPushService  _webPush;

    public PushController(IPushRepository push, IWebPushService webPush)
    {
        _push    = push;
        _webPush = webPush;
    }

    /// <summary>Configurazione pubblica necessaria al client per iscriversi.</summary>
    [HttpGet("config")]
    public IActionResult GetConfig() =>
        Ok(new { vapidPublicKey = _webPush.PublicKey });

    /// <summary>
    /// Registra (o aggiorna) una sottoscrizione Web Push VAPID.
    /// Chiamato dal frontend del sito dopo che l'utente concede il permesso.
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        var isAndroid = "android".Equals(req.DeviceType, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(req.Endpoint) ||
            (!isAndroid && (string.IsNullOrWhiteSpace(req.P256dh) || string.IsNullOrWhiteSpace(req.Auth))))
            return BadRequest(new { message = "Parametri mancanti." });

        await _push.UpsertAsync(new PushSubscription
        {
            Endpoint      = req.Endpoint.Trim(),
            P256dh        = req.P256dh?.Trim() ?? "",
            Auth          = req.Auth?.Trim()   ?? "",
            OperatorId    = req.OperatorId,
            VehicleId     = req.VehicleId,
            DeviceType    = req.DeviceType ?? "web",
            UserEmail     = string.IsNullOrWhiteSpace(req.UserEmail) ? null : req.UserEmail.Trim().ToLowerInvariant(),
            TopicGeneral  = req.TopicGeneral,
            TopicVehicles = req.TopicVehicles,
            TopicNews     = req.TopicNews,
        });

        return Ok(new { message = "ok" });
    }

    /// <summary>
    /// Endpoint di diagnostica: mostra le subscription in DB e invia una notifica di test.
    /// Usare solo per debug — rimuovere in produzione.
    /// GET /api/push/debug?operatorId=xxx
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("debug")]
    public async Task<IActionResult> Debug([FromQuery] Guid? operatorId)
    {
        var all  = await _push.GetAllAsync(operatorId);
        var results = new List<object>();

        foreach (var sub in all)
        {
            string status;
            try
            {
                var sent = await _webPush.SendAsync(
                    new[] { sub },
                    "Test EasyCars",
                    "Notifica di test inviata da /api/push/debug",
                    null);
                status = sent > 0 ? "ok (201)" : "skipped";
            }
            catch (Exception ex)
            {
                status = $"errore: {ex.Message}";
            }

            results.Add(new
            {
                endpoint    = sub.Endpoint[..Math.Min(60, sub.Endpoint.Length)],
                deviceType  = sub.DeviceType,
                operatorId  = sub.OperatorId,
                topicGeneral = sub.TopicGeneral,
                pushResult  = status,
            });
        }

        return Ok(new { total = all.Count, results });
    }

    /// <summary>Rimuove una sottoscrizione quando l'utente revoca il permesso.</summary>
    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Endpoint))
            return BadRequest();

        await _push.DeleteAsync(req.Endpoint.Trim());
        return NoContent();
    }
}

public sealed class SubscribeRequest
{
    public string  Endpoint   { get; set; } = "";
    public string  P256dh     { get; set; } = "";
    public string  Auth       { get; set; } = "";
    public Guid?   OperatorId { get; set; }
    public Guid?   VehicleId  { get; set; }
    public string? DeviceType    { get; set; }
    public string? UserEmail     { get; set; }
    public bool    TopicGeneral  { get; set; } = true;
    public bool    TopicVehicles { get; set; } = true;
    public bool    TopicNews     { get; set; } = true;
}

public sealed class UnsubscribeRequest
{
    public string Endpoint { get; set; } = "";
}
