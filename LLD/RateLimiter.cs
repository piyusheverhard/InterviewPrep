/*
 *
Problem 4: The Rate Limiter
--------------------------------------------------

The Scenario:
You are building an API Gateway. To prevent abuse and protect your downstream microservices, you need to implement a rate limiter.

The Requirements:

The rate limiter must enforce a limit of N requests per Time Window (T) per user. (e.g., maximum 5 requests per 1 minute).

It needs a core method: bool IsAllowed(string userId).

If the user has made fewer than N requests in the past T seconds, return true and record the request.

If the user has exceeded the limit, return false (meaning the API will return an HTTP 429 Too Many Requests).

The time window is a "sliding window." If the limit is 5 requests per 60 seconds, and I make 5 requests at 10:00:00, I am blocked. At exactly 10:01:01, my previous requests fall out of the 60-second window, and I should be allowed to make requests again.

*/

namespace RateLimiter;

public class RateLimiter
{

    // One approach could be storing all the timestamps of the active requests for each user in a queue
    // like 1 -> t1, t2, t3; 2 -> t2, t5
    // If I get request at t6 for user 1. I will do while queue.size == n and current time - queue.top > window dequeue.
    // if queue.size < n , allow this request and vice versa.
    // But this takes o(1) time complexity but O(N * M) space complexity where M is number of users and N is number of allowed request (could be a constant like 1000) 
    // per time window.
    //
    // Can we optimize the space here? 
    // I am thinking of a better approach where I give credits to users. I store the last time they made a request and credits left.
    // when i recieve a request I first recharge their credit and then take in their requests. but I don't think this maintains the sliding window thing.
    //
    // I can use a variation here maybe. I can store the first and last timestamp and number of requests in b/w them. I don't think this will work we kind of need
    // at least N timestamps so that we can evaluate things. I think an optimization could be dequeue till all the timestamps are in the current - timewindow window.


    // In real world scenario, we won't have fixed limits for every user. we will have this in some data store (DBs) but limits don't change frequently
    // so maybe we can use some caching to store the limits for users, so we don't query db much.

    private readonly IUserRepository _userRepository;

    // Assuming this is inmemory rate limiter. We should have some cleanup thread here which will remove old users which are out of ratelimit window.
    // Ideally this should contain data only fresh data (fresh meaning whatever is in now - timewindow)
    // Other options could be using a DB, which I think is a bad idea. A cache like redis could be an option where we use TTL for each entry and query
    // by userId to get count.
    private Dictionary<string, Queue<DateTime>> _userRequests;

    public RateLimiter(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _userRequests = new();
    }

    public async Task<bool> IsAllowedAsync(string userId)
    {
        var rateLimitForUser = await _userRepository.GetRateLimitForUserAsync(userId);
        var now = DateTime.UtcNow;

        if (_userRequests.ContainsKey(userId))
        {
            var expiredTimeWindow = DateTime.UtcNow.AddSeconds(-rateLimitForUser.TimeWindowInSeconds);
            while (_userRequests[userId].Count > 0 && _userRequests[userId].Peek() < expiredTimeWindow)
            {
                _userRequests[userId].Dequeue();
            }
            if (_userRequests[userId].Count < rateLimitForUser.NumberOfAllowRequests)
            {
                _userRequests[userId].Enqueue(now);
                return true;
            }
        }
        else
        {
            _userRequests.Add(userId, new());
            _userRequests[userId].Enqueue(now);
            return true;
        }

        return false;
    }
}


public interface IUserRepository
{
    public Task<UserRateLimit> GetRateLimitForUserAsync(string userId);
}

public record UserRateLimit
{
    public required string UserId;
    public int NumberOfAllowRequests;
    public int TimeWindowInSeconds;
}
