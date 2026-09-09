public enum NotificationType { Email, Sms }

public interface INotificationService
{
    Task<bool> NotifyUserAsync(string userId, string message);
}

public interface INotificationClient
{
    // The client only cares about WHERE to send it and WHAT to send.
    Task<bool> SendAsync(string destination, string message);
}

public interface INotificationClientFactory
{
    INotificationClient GetClient(NotificationType type);
}

public class EmailNotificationClient : INotificationClient
{
    private readonly ILogger _logger;
    private readonly SmtpClient _smtpClient;

    public EmailNotificationClient(ILogger logger)
    {
        _logger = logger;
        _smtpClient = new SmtpClient("myprovider", 123);
    }

    public async Task<bool> SendAsync(string emailAddress, string message)
    {
        try
        {
            // Note: In real code, we'd set credentials and use MailMessage
            await _smtpClient.SendMailAsync("noreply@us.com", emailAddress, "Subject", message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Email send failed to {emailAddress}");
            return false;
        }
    }
}

public class SmsNotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public SmsNotificationClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string phoneNumber, string message)
    {
        try
        {
            // Call Twilio or SMS provider
            await _httpClient.PostAsync($"/send?to={phoneNumber}&msg={message}", null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SMS send failed to {phoneNumber}");
            return false;
        }
    }
}

public class NotificationClientFactory : INotificationClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public INotificationClient GetClient(NotificationType type)
    {
        // The Factory decides WHICH class to pull from the DI container
        return type switch
        {
            NotificationType.Email => _serviceProvider.GetRequiredService<EmailNotificationClient>(),
            NotificationType.Sms => _serviceProvider.GetRequiredService<SmsNotificationClient>(),
            _ => throw new NotSupportedException($"Notification type {type} is not supported.")
        };
    }
}

public class NotificationService : INotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationClientFactory _factory;
    private readonly ILogger _logger;

    public NotificationService(
        IUserRepository userRepository,
        INotificationClientFactory factory,
        ILogger logger)
    {
        _userRepository = userRepository;
        _factory = factory;
        _logger = logger;
    }

    public async Task<bool> NotifyUserAsync(string userId, string message)
    {
        // 1. Fetch data ONCE
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        // 2. Business Logic: Determine type and destination
        NotificationType type = user.Email != null ? NotificationType.Email : NotificationType.Sms;
        string destination = type == NotificationType.Email ? user.Email : user.PhoneNumber;

        // 3. Factory gives us the right Strategy
        INotificationClient client = _factory.GetClient(type);

        // 4. Execute
        return await client.SendAsync(destination, message);
    }
}
