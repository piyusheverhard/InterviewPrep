using System;
using System.Collections.Generic;
using System.Threading;

namespace Concurrency.ThreadSafeLruCache;

public class ThreadSafeLruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;

    // Is fastest here because the thread gets blocked instantly
    // after checking a single bit in this object's header.
    private readonly object _lock;

    // Will be slower as it will try to throttle threads
    // and tries to maintain a counter internally to track the number of threads accessing
    // the block
    private readonly SemaphoreSlim _semaphoreSlim;

    // Is not really needed cause we need to take write lock in both get and put operations
    // see comment below
    private readonly ReaderWriterLock _readWriteLock;

    private LinkedList<CacheItem> _cacheItems;

    private Dictionary<TKey, LinkedListNode<CacheItem>> _nodeLookup;

    private readonly LockType _lockType;


    public ThreadSafeLruCache(int capacity, LockType lockType = LockType.LegacyLock)
    {
        _capacity = capacity;
        _cacheItems = new();
        _nodeLookup = new();
        _lock = new();
        _readWriteLock = new();
        _semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        _lockType = lockType;
    }

    public TValue Get(TKey key)
    {
        return _lockType switch
        {
            LockType.LegacyLock => GetWithLegacyLock(key),
            LockType.ReadWriteLock => GetWithReadWriteLock(key),
            LockType.SemaphoreSlim => GetWithSempahoreSlim(key),
            _ => GetWithLegacyLock(key)
        };
    }

    public void Put(TKey key, TValue value)
    {
        switch (_lockType)
        {
            case LockType.LegacyLock:
                PutWithLegacyLock(key, value);
                return;
            case LockType.ReadWriteLock:
                PutWithReadWriteLock(key, value);
                return;
            case LockType.SemaphoreSlim:
                PutWithSemaphoreSlim(key, value);
                return;
            default:
                PutWithLegacyLock(key, value);
                return;
        }
    }

    private TValue GetWithLegacyLock(TKey key)
    {
        lock (_lock)
        {
            if (!_nodeLookup.ContainsKey(key))
            {
                throw new KeyNotFoundException($"key doesn't exits {key}");
            }
            var node = _nodeLookup[key];
            _cacheItems.Remove(node);
            _cacheItems.AddFirst(node);
            return node.Value.Value;
        }
    }

    private void PutWithLegacyLock(TKey key, TValue value)
    {
        lock (_lock)
        {
            _nodeLookup.TryGetValue(key, out var node);
            if (node is null)
            {
                node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
            }
            else
            {
                _cacheItems.Remove(node);
            }
            _cacheItems.AddFirst(node);
            _nodeLookup[key] = node;
            if (_cacheItems.Count > _capacity)
            {
                EvictLeastRecentItem();
            }
        }
    }

    private TValue GetWithSempahoreSlim(TKey key)
    {
        _semaphoreSlim.Wait();
        try
        {
            if (!_nodeLookup.ContainsKey(key))
            {
                throw new KeyNotFoundException($"key doesn't exits {key}");
            }
            var node = _nodeLookup[key];
            _cacheItems.Remove(node);
            _cacheItems.AddFirst(node);
            return node.Value.Value;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private void PutWithSemaphoreSlim(TKey key, TValue value)
    {
        _semaphoreSlim.Wait();
        try
        {
            _nodeLookup.TryGetValue(key, out var node);
            if (node is null)
            {
                node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
            }
            else
            {
                _cacheItems.Remove(node);
            }
            _cacheItems.AddFirst(node);
            _nodeLookup[key] = node;
            if (_cacheItems.Count > _capacity)
            {
                EvictLeastRecentItem();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private TValue GetWithReadWriteLock(TKey key)
    {
        // NOTE: INCORRECT IMPLEMENTATION
        // This is wrong, we should acquire a write lock here because 
        // we are modifying the linked list. 
        _readWriteLock.AcquireReaderLock(100);
        try
        {
            if (!_nodeLookup.ContainsKey(key))
            {
                throw new KeyNotFoundException($"key doesn't exits {key}");
            }
            var node = _nodeLookup[key];
            _cacheItems.Remove(node);
            _cacheItems.AddFirst(node);
            return node.Value.Value;
        }
        finally
        {
            _readWriteLock.ReleaseReaderLock();
        }
    }

    private void PutWithReadWriteLock(TKey key, TValue value)
    {
        _readWriteLock.AcquireWriterLock(500);
        try
        {
            _nodeLookup.TryGetValue(key, out var node);
            if (node is null)
            {
                node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
            }
            else
            {
                _cacheItems.Remove(node);
            }
            _cacheItems.AddFirst(node);
            _nodeLookup[key] = node;
            if (_cacheItems.Count > _capacity)
            {
                EvictLeastRecentItem();
            }
        }
        finally
        {
            _readWriteLock.ReleaseWriterLock();
        }
    }

    private void EvictLeastRecentItem()
    {
        if (_cacheItems.Last is null) { return; }

        var oldestNode = _cacheItems.Last;
        _nodeLookup.Remove(oldestNode.Value.Key);
        _cacheItems.RemoveLast();
    }

    public enum LockType
    {
        LegacyLock,
        SemaphoreSlim,
        ReadWriteLock
    }

    private class CacheItem
    {
        public TKey Key;
        public TValue Value;

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}

public class ShardedLruCache<TKey, TValue> where TKey : notnull
{
    private readonly ThreadSafeLruCache<TKey, TValue>[] _shards;

    private readonly int _numShards;

    public ShardedLruCache(int totalCapacity, int numberOfShards = 32)
    {
        _shards = new ThreadSafeLruCache<TKey, TValue>[numberOfShards];
        _numShards = numberOfShards;

        for (var i = 0; i < totalCapacity % numberOfShards; i++)
        {
            _shards[i] = new ThreadSafeLruCache<TKey, TValue>(totalCapacity / numberOfShards + 1);
        }
        for (var i = totalCapacity % numberOfShards; i < numberOfShards; i++)
        {
            _shards[i] = new ThreadSafeLruCache<TKey, TValue>(totalCapacity / numberOfShards);
        }
    }

    public TValue Get(TKey key) => _shards[Math.Abs(key.GetHashCode()) % _numShards].Get(key);

    public void Put(TKey key, TValue value) => _shards[Math.Abs(key.GetHashCode()) % _numShards].Put(key, value);
}

