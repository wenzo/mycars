using MyCars.Domain.Repositories;

namespace MyCars.Infrastructure.Push;

public sealed class PushSchedulerService : BackgroundService
{
    private readonly IServiceProvider           _sp;
    private readonly ILogger<PushSchedulerService> _log;

    public PushSchedulerService(IServiceProvider sp, ILogger<PushSchedulerService> log)
    {
        _sp  = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Prima esecuzione dopo 30 secondi dall'avvio
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try   { await ProcessAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "PushScheduler: errore ciclo"); }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope   = _sp.CreateScope();
        var repo    = scope.ServiceProvider.GetRequiredService<IScheduledPushRepository>();
        var push    = scope.ServiceProvider.GetRequiredService<IPushRepository>();
        var webPush = scope.ServiceProvider.GetRequiredService<IWebPushService>();

        if (!webPush.IsEnabled) return;

        var pending = await repo.GetPendingAsync();
        if (pending.Count == 0) return;

        _log.LogInformation("PushScheduler: {Count} notifiche da inviare", pending.Count);

        foreach (var n in pending)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var subs = await push.GetByTopicAsync(n.Topic, n.OperatorId);
                if (subs.Count > 0)
                    await webPush.SendAsync(subs, n.Title, n.Body, n.ImageUrl);
                await repo.MarkSentAsync(n.Id);
                _log.LogInformation("PushScheduler: notifica {Id} inviata a {Count} dispositivi", n.Id, subs.Count);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "PushScheduler: errore notifica {Id}", n.Id);
                await repo.MarkErrorAsync(n.Id, ex.Message);
            }
        }
    }
}
