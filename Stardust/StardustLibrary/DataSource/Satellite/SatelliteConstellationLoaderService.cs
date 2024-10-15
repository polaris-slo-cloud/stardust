using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class SatelliteConstellationLoaderService : BackgroundService
{
    public SatelliteConstellationLoaderService(System.IServiceProvider serviceProvider)
    {
        _ = serviceProvider.GetService<TleSatelliteConstellationLoader>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
