using System.Text.Json;
using Confluent.Kafka;
using Messaging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Kafka;

/// <summary>
/// Background service that consumes messages from Kafka topic
/// </summary>
public sealed class KafkaConsumerService<TMessage> : BackgroundService 
    where TMessage : class
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService<TMessage>> _logger;
    private readonly IKafkaMessageSerializer _serializer;
    private readonly string _topic;

    public KafkaConsumerService(
        IOptions<KafkaOptions> options,
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerService<TMessage>> logger,
        IKafkaMessageSerializer serializer,
        string topic)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _serializer = serializer;
        _topic = topic;

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = string.IsNullOrEmpty(options.Value.GroupId) 
                ? $"{options.Value.ClientId}-{typeof(TMessage).Name.ToLower()}-consumer"
                : options.Value.GroupId,
            AutoOffsetReset = options.Value.AutoOffsetReset,
            EnableAutoCommit = options.Value.EnableAutoCommit
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Started consuming from topic: {Topic}", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult?.Message == null) continue;

                await ProcessMessageAsync(consumeResult, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from Kafka topic {Topic}", _topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Kafka consumer for topic {Topic}", _topic);
            }
        }

        _consumer.Close();
        _logger.LogInformation("Stopped consuming from topic: {Topic}", _topic);
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        _logger.LogDebug(
            "Received message from {Topic} at offset {Offset}: {Key}",
            consumeResult.Topic,
            consumeResult.Offset,
            consumeResult.Message.Key);

        try
        {
            var message = _serializer.Deserialize<TMessage>(consumeResult.Message.Value);
            
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message from topic {Topic} at offset {Offset}", 
                    consumeResult.Topic, consumeResult.Offset);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IKafkaConsumer<TMessage>>();

            await messageHandler.HandleAsync(message, stoppingToken);
            
            _consumer.Commit(consumeResult);
            
            _logger.LogDebug(
                "Successfully processed message from {Topic} at offset {Offset}",
                consumeResult.Topic,
                consumeResult.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic} at offset {Offset}", 
                consumeResult.Topic, consumeResult.Offset);
            // Don't commit on error - message will be reprocessed based on AutoOffsetReset or manual retry logic
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

