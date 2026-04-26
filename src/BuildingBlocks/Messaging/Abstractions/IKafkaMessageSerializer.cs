namespace Messaging.Abstractions;

public interface IKafkaMessageSerializer
{
    string Serialize<T>(T message);
    T? Deserialize<T>(string data);
}