using Aihrly.Api.Data;
using Aihrly.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Services;

public sealed class NotificationWorker(
    NotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process notification for application {Id}", job.ApplicationId);
            }
        }
    }

    private async Task ProcessAsync(NotificationJob job)
    {
        // scoped db context, can't inject directly into a singleton-lifetime service
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation(
            "Sending {Type} notification for application {Id}",
            job.Type, job.ApplicationId);

        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            ApplicationId = job.ApplicationId,
            Type = job.Type,
            SentAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
