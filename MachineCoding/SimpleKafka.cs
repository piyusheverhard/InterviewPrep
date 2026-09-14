using System.Collections.Concurrent;

namespace SimpleKafka;

public class Topic
{
    public string TopicName { get; init; }
    private List<string> Messages { get; init; }
    private readonly object _lock = new();

    public Topic(string name)
    {
        TopicName = name;
        Messages = new();
    }

    public void Publish(List<string> messages)
    {
        lock (_lock)
        {
            Messages.AddRange(messages);
        }
    }

    public List<string> GetMessagesFromOffSet(int offset, int maxNumberOfMessages = 1)
    {
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
    private ConcurrentDictionary<string, int> _subscribedTopicsWithOffset;
    private readonly object _lock = new();

    public ConsumerGroup(string id)
    {
        ConsumerGroupId = id;
        _subscribedTopicsWithOffset = new();
    }

    public void Subscribe(string topic)
    {
        if (!_subscribedTopicsWithOffset.TryAdd(topic, 0))
        {
            Console.WriteLine($"topic {topic} is already subscribed for consumer {ConsumerGroupId}");
        }
    }

    // This method securely handles reading messages and incrementing the offset atomically per ConsumerGroup
    public List<string> ReadMessages(Topic topic, int maxNumberOfMessages)
    {
        if (!_subscribedTopicsWithOffset.ContainsKey(topic.TopicName))
        {
            throw new ArgumentOutOfRangeException(nameof(topic), $"Not subscribed to topic {topic.TopicName}");
        }
        
        lock (_lock)
        {
            var currentOffset = _subscribedTopicsWithOffset[topic.TopicName];
            var messages = topic.GetMessagesFromOffSet(currentOffset, maxNumberOfMessages);
            _subscribedTopicsWithOffset[topic.TopicName] = currentOffset + messages.Count;
            return messages;
        }
    }
}

public class Simplekafka
{
    private ConcurrentDictionary<string, Topic> _topics;
    private ConcurrentDictionary<string, ConsumerGroup> _consumers;

    public Simplekafka()
    {
        _topics = new();
        _consumers = new();
    }

    public void AddTopic(string name)
    {
        if (!_topics.TryAdd(name, new Topic(name)))
        {
            Console.WriteLine($"topic {name} already exists.");
        }
    }

    public void AddConsumer(string id)
    {
        if (!_consumers.TryAdd(id, new ConsumerGroup(id)))
        {
            Console.WriteLine($"consumer {id} already exists.");
        }
    }

    public void SubscribeConsumerToTopic(string consumerId, string topicName)
    {
        if (!_topics.ContainsKey(topicName))
        {
            throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
        }
        if (_consumers.TryGetValue(consumerId, out var consumer))
        {
            consumer.Subscribe(topicName);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(consumerId), $"consumer {consumerId} doesn't exist.");
        }
    }

    public List<string> ReadMessages(string consumerId, string topicName, int maxNumberOfMessages = 1)
    {
        if (!_topics.TryGetValue(topicName, out var topic))
        {
            throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
        }
        if (!_consumers.TryGetValue(consumerId, out var consumer))
        {
            throw new ArgumentOutOfRangeException(nameof(consumerId), $"consumer {consumerId} doesn't exist.");
        }

        try
        {
            // The lock is now scoped tightly inside the consumer group, removing the global bottleneck
            return consumer.ReadMessages(topic, maxNumberOfMessages);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read messages. {ex.Message}");
            return [];
        }
    }

    public void PublishMessages(string topicName, List<string> messages)
    {
        if (_topics.TryGetValue(topicName, out var topic))
        {
            topic.Publish(messages);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
        }
    }
}
