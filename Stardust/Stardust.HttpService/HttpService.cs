using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Routing;
using Stardust.Abstraction.Simulation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust.HttpService;

public class HttpService : BackgroundService
{
    private readonly ISimulationController simulationController;
    private readonly IDeploymentOrchestrator deploymentOrchestrator;
    private readonly ILogger<HttpService> logger;

    public HttpService(ISimulationController simulationController, IDeploymentOrchestrator deploymentOrchestrator, ILogger<HttpService> logger)
    {
        this.simulationController = simulationController;
        this.deploymentOrchestrator = deploymentOrchestrator;
        this.logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var specs = new HttpDeploymentSpecs(new DeployableService("HttpService", 1, 1));
        deploymentOrchestrator.CreateDeploymentAsync(specs);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            using var client = new HttpClient();
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8081/");
            listener.Start();

            var nodes = await simulationController.GetAllNodesAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Factory.StartNew(async (o) =>
                {
                    HttpListenerContext innerContext = (HttpListenerContext)o!;
                    HttpListenerRequest request = innerContext.Request;
                    using var response = innerContext.Response;

                    string? fromNode = request.Headers.Get("fromNode") ?? "Vienna";
                    string? toNode = request.Headers.Get("toNode");
                    string? toService = request.Headers.Get("toService") ?? "HttpService";

                    if (string.IsNullOrWhiteSpace(fromNode) || (string.IsNullOrWhiteSpace(toNode) && string.IsNullOrWhiteSpace(toService)))
                    {
                        await ReplyBadRequestAsync(innerContext, "Header missing");
                        return;
                    }

                    var source = nodes.FirstOrDefault(n => n.Name == fromNode);
                    if (source == null)
                    {
                        await ReplyBadRequestAsync(innerContext, "Unknown source node");
                        return;
                    }

                    IRouteResult routeResult;
                    if (!string.IsNullOrWhiteSpace(toNode))
                    {
                        var target = nodes.FirstOrDefault(n => n.Name == toNode);
                        if (target == null)
                        {
                            await ReplyBadRequestAsync(innerContext, "Unknown target node");
                            return;
                        }

                        routeResult = await source.Router.RouteAsync(target);
                    }
                    else
                    {
                        routeResult = await source.Router.RouteAsync(toService!);
                    }

                    if (!routeResult.Reachable)
                    {
                        await ReplyUnreachableAsync(context, "No Route");
                        return;
                    }

                    await routeResult.WaitLatencyAsync();

                    using var httpResponse = await client.GetAsync($"https://www.cloudflare.com{request.Url?.AbsolutePath ?? string.Empty}", stoppingToken);
                    using var responseStream = await httpResponse.Content.ReadAsStreamAsync(stoppingToken);

                    response.ContentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "text";
                    response.StatusCode = (int)httpResponse.StatusCode;
                    response.StatusDescription = httpResponse.ReasonPhrase ?? "null";
                    foreach (var header in httpResponse.Headers)
                    {
                        response.Headers.Add(header.Key, string.Join(',', header.Value));
                    }

                    await routeResult.WaitLatencyAsync();
                    await responseStream.CopyToAsync(response.OutputStream, stoppingToken);
                }, context, stoppingToken);
            }
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static Task ReplyBadRequestAsync(HttpListenerContext context, string message)
    {
        return ReplyStatusAndMessage(context, message, 400, "Bad Request");
    }

    private static Task ReplyUnreachableAsync(HttpListenerContext context, string message)
    {
        return ReplyStatusAndMessage(context, message, 523, "Origin Is Unreachable");
    }

    private static async Task ReplyStatusAndMessage(HttpListenerContext context, string message, int code, string phrase)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var response = context.Response;

        response.StatusCode = code;
        response.StatusDescription = phrase;
        response.ContentType = "text";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);
    }
}
