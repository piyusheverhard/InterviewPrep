/*
 *The Scenario:
  You are building the backend for a SaaS application. You integrate with Stripe for subscriptions.  
  When a user pays, Stripe sends a Webhook to your API: POST /api/webhooks/stripe.
  Because of the unreliable network, Stripe explicitly states they use "At-Least-Once Delivery". If  
  they don't receive a 200 OK from your API within 5 seconds, they will retry the webhook. In extreme
  cases, they might send the exact same webhook payload 3 times concurrently.
  
  The payload looks like this:
  
    {
      "eventId": "evt_9b1deb4d3b7d",
      "userId": "user_123",
      "plan": "Premium"
    }
  
  Your Task:
  Design the C# Application Service method that processes this webhook.
  Write the pseudo-code for public async Task ProcessStripeWebhook(string eventId, string userId).   
  Show exactly how you orchestrate the database (or Redis) atomic locks and the business logic       
  (user.UpgradeToPremium()) to ensure that even if 3 identical webhooks arrive at the exact same     
  millisecond, the user's account is only upgraded once, and your API doesn't throw unhandled 500    
  errors back to Stripe.
  
 */

using System.Text.Json;

namespace PaymentProcessor;

public class EventPayload
{
    public string EventId { get; init; }
    public string UserId { get; init; }
    public UserPlan Plan { get; init; }

    public EventPayload(string eventId, string userId, UserPlan plan)
    {
        EventId = eventId;
        UserId = userId;
        Plan = plan;
    }
}

public enum EventType
{
    UserPlanUpgrade,
}

public enum EventStatus
{
    Processing,
    Processed,
    Failed
}

public class EventLog
{
    public string EventId;
    public EventStatus Status;
    // Assuming we receive other kinds of events,
    // we store the json payload for them, this will help us
    // in (de)serialize to the exact type of payload.
    public EventType EventType;
    // the json payload
    public string Payload;

    public EventLog(string eventId, EventStatus status, EventType eventType, string payload)
    {
        EventId = eventId;
        Status = status;
        EventType = eventType;
        Payload = payload;
    }
}

public interface IDatabase
{
    // Atomically tries to insert an eventlog with processing status in events table.
    // if a unique key constraint is thrown, means event alread exists.
    // if the event already exists, returns the existing event.
    // isNew is true if this event was inserted else false.
    public Task<(EventLog eventLog, bool isNew)> GetOrInsertEventAsync(EventLog eventLog);
    public Task UpdateUserPlan(string userId, UserPlan plan);
    // Moves an event log to processing state only if it's not in Processing/ Processed.
    public Task<bool> AcquireProcessingLockAsync(string eventId);
    public Task SetEventStatus(string eventId, EventStatus status);
}

public interface ICache { }

public enum UserPlan
{
    Basic,
    Premium,
}

public class User
{
    public string UserId { get; init; }
    public UserPlan UserPlan { get; set; }

    public User(string userId, UserPlan plan)
    {
        UserId = userId;
        UserPlan = plan;
    }
}

public class PaymentProcessor
{
    private readonly IDatabase _database;

    public PaymentProcessor(IDatabase database)
    {
        _database = database;
    }

    public async Task<bool> ProcessStripeUpgradeUserWebhook(EventPayload payload)
    {
        try
        {
            var eventLog = new EventLog(payload.EventId, EventStatus.Processing, EventType.UserPlanUpgrade, JsonSerializer.Serialize(payload)!);

            var result = await _database.GetOrInsertEventAsync(eventLog);
            if (result.isNew)
            {
                await _database.UpdateUserPlan(payload.UserId, UserPlan.Premium);
                await SetProcessingResult(result.eventLog.EventId, EventStatus.Processed);
            }
            else if (result.eventLog.Status == EventStatus.Failed)
            {
                var isLocked = await _database.AcquireProcessingLockAsync(payload.EventId);
                if (isLocked)
                {
                    await _database.UpdateUserPlan(payload.UserId, UserPlan.Premium);
                    await SetProcessingResult(result.eventLog.EventId, EventStatus.Processed);
                }
            }
            return result.eventLog.Status == EventStatus.Processed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process event {payload.EventId}, exception: {ex}");
            await TrySetProcessingResult(payload.EventId, EventStatus.Failed);
            return false;
        }
    }

    private async Task SetProcessingResult(string eventId, EventStatus status)
    {
        await _database.SetEventStatus(eventId, status);
    }

    private async Task<bool> TrySetProcessingResult(string eventId, EventStatus status)
    {
        try
        {
            await SetProcessingResult(eventId, status);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write status for eventlog, eventId: {eventId}, status: {status}, exception: {ex}");
            return false;
        }
    }

}
