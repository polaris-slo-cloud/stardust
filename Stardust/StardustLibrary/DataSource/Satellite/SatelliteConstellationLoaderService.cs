using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class SatelliteConstellationLoaderService : BackgroundService
{
    // force DI to load TleSatelliteConstellationLoader (and potentially other loaders)
    public SatelliteConstellationLoaderService(TleSatelliteConstellationLoader tleSatelliteConstellationLoader)
    {
    }

    public SatelliteConstellationLoaderService()
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
