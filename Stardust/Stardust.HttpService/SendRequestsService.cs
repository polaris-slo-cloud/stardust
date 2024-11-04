using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust.HttpService;

public class SendRequestsService(ILogger<SendRequestsService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {

            var sw = Stopwatch.StartNew();
            using var client = new HttpClient()
            {
                BaseAddress = new System.Uri("http://localhost:8081")
            };
            client.DefaultRequestHeaders.Add("fromNode", "Vienna");
            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                using var message = new HttpRequestMessage(HttpMethod.Get, "/");
                message.Headers.Add("toService", "HttpService");

                using var response = await client.SendAsync(message, stoppingToken);
                logger.LogInformation("Request to response status {0} in {1}ms", response.StatusCode, sw.ElapsedMilliseconds);

                await Task.Delay(5_000);
            }
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
}
}
