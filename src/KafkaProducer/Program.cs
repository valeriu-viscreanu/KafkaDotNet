using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register the Kafka Producer as a Singleton
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        MessageTimeoutMs = 5000 // Fast fail if broker is unreachable
    };
    return new ProducerBuilder<Null, string>(producerConfig).Build();
});



var app = builder.Build();

app.MapGet("/", () => "Kafka .NET Application is running. Use POST /produce?message=hello to produce messages.");

app.MapPost("/produce", async (
    [FromQuery] string message, 
    [FromServices] IProducer<Null, string> producer, 
    [FromServices] IConfiguration config,
    [FromServices] ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(message))
    {
        return Results.BadRequest("Message query parameter 'message' is required.");
    }

    var topic = config["Kafka:Topic"] ?? "test-topic";
    logger.LogInformation("Attempting to produce message '{Message}' to topic '{Topic}'", message, topic);

    try
    {
        var kafkaMessage = new Message<string, string> {Key = new Guid().ToString(), Value = message };
        var deliveryResult = await producer.ProduceAsync(topic, kafkaMessage);
        producer.Flush(TimeSpan.FromSeconds(5)); // Ensure message is sent before responding

        logger.LogInformation("Delivered message to '{Topic}' at offset {Offset}", 
            deliveryResult.TopicPartitionOffset.Topic, deliveryResult.TopicPartitionOffset.Offset);
            
        return Results.Ok(new
        {
            Status = "Success",
            Topic = deliveryResult.Topic,
            Partition = deliveryResult.Partition.Value,
            Offset = deliveryResult.Offset.Value,
            Message = message
        });
    }
    catch (ProduceException<Null, string> ex)
    {
        logger.LogError(ex, "Failed to deliver message to Kafka");
        return Results.Problem(
            title: "Kafka Delivery Failure",
            detail: ex.Error.Reason,
            statusCode: 500
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unexpected error occurred producing message");
        return Results.Problem(
            title: "Unexpected Failure",
            detail: ex.Message,
            statusCode: 500
        );
    }
});

app.Run();
