namespace SaverSearch.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendPushNotificationAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
}
