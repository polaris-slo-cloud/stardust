using Microsoft.Extensions.Logging;
using Stardust.Abstraction.DataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class SatelliteConstellationLoader
{
    private readonly Dictionary<string, ISatelliteConstellationLoader> constellationLoaders = [];
    private readonly ILogger<SatelliteConstellationLoader> logger;

    public SatelliteConstellationLoader(ILogger<SatelliteConstellationLoader> logger)
    {
        this.logger = logger;
    }

    public async Task<List<Stardust.Abstraction.Node.Satellite>> LoadSatelliteConstellation(string dataSource, string? sourceType)
    {
        logger.LogInformation("Trying to load satellite constellation data using source {0} with file type {1}", dataSource, sourceType);

        Stream? sourceStream = null;
        if (File.Exists(dataSource))
        {
            sourceStream = File.OpenRead(dataSource);
        }
        else if (dataSource.StartsWith("http://") || dataSource.StartsWith("https://"))
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(dataSource).ConfigureAwait(false);
            sourceStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        if (sourceStream == null)
        {
            throw new ArgumentException("Invalid data source", nameof(dataSource));
        }

        sourceType ??= TryGuessSourceType(dataSource);

        var loader = constellationLoaders[sourceType];
        if (loader == null)
        {
            throw new ArgumentException("Unsupported data source file type", nameof(dataSource));
        }

        var satellites = await loader.Load(sourceStream).ConfigureAwait(false);
        Parallel.ForEach(satellites, async (s) => await s.ConfigureConstellation(satellites.SkipWhile(i => i != s).ToList()).ConfigureAwait(false));
        //foreach (var satellite in satellites)
        //{
        //    await satellite.ConfigureConstellation(satellites.SkipWhile(s => s != satellite).ToList()); // use skipwhile, that in method ConfigureConstellation there is no .Any(...) search which would make time O(n^2) (now O(n)) and take up to 5min startup time
        //}

        logger.LogInformation("{0} satellites loaded", satellites.Count);
        return satellites;
    }

    public void RegisterDataSourceLoader(string sourceType, ISatelliteConstellationLoader loader)
    {
        constellationLoaders.Add(sourceType, loader);
    }

    private string TryGuessSourceType(string sourceType)
    {
        throw new NotImplementedException();
    }
}
