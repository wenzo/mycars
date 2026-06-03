using MyCars.Domain.Models;

namespace MyCars.Infrastructure.Push;

internal sealed class NullWebPushService : IWebPushService
{
    public bool   IsEnabled => false;
    public string PublicKey => "";

    public Task<int> SendAsync(
        IReadOnlyList<PushSubscription> subscriptions,
        string title, string body, string? iconUrl = null)
        => Task.FromResult(0);
}
