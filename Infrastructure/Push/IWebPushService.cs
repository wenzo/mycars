using MyCars.Domain.Models;

namespace MyCars.Infrastructure.Push;

public interface IWebPushService
{
    bool   IsEnabled { get; }
    string PublicKey { get; }

    Task<int> SendAsync(
        IReadOnlyList<PushSubscription> subscriptions,
        string                          title,
        string                          body,
        string?                         iconUrl = null);
}
