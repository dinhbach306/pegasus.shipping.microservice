using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging;

/// <summary>
/// Background service that consumes messages from Kafka topic
/// </summary>
public sealed class KafkaConsumerService<TMessage> : BackgroundService 
    where TMessage : class
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService<TMessage>> _logger;
    private readonly string _topic;

    public KafkaConsumerService(
        IOptions<KafkaOptions> options,
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerService<TMessage>> logger,
        string topic)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _topic = topic;

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = $"{options.Value.ClientId}-{typeof(TMessage).Name.ToLower()}-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Started consuming from topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                    {
                        continue;
                    }

                    _logger.LogInformation(
                        "Received message from {Topic} at offset {Offset}: {Key}",
                        consumeResult.Topic,
                        consumeResult.Offset,
                        consumeResult.Message.Key);

                    // Use camelCase to match serialization policy
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true, // Allow case-insensitive matching
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    
                    var message = JsonSerializer.Deserialize<TMessage>(consumeResult.Message.Value, jsonOptions);
                    
                    if (message != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var messageHandler = scope.ServiceProvider.GetRequiredService<IKafkaConsumer<TMessage>>();

                        await messageHandler.HandleAsync(message, stoppingToken);
                        _consumer.Commit(consumeResult);
                        
                        _logger.LogInformation(
                            "Successfully processed message from {Topic} at offset {Offset}",
                            consumeResult.Topic,
                            consumeResult.Offset);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    // Don't commit on error - message will be reprocessed
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Stopped consuming from topic: {Topic}", _topic);
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

