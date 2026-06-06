using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KafkaConsumer;

public class KafkaConsumerWorker : BackgroundService
{
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public KafkaConsumerWorker(ILogger<KafkaConsumerWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:Topic"] ?? "test-topic";
        _groupId = configuration["Kafka:GroupId"] ?? "dotnet-consumer-group";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Worker starting. Target Topic: {Topic}, Broker: {Servers}", _topic, _bootstrapServers);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        // Yield execution to allow startup of host
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(_topic);
                _logger.LogInformation("Successfully subscribed to topic: {Topic}", _topic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        if (consumeResult != null)
                        {
                            _logger.LogInformation($"""
                                Consumed message: Value={consumeResult.Message.Value} 
                                at Offset={consumeResult.Offset} 
                                Key={consumeResult.Message.Key}""");
                                
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error occurred during message consumption");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize or run Kafka consumer. Retrying in 5 seconds...");
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Ignore, stopping
                }
            }
        }

        _logger.LogInformation("Kafka Consumer Worker stopped.");
    }
}
