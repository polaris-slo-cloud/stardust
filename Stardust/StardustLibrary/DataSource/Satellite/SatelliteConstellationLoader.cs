using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class SatelliteConstellationLoader
{
    private readonly Dictionary<string, ISatelliteConstellationLoader> constellationLoaders = [];
    private readonly Dictionary<string, List<Node.Satellite>> loadedConstellations = [];
    private readonly ILogger<SatelliteConstellationLoader>? logger;

    public SatelliteConstellationLoader(ILogger<SatelliteConstellationLoader>? logger = default)
    {
        this.logger = logger;
    }

    public async Task<List<Node.Satellite>> LoadSatelliteConstellation(string dataSource, string? sourceType)
    {
        logger?.LogInformation("Trying to load satellite constellation data using source {0} with file type {1}", dataSource, sourceType);

        if (loadedConstellations.TryGetValue(dataSource, out var satellites))
        {
            return satellites;
        }

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
        return loader == null
            ? throw new ArgumentException("Unsupported data source file type", nameof(dataSource))
            : (loadedConstellations[dataSource] = await loader.Load(sourceStream).ConfigureAwait(false));
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
