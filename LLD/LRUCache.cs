/*
 *
 *Problem 3: The LRU (Least Recently Used) Cache
This is the ultimate test of combining data structures to achieve strict performance constraints. Unlike the Parking Lot (which tests domain modeling), the LRU Cache is purely about algorithmic efficiency and memory management.

The Requirements:

You are building an in-memory cache component (like a miniature Redis).

The cache is initialized with a fixed, maximum Capacity (e.g., 3 items).

It must support a Get(string key) method:

If the key exists, return the value.

Accessing this key makes it the most recently used item.

If the key does not exist, return null or throw an exception.

It must support a Put(string key, string value) method:

If the key exists, update its value and mark it as the most recently used item.

If the key does not exist, insert it.

If inserting pushes the cache over its Capacity, you must immediately evict the least recently used item before inserting.

The Hard Constraint: Both Get and Put must execute in strict O(1) time complexity. You cannot use loops or LINQ (.OrderBy, .First, etc.) to find the oldest item./
*/

namespace LruCache;

public class LruCache
{
    private readonly int _capacity;

    private Dictionary<string, string> _cache;

    private Dictionary<string, int> _latestTimeStampForKey;

    private Queue<(string Key, int TimeStamp)> _recentQueue; // will fail -> issues: memory leak ever growing queue. We can get integer overflow for timestamp.

    // A better solution would be to use a doubly linked list. We store the pointers in the dictionary instead of values. Whenever anything is accessed we get the node pointer
    // from the dictionary and move it at the head of the linked list.


    private int _currentTimeStamp;

    public LruCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity should be greater than zero.", nameof(capacity));
        }
        _capacity = capacity;
        _cache = new();
        _latestTimeStampForKey = new();
        _recentQueue = new();
        _currentTimeStamp = 0;
    }

    public string? Get(string key)
    {
        if (!_cache.ContainsKey(key))
        {
            return null;
        }
        _recentQueue.Enqueue((key, _currentTimeStamp));
        _latestTimeStampForKey[key] = _currentTimeStamp++;
        return _cache[key];
    }

    public void Put(string key, string value)
    {
        if (_cache.ContainsKey(key))
        {
            _cache[key] = value;
            RecordTimeStampForKey(key);
        }
        else
        {
            _cache.Add(key, value);
            RecordTimeStampForKey(key);
            if (_cache.Count() > _capacity)
            {
                EvictLeastRecentKey();
            }
        }
    }

    private void RecordTimeStampForKey(string key)
    {
        _recentQueue.Enqueue((key, _currentTimeStamp));
        if (_latestTimeStampForKey.ContainsKey(key))
        {
            _latestTimeStampForKey[key] = _currentTimeStamp++;
        }
        else
        {
            _latestTimeStampForKey.Add(key, _currentTimeStamp++);
        }
    }

    private void EvictLeastRecentKey()
    {
        bool evicted = false;
        while (!evicted)
        {
            var item = _recentQueue.Dequeue();
            if (item.TimeStamp == _latestTimeStampForKey[item.Key])
            {
                _cache.Remove(item.Key);
                _latestTimeStampForKey.Remove(item.Key);
                evicted = true;
            }
        }
    }
}

