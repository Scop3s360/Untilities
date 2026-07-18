using SaverSearch.Application.Common.Interfaces;

namespace SaverSearch.Infrastructure.Services.Notifications;

public class EmailNotificationService : INotificationService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Future-proof: Connect to SendGrid, SMTP, or AWS SES
        return Task.CompletedTask;
    }

    public Task SendPushNotificationAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default)
    {
        // Future-proof: FCM/APNS integration
        return Task.CompletedTask;
    }
}
