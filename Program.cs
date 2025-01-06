using Microsoft.Extensions.Configuration;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Net;

string requestsStream = "requests-stream";
string responseTopic = "response.feedback";

// Usage:
// $ nats-worker
//   Launches a worker that listens on all subjects
// $ nats-worker <subject>
//   Launches a worker that filters on the specific subject

var arguments = Environment.GetCommandLineArgs();
string subject = (arguments.Length > 1) ? arguments[1] : "all";

Console.WriteLine("Starting worker for " + subject);
var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();
var natsOpts = new NatsOpts();
config.GetSection("NATS").Bind(natsOpts);

await using var client = new NatsClient(natsOpts);
var js = client.CreateJetStreamContext();
string consumerName = subject + "-consumer";

Console.CancelKeyPress += async (sender, eventArgs) =>
    await js.DeleteConsumerAsync(requestsStream, consumerName);

var consumerConfig = new ConsumerConfig(consumerName)
{
    FilterSubject = "requests." + ((subject == "all") ? "*" : subject)
};
var consumer = await js.CreateOrUpdateConsumerAsync(requestsStream, consumerConfig);

Console.WriteLine("Consumer listening..");
await foreach (var message in consumer.ConsumeAsync<string>())
{
    Console.WriteLine($"Received: {message.Data}");
    await Task.Delay(5); // pretend to do some work
    await js.PublishAsync(responseTopic, message.Data);
    await message.AckAsync();
}
