using Microsoft.Extensions.Configuration;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Net;
using NATS.Client.JetStream;
using System.Threading.Channels;

string requestsStream = "work-stream";
string responseTopic = "work.complete";

Console.WriteLine("Starting worker");
var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();
var natsOpts = new NatsOpts();
config.GetSection("NATS").Bind(natsOpts);
await using var client = new NatsClient(natsOpts);
var js = client.CreateJetStreamContext();

string consumerName = "nats-consumer";

Console.CancelKeyPress += async (sender, eventArgs) =>
    await js.DeleteConsumerAsync(requestsStream, consumerName);

var consumerConfig = new ConsumerConfig(consumerName)
{
    FilterSubject = "work.requests",
    MaxAckPending = 10_000,
    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
    AckPolicy = ConsumerConfigAckPolicy.Explicit,
    MaxWaiting = 128,
    MaxBatch = 1000
};
var consumer = await js.CreateOrUpdateConsumerAsync(requestsStream, consumerConfig);

var parallelWorkers = 10;
var channel = Channel.CreateBounded<NatsJSMsg<Payload>>(
    new BoundedChannelOptions(parallelWorkers)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

var processingTasks = Enumerable.Range(0, parallelWorkers).Select(async _ =>
{
    await foreach (var message in channel.Reader.ReadAllAsync())
    {
        try
        {
            await js.PublishAsync(responseTopic, message.Data);
            await message.AckAsync();
            Console.WriteLine($"Processed message {message.Data.workflowInstanceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            await message.NakAsync(delay: TimeSpan.FromSeconds(5));
        }
    }
}).ToArray();

while (!channel.Reader.Completion.IsCompleted)
{
    try
    {
        await foreach (var msg in consumer.FetchAsync<Payload>(
            opts: new NatsJSFetchOpts
            {
                MaxMsgs = 1000,
                Expires = TimeSpan.FromSeconds(30)
            }))
        {
            await channel.Writer.WriteAsync(msg);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fetch error: {ex.Message}");
        await Task.Delay(1000);
    }
}
