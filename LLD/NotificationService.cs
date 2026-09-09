/*
Problem Statement:

You are building a Notification System for your backend.

You need to support sending notifications via Email and SMS.

Creating an Email notification requires complex setup (SMTP host, port, credentials).

Creating an SMS notification requires different setup (Twilio API key, sender ID).

Because the creation logic for each is heavy and distinct, a Simple Factory (switch statement) will be too messy.

Task: Write the C# code using the Factory Method pattern to solve this.
Provide:

The Notification interface.

The concrete notification classes.

The abstract Creator (Factory) class.

The concrete Creator classes.
*/

public interface INotificatonService
{
    public Task NotifyUser(string userId, string message);
}

public interface INotificatonClientFactory
{
    public async Task<INotificatonClient> GetNotificatonClientForUserAsync(string userId);
}

public interface INotificatonClient
{
    public Task SendNotificaton(string userId, string message);
}

public class NotificationService : INotificationService
{
    private readonly INotificationClientFactory _notificationClientFactory;

    public Task<bool> NotifyUser(string userId, string message)
    {
        try
        {
            var notificationClient = _notificationClientFactory.GetNotificationClientForUser(userId);
            return notificationClient.SendNotification(userId, message);
        }
        catch (exception ex) {
            _logger.Log($"Failed to notify user {userId}, {ex}");
            return false;
        }
    }
}

public class NotificationClientFactory : INotificatonClientFactory
{
    private readonly IRepositoryFactory<User> _userRepositoryFactory;

    public NotificationClientFactory(IRepositoryFactor<User> userRepositoryFactory)
    {
        _userRepositoryFactory = userRepositoryFactory;
    }

    public async Task<INotificatonClient> GetNotificatonClientForUser(string userId) {
        var repository = _userRepositoryFactory.GetRepository();
        var user = repository.GetUserByUserId(userId);
        if (user == null)
        {
            throw new NotFoundException($"user with userId {userId} not found.");
        }
        user.Email is null ? new SmsNotificationClient() : new EmailNotificationClient();
    }
}

public class EmailNotificationClient : INotificatonClient
{
    private const int Port = 123;
    // These should come from vault or other secret store from appsettings.
    private const string Host = "myprovider";
    private readonly string _userName = "userName";
    private readonly string _password = "password";
    private readonly SmtpClient _smtpClient;
    private readonly IRepositoryFactory<User> _userRepositoryFactory;
    private readonly ILogger _logger;

    public EmailNotificationClient(IRepositoryFactory<User> userRepositoryFactory, ILogger logger)
    {
        _userRepositoryFactory = userRepositoryFactory;
        _logger = logger;
        _smtpClient = new SmtpClient();
    }
    public async Task<bool> SendNotification(string userId, sring message)
    {
        using var repository = _userRepositoryFactory.GetRepository();
        var user = await repository.GetUserByUserId(userId);
        if (user is null)
        {
            throw new NotFoundException($"user with {userId} doesn't exists.");
        }
        var userEmail = user.Email;
        try
        {
            await _smtpClient.ConnectAsync(Host, Port, _userName, _password);
            _client.SendAsync(userEmail, message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log($"Email send failed for user {userId}. Exception{ex}");
            return false;
        }
        finally
        {
            _smtpClient.DisconnectAsync();
        }
    }
}

public class SmsNotificationClient : INotificatonClient
{
    private const string ApiKeyHeaderName = "API_KEY";
    // These should come from vault or other secret store from appsettings.
    private readonly string _apiEndpoint = "www.sendsms.com";
    private readonly string _apiKey = "password";
    private readonly HttpClient _smsClient;
    private readonly IRepositoryFactory<User> _userRepositoryFactory;
    private readonly ILogger _logger;

    public SmsNotificationClient(IRepositoryFactory<User> userRepositoryFactory, ILogger logger)
    {
        _userRepositoryFactory = userRepositoryFactory;
        _logger = logger;
        _smsClient = new HttpClient(_apiEndpoint);
        _smsClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, _apiKey);
    }
    public async Task<bool> SendNotification(string userId, sring message)
    {
        using var repository = _userRepositoryFactory.GetRepository();
        var user = await repository.GetUserByUserId(userId);
        if (user is null)
        {
            throw new NotFoundException($"user with {userId} doesn't exists.");
        }
        var userPhoneNumber = user.PhoneNumber;
        try
        {
            await _smsClient.PostAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log($"SMS send failed for user {userId}. Exception{ex}");
            return false;
        }
    }
}
