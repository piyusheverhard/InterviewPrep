using System.Collections.Concurrent;

namespace PartitionedKafka;

public class Partition
{
    public int PartitionId { get; init; }
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
            _messages.AddRange(messages);
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
                messages.Add(_messages[i]);
            }
            _consumerOffset[consumerId] = offset + messages.Count;
            return messages;
        }
    }

    public void AddConsumer(string consumerId)
    {
        lock (_lock)
        {
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
        // Simple assignment: Subscribing the consumer to all partitions with offset 0
        foreach (var partition in _partitions)
        {
            partition.AddConsumer(consumerId);
        }
    }
}

public class PartitionedKafka
{
    private ConcurrentDictionary<string, Topic> _topics;

    public PartitionedKafka()
    {
        _topics = new();
    }

    public void AddTopic(string name, int numPartition)
    {
        if (!_topics.TryAdd(name, new Topic(name, numPartition)))
        {
            Console.WriteLine($"topic {name} already exists.");
        }
    }

    public void SubscribeConsumerToTopic(string consumerId, string topicName)
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

    public List<string> ReadMessages(string consumerId, string topicName, int partitionId, int maxNumberOfMessages = 1)
    {
        if (_topics.TryGetValue(topicName, out var topic))
        {
            return topic.GetMessagesFromPartition(partitionId, consumerId, maxNumberOfMessages);
        }
        
        throw new ArgumentOutOfRangeException(nameof(topicName), $"topic {topicName} doesn't exist.");
    }

    public void PublishMessages(string topicName, List<string> messages, string routingKey)
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
}
