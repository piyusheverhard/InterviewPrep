namespace SimpleKafkaWithRwLock;

public class Topic
{
    public string TopicName { get; init; }
    
    // A standard List is NOT thread-safe, so all access to it MUST be wrapped in a lock.
    private List<string> Messages { get; init; }
    private readonly object _lock = new();

    public Topic(string name)
    {
        TopicName = name;
        Messages = new();
    }

    public void Publish(List<string> messages)
    {
        // We lock before modifying the list
        lock (_lock)
        {
            Messages.AddRange(messages);
        }
    }

    public List<string> GetMessagesFromOffSet(int offset, int maxNumberOfMessages = 1)
    {
        // We lock before reading from the list to prevent reading while a publish is happening
        lock (_lock)
        {
            var result = new List<string>();
            for (int i = offset; i < Messages.Count && result.Count < maxNumberOfMessages; i++)
            {
                result.Add(Messages[i]);
            }
            return result;
        }
    }
}

public class ConsumerGroup
{
    public string ConsumerGroupId { get; init; }
    
    private Dictionary<string, int> _subscribedTopicsWithOffset;
    
    // CHANGED: We replaced ReaderWriterLockSlim with a simple object lock.
    // Why? Because both Subscribe() and ReadMessages() mutate state (adding keys or incrementing offsets).
    // Since we never have a purely "read-only" operation here, a ReadWrite lock provides zero benefit.
    private readonly object _lock = new();

    public ConsumerGroup(string id)
    {
        ConsumerGroupId = id;
        _subscribedTopicsWithOffset = new();
    }

    public void Subscribe(string topic)
    {
        lock (_lock)
        {
            // TryAdd checks if the key exists, and if not, adds it. 
            // Because it is wrapped inside this lock block, this Check-And-Add operation is 100% thread-safe.
            if (!_subscribedTopicsWithOffset.TryAdd(topic, 0))
            {
                Console.WriteLine($"topic {topic} is already subscribed for consumer {ConsumerGroupId}");
            }
        }
    }

    public List<string> ReadMessages(Topic topic, int maxNumberOfMessages)
    {
        lock (_lock)
        {
            if (!_subscribedTopicsWithOffset.TryGetValue(topic.TopicName, out var currentOffset))
            {
                throw new ArgumentOutOfRangeException(nameof(topic), $"Not subscribed to topic {topic.TopicName}");
            }
            
            var messages = topic.GetMessagesFromOffSet(currentOffset, maxNumberOfMessages);
            _subscribedTopicsWithOffset[topic.TopicName] = currentOffset + messages.Count;
            return messages;
        }
    }
}

public class Simplekafka
{
    private Dictionary<string, Topic> _topics;
    private Dictionary<string, ConsumerGroup> _consumers;

    // The explicit ReaderWriterLocks required by the interviewer
    private readonly ReaderWriterLockSlim _topicsLock = new();
    private readonly ReaderWriterLockSlim _consumersLock = new();

    public Simplekafka()
    {
        _topics = new();
        _consumers = new();
    }

    public void AddTopic(string name)
    {
        _topicsLock.EnterWriteLock();
        try
        {
            if (!_topics.TryAdd(name, new Topic(name)))
            {
                Console.WriteLine($"topic {name} already exists.");
            }
        }
        finally
        {
            _topicsLock.ExitWriteLock();
        }
    }

    public void AddConsumer(string id)
    {
        _consumersLock.EnterWriteLock();
        try
        {
            if (!_consumers.TryAdd(id, new ConsumerGroup(id)))
            {
                Console.WriteLine($"consumer {id} already exists.");
            }
        }
        finally
        {
            _consumersLock.ExitWriteLock();
        }
    }

    public void SubscribeConsumerToTopic(string consumerId, string topicName)
    {
        // SAFE NESTED LOCKING: Every lock acquisition must have its own try/finally block!
        _topicsLock.EnterReadLock();
        try
        {
            _consumersLock.EnterReadLock();
            try
            {
                if (!_topics.ContainsKey(topicName)) throw new ArgumentOutOfRangeException(nameof(topicName));
                if (!_consumers.TryGetValue(consumerId, out var consumer)) throw new ArgumentOutOfRangeException(nameof(consumerId));
                
                consumer.Subscribe(topicName);
            }
            finally
            {
                _consumersLock.ExitReadLock();
            }
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }

    public List<string> ReadMessages(string consumerId, string topicName, int maxNumberOfMessages = 1)
    {
        _topicsLock.EnterReadLock();
        try
        {
            _consumersLock.EnterReadLock();
            try
            {
                if (!_topics.TryGetValue(topicName, out var topic)) throw new ArgumentOutOfRangeException(nameof(topicName));
                if (!_consumers.TryGetValue(consumerId, out var consumer)) throw new ArgumentOutOfRangeException(nameof(consumerId));

                // The consumer group handles its own internal locking for the offset increment
                return consumer.ReadMessages(topic, maxNumberOfMessages);
            }
            finally
            {
                _consumersLock.ExitReadLock();
            }
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }

    public void PublishMessages(string topicName, List<string> messages)
    {
        _topicsLock.EnterReadLock();
        try
        {
            if (_topics.TryGetValue(topicName, out var topic))
            {
                topic.Publish(messages);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(topicName));
            }
        }
        finally
        {
            _topicsLock.ExitReadLock();
        }
    }
}
