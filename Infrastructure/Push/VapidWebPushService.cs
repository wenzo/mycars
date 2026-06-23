using System.Text.Json;
using FirebaseAdmin.Messaging;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.DependencyInjection;
using MyCars.Configuration;
using MyCars.Domain.Repositories;
using DomainSub  = MyCars.Domain.Models.PushSubscription;
using LibPushSub = Lib.Net.Http.WebPush.PushSubscription;
using LibPushMsg = Lib.Net.Http.WebPush.PushMessage;
using LibClient  = Lib.Net.Http.WebPush.PushServiceClient;

namespace MyCars.Infrastructure.Push;

public sealed class VapidWebPushService : IWebPushService
{
    private readonly LibClient                    _client;
    private readonly IServiceScopeFactory         _scopeFactory;
    private readonly ILogger<VapidWebPushService> _log;

    public bool   IsEnabled => true;
    public string PublicKey { get; }

    // IServiceScopeFactory è thread-safe e può essere iniettato in un singleton.
    // Lo usiamo per creare uno scope breve quando dobbiamo rimuovere subscription scadute,
    // evitando il problema di catturare un servizio Scoped dentro un Singleton.
    public VapidWebPushService(
        IOptions<VapidOptions>        opts,
        IServiceScopeFactory          scopeFactory,
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

        PublicKey     = o.PublicKey;
        _scopeFactory = scopeFactory;
        _log          = log;
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
                if (string.Equals(sub.DeviceType, "android", StringComparison.OrdinalIgnoreCase))
                    await SendFcmAsync(sub, title, body, iconUrl);
                else
                    await SendVapidAsync(sub, message);
                sent++;
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    "Push fallito per {Endpoint}: {Msg}",
                    sub.Endpoint[..Math.Min(50, sub.Endpoint.Length)],
                    ex.Message);

                if (IsExpiredEndpoint(ex))
                    await TryDeleteSubscriptionAsync(sub.Endpoint);
            }
        }
        return sent;
    }

    private async Task SendVapidAsync(DomainSub sub, LibPushMsg message)
    {
        var libSub = new LibPushSub
        {
            Endpoint = sub.Endpoint,
            Keys = new Dictionary<string, string>
            {
                ["auth"]   = sub.Auth   ?? "",
                ["p256dh"] = sub.P256dh ?? "",
            },
        };
        await _client.RequestPushMessageDeliveryAsync(libSub, message);
    }

    private async Task SendFcmAsync(DomainSub sub, string title, string body, string? iconUrl)
    {
        var messaging = FirebaseMessaging.DefaultInstance;
        if (messaging is null)
        {
            _log.LogWarning("Firebase Admin non inizializzato — impossibile inviare FCM ad Android.");
            return;
        }

        var msg = new Message
        {
            Token = sub.Endpoint,
            Notification = new Notification { Title = title, Body = body },
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "default",
                    Sound     = "default",
                    ImageUrl  = iconUrl,
                },
            },
            Data = new Dictionary<string, string>
            {
                ["title"] = title,
                ["body"]  = body,
            },
        };

        _log.LogInformation(
            "FCM → invio a token ...{Token}, titolo='{Title}'",
            sub.Endpoint[^Math.Min(20, sub.Endpoint.Length)..], title);

        try
        {
            var msgId = await messaging.SendAsync(msg);
            _log.LogInformation("FCM → consegnato, messageId={MsgId}", msgId);
        }
        catch (FirebaseMessagingException ex)
            when (ex.MessagingErrorCode is MessagingErrorCode.Unregistered
                                        or MessagingErrorCode.InvalidArgument)
        {
            _log.LogWarning(
                "FCM → token non valido ({Code}): {Msg} — rimosso dal DB",
                ex.MessagingErrorCode, ex.Message);
            await TryDeleteSubscriptionAsync(sub.Endpoint);
        }
    }

    private async Task TryDeleteSubscriptionAsync(string endpoint)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var push = scope.ServiceProvider.GetRequiredService<IPushRepository>();
            await push.DeleteAsync(endpoint);
            _log.LogInformation("Rimossa subscription scaduta: {Endpoint}", endpoint[..Math.Min(50, endpoint.Length)]);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Pulizia subscription fallita: {Msg}", ex.Message);
        }
    }

    private static bool IsExpiredEndpoint(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("410") || msg.Contains("404") ||
               msg.Contains("Gone") || msg.Contains("Not Found") ||
               msg.Contains("expired") || msg.Contains("unsubscribed");
    }
}
