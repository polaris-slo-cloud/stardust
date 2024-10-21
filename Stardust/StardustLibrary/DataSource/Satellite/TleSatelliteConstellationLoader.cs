using Microsoft.Extensions.Logging;
using Stardust.Abstraction.DataSource;
using Stardust.Abstraction.Links;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public class TleSatelliteConstellationLoader : ISatelliteConstellationLoader
{
    private const string DATA_SOURCE_TYPE = "tle";
    private const string CANNOT_PARSE = "Cannot parse tle data source";

    private readonly InterSatelliteLinkConfig config;
    private readonly ILogger<TleSatelliteConstellationLoader>? logger;

    public TleSatelliteConstellationLoader(InterSatelliteLinkConfig config, SatelliteConstellationLoader constellationLoader, ILogger<TleSatelliteConstellationLoader>? logger = default) : this(config, logger)
    {
        constellationLoader.RegisterDataSourceLoader(DATA_SOURCE_TYPE, this);
    }

    public TleSatelliteConstellationLoader(InterSatelliteLinkConfig config, ILogger<TleSatelliteConstellationLoader>? logger = default)
    {
        this.config = config;
        this.logger = logger;
    }

    public async Task<List<Stardust.Abstraction.Node.Satellite>> Load(Stream stream)
    {
        logger?.LogTrace("Try to parse stream as tle data.");

        var satellites = new List<Stardust.Abstraction.Node.Satellite>();
        using var reader = new StreamReader(stream, leaveOpen: false);

        string? line1, line2;
        while ((line1 = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line1))
            {
                continue;
            }

            string? name = null;
            if (!line1.StartsWith('1'))
            {
                name = line1.Trim();

                line1 = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line1 == null || !line1.StartsWith('1'))
                {
                    throw new ApplicationException(CANNOT_PARSE);
                }
            }

            line2 = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line2 == null || !line2.StartsWith('2'))
            {
                throw new ApplicationException(CANNOT_PARSE);
            }

            if (string.IsNullOrEmpty(name))
            {
                name = line1.Substring(2, 4);
            }

            var builder = new SatelliteBuilder();

            builder.SetName(name);
            builder.SetInclination(double.Parse(line2.Substring(8, 8).Trim(), CultureInfo.InvariantCulture)); // Inclination in degrees
            builder.SetRightAscension(double.Parse(line2.Substring(17, 8).Trim(), CultureInfo.InvariantCulture)); // RAAN in degrees
            builder.SetEccentricity(double.Parse("0." + line2.Substring(26, 7).Trim(), CultureInfo.InvariantCulture)); // Eccentricity
            builder.SetArgumetOfPerigee(double.Parse(line2.Substring(34, 8).Trim(), CultureInfo.InvariantCulture)); // Argument of Perigee in degrees
            builder.SetMeanAnomaly(double.Parse(line2.Substring(43, 8).Trim(), CultureInfo.InvariantCulture)); // Mean Anomaly in degrees
            builder.SetMeanMotion(double.Parse(line2.Substring(52, 12).Trim(), CultureInfo.InvariantCulture)); // Mean motion (revolutions per day)

            // Parse the epoch
            string epochStr = line1.Substring(18, 12).Trim(); // Get epoch from line 1
            DateTime epoch = ParseEpoch(epochStr);

            builder.SetEpoch(epoch)
                .ConfigureIsl((b) => b.SetConfig(config));

            var satellite = builder.Build();
            satellites.Add(satellite);
        }

        return satellites;
    }

    private static DateTime ParseEpoch(string epochStr)
    {
        // Extract the year and day of year from the epoch string
        int year = int.Parse(epochStr.Substring(0, 2)) + 2000; // Assume 2000s
        double dayOfYear = double.Parse(epochStr.Substring(2), CultureInfo.InvariantCulture);

        // Create DateTime for the epoch (start of the year)
        DateTime startOfYear = new DateTime(year, 1, 1);

        // Calculate the exact date based on the day of the year
        return startOfYear.AddDays(dayOfYear - 1); // Subtract 1 because days are 1-indexed
    }
}