/*
 The Scenario:
  You are designing the backend for a ride-sharing app (like Uber). After a ride completes, the      
  system must send a receipt to the rider.
  
  Here are the business rules:
  
  1. If the user's profile has a verified phone number, send the receipt via SMS (using the Twilio   
  API).
  2. If they only have an email, send it via Email (using the SendGrid API).
  3. The Failure Condition: External APIs are flaky. If the Twilio or SendGrid API throws an         
  exception (e.g., HTTP 429 Rate Limit or 500 Server Error), we cannot drop the receipt. Instead, the
  system must fallback and publish the receipt payload to an internal Message Queue (RabbitMQ) so a  
  background worker can retry it later.
  
  Your Task:
  Using Clean Architecture and Dependency Inversion, design the C# architecture for this             
  ReceiptDispatcher service.
  
  I don't need the full functional code. Just provide:
  
  1. The Interfaces (Ports) you would define in your core domain.
  2. The Core Service Class showing its constructor and dependencies.
  3. A brief explanation of how you would structure the fallback logic without leaking Twilio or     
  RabbitMQ specific code into your core domain.
  */

namespace ReceiptDispatcher;

public interface IUserRepository
{
    public Task<UserProfile> GetUserProfileByUserId(string userId);
}

public record UserProfile
{
    public required string UserId;
    public string? PhoneNumber;
    public required string Email;
}

public record Receipt
{
    public required string UserName;
    public required string RideId;
    public required double Fare;
}

public interface INotificationClient
{
    public Task<bool> NotifyClient(string notificationAddress, string payload);
}

public interface ISmsNotificationClient : INotificationClient
{
    public Task<bool> NotifyClient(string phoneNumber, string payload);
}

public interface IEmailNotificationClient : INotificationClient
{
    public Task<bool> NotifyClient(string emailAddress, string payload);
}

public interface IEventPublisher
{
    public Task<bool> PublishAsync(string eventName, string payload);
}

public class ReceiptDispatcher
{
    private readonly INotificationClient _smsClient;
    private readonly INotificationClient _emailClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUserRepository _userRepository;

    public ReceiptDispatcher(ISmsNotificationClient smsClient, IEmailNotificationClient emailClient, IEventPublisher eventPublisher, IUserRepository userRepository)
    {
        _smsClient = smsClient;
        _emailClient = emailClient;
        _eventPublisher = eventPublisher;
        _userRepository = userRepository;
    }

    public async Task<bool> DispatchReceipt(string userId, Receipt receipt)
    {
        var userProfile = await _userRepository.GetUserProfileByUserId(userId);
        if (userProfile == null)
        {
            Console.WriteLine($"User profile not found for {userId}");
            return false;
        }
        try
        {
            var notified = false;
            if (!string.IsNullOrEmpty(userProfile.PhoneNumber))
            {
                notified = await TryNotifyUserBySms(userProfile.PhoneNumber, receipt);
            }
            if (!notified)
            {
                notified = await TryNotifyUserByEmail(userProfile.Email, receipt);
            }
            if (!notified)
            {
                notified = await _eventPublisher.PublishAsync("send-receipt", receipt.ToString());
            }
            return notified;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send receipt to user {userId}, ex: {ex}");
            return false;
        }
    }

    private async Task<bool> TryNotifyUserBySms(string phoneNumber, Receipt receipt)
    {
        var notified = false;
        try
        {
            notified = await _smsClient.NotifyClient(phoneNumber, receipt.ToString());
            return notified;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send receipt to phone number {phoneNumber}, ex: {ex}");
            return false;
        }
    }

    private async Task<bool> TryNotifyUserByEmail(string email, Receipt receipt)
    {
        var notified = false;
        try
        {
            notified = await _emailClient.NotifyClient(email, receipt.ToString());
            return notified;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send receipt to email {email}, ex: {ex}");
            return false;
        }
    }
}
