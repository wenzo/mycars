using System.Text.Json;
using Lib.Net.Http.WebPush.Authentication;
using MyCars.Configuration;
using DomainSub  = MyCars.Domain.Models.PushSubscription;
using LibPushSub = Lib.Net.Http.WebPush.PushSubscription;
using LibPushMsg = Lib.Net.Http.WebPush.PushMessage;
using LibClient  = Lib.Net.Http.WebPush.PushServiceClient;

namespace MyCars.Infrastructure.Push;

public sealed class VapidWebPushService : IWebPushService
{
    private readonly LibClient                    _client;
    private readonly ILogger<VapidWebPushService> _log;

    public bool   IsEnabled => true;
    public string PublicKey { get; }

    public VapidWebPushService(
        IOptions<VapidOptions>        opts,
        ILogger<VapidWebPushService>  log)
    {
        var o = opts.Value;
        if (string.IsNullOrWhiteSpace(o.PublicKey) || string.IsNullOrWhiteSpace(o.PrivateKey))
            throw new InvalidOperationException(
                "Vapid:PublicKey e Vapid:PrivateKey non configurati.\n" +
                "Genera le chiavi con: dotnet run -- generate-vapid-keys\n" +
                "Poi imposta via User Secrets:\n" +
                "  dotnet user-secrets set \"Vapid:PublicKey\"  \"<chiave>\"\n" +
                "  dotnet user-secrets set \"Vapid:PrivateKey\" \"<chiave>\"");

        _client = new LibClient(new HttpClient())
        {
            DefaultAuthentication = new VapidAuthentication(o.PublicKey, o.PrivateKey)
            {
                Subject = string.IsNullOrWhiteSpace(o.Subject)
                    ? "mailto:admin@mycars.app"
                    : o.Subject,
            },
        };

        PublicKey = o.PublicKey;
        _log      = log;
    }

    public async Task<int> SendAsync(
        IReadOnlyList<DomainSub> subscriptions,
        string title, string body, string? iconUrl = null)
    {
        if (subscriptions.Count == 0) return 0;

        var payload = JsonSerializer.Serialize(new { title, body, icon = iconUrl });
        var message = new LibPushMsg(payload) { TimeToLive = 86_400 };
        var sent    = 0;

        foreach (var sub in subscriptions)
        {
            try
            {
                var libSub = new LibPushSub
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        ["auth"]   = sub.Auth,
                        ["p256dh"] = sub.P256dh,
                    },
                };
                await _client.RequestPushMessageDeliveryAsync(libSub, message);
                sent++;
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    "WebPush fallito per {Endpoint}: {Msg}",
                    sub.Endpoint[..Math.Min(50, sub.Endpoint.Length)],
                    ex.Message);
            }
        }
        return sent;
    }
}
