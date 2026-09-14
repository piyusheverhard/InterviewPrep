namespace PartitionedKafkaWithRwLock;

public class Partition
{
    public int PartitionId { get; init; }
    
    // A standard List is NOT thread-safe, so it is protected by _lock
    private List<string> _messages;
    private Dictionary<string, int> _consumerOffset;
    private readonly object _lock = new();

    public Partition(int partitionId)
    {
        PartitionId = partitionId;
        _messages = new();
        _consumerOffset = new();
    }

    public void Publish(List<string> messages)
    {
        lock (_lock)
        {
            _messages.AddRange(messages); // Protected mutation
        }
    }

    public List<string> GetMessages(string consumerId, int maxNumberOfMessages)
    {
        lock (_lock)
        {
            if (!_consumerOffset.TryGetValue(consumerId, out var offset))
            {
                throw new InvalidOperationException($"Consumer {consumerId} is not subscribed to partition {PartitionId}");
            }
            var messages = new List<string>();
            for (int i = offset; i < _messages.Count && messages.Count < maxNumberOfMessages; i++)
            {
                messages.Add(_messages[i]); // Protected read
            }
            _consumerOffset[consumerId] = offset + messages.Count; // Protected offset increment
            return messages;
        }
    }

    public void AddConsumer(string consumerId)
    {
        lock (_lock)
        {
            // TryAdd checks if the key exists, and adds it if it doesn't.
            // Since it is inside lock(_lock), it is an atomic operation.
            if (!_consumerOffset.TryAdd(consumerId, 0))
            {
                Console.WriteLine($"consumer {consumerId} already added to partition {PartitionId}.");
            }
        }
    }
}

public class Topic
{
    public string TopicName { get; init; }
    
    // Notice: We NEVER lock this list! 
    // Why? Because after it is populated in the constructor, it is never mutated again.
    // A standard List is perfectly thread-safe for infinite readers as long as zero threads are writing to it.
    private readonly List<Partition> _partitions;

    public Topic(string name, int numPartition)
    {
        TopicName = name;
        _partitions = new();
        for (int i = 0; i < numPartition; i++)
        {
            _partitions.Add(new Partition(i));
        }
    }

    public void Publish(List<string> messages, string routingKey)
    {
        // Safe read from _partitions without a lock
        var partitionId = Math.Abs(routingKey.GetHashCode()) % _partitions.Count;
        _partitions[partitionId].Publish(messages);
    }

    public List<string> GetMessagesFromPartition(int partitionId, string consumerId, int maxNumberOfMessages = 1)
    {
        if (partitionId < 0 || partitionId >= _partitions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(partitionId), $"Partition {partitionId} does not exist in topic {TopicName}");
        }
        return _partitions[partitionId].GetMessages(consumerId, maxNumberOfMessages);
    }

    public void AddConsumer(string consumerId)
    {
        foreach (var partition in _partitions)
        {
            partition.AddConsumer(consumerId);
        }
    }
}

public class PartitionedKafka
{
    private Dictionary<string, Topic> _topics;
    private readonly ReaderWriterLockSlim _topicsLock = new();

    public PartitionedKafka()
    {
        _topics = new();
    }

    public void AddTopic(string name, int numPartition)
    {
        _topicsLock.EnterWriteLock();
        try
        {
            if (!_topics.TryAdd(name, new Topic(name, numPartition)))
            {
                Console.WriteLine($"topic {name} already exists.");
            }
        }
        finally
        {
            _topicsLock.ExitWriteLock();
        }
    }

    public void SubscribeConsumerToTopic(string consumerId, string topicName)
    {
        _topicsLock.EnterReadLock();
        try
        {
            if (_topics.TryGetValue(topicName, out var topic))
            {
                topic.AddConsumer(consumerId);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
            }
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }

    public List<string> ReadMessages(string consumerId, string topicName, int partitionId, int maxNumberOfMessages = 1)
    {
        _topicsLock.EnterReadLock();
        try
        {
            if (_topics.TryGetValue(topicName, out var topic))
            {
                return topic.GetMessagesFromPartition(partitionId, consumerId, maxNumberOfMessages);
            }
            
            throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }

    public void PublishMessages(string topicName, List<string> messages, string routingKey)
    {
        _topicsLock.EnterReadLock();
        try
        {
            if (_topics.TryGetValue(topicName, out var topic))
            {
                topic.Publish(messages, routingKey);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
            }
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }
}
