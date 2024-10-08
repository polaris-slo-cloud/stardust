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

    public TleSatelliteConstellationLoader(SatelliteConstellationLoader constellationLoader)
    {
        constellationLoader.RegisterDataSourceLoader(DATA_SOURCE_TYPE, this);
    }

    public TleSatelliteConstellationLoader()
    {
    }

    public async Task<List<Node.Satellite>> Load(Stream stream)
    {
        var satellites = new List<Node.Satellite>();
        using var reader = new StreamReader(stream, leaveOpen: false);

        string? line1, line2;
        while ((line1 = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line1))
            {
                continue;
            }

            string? name = null;
            if (!line1.StartsWith('1'))
            {
                name = line1.Trim();

                if ((line1 = await reader.ReadLineAsync()) == null || !line1.StartsWith('1'))
                {
                    throw new ApplicationException(CANNOT_PARSE);
                }
            }

            if ((line2 = await reader.ReadLineAsync()) == null || !line2.StartsWith('2'))
            {
                throw new ApplicationException(CANNOT_PARSE);
            }

            if (string.IsNullOrEmpty(name))
            {
                name = line1.Substring(2, 4);
            }

            double inclination = double.Parse(line2.Substring(8, 8).Trim(), CultureInfo.InvariantCulture); // Inclination in degrees
            double rightAscension = double.Parse(line2.Substring(17, 8).Trim(), CultureInfo.InvariantCulture); // RAAN in degrees
            double eccentricity = double.Parse("0." + line2.Substring(26, 7).Trim(), CultureInfo.InvariantCulture); // Eccentricity
            double argumentOfPerigee = double.Parse(line2.Substring(34, 8).Trim(), CultureInfo.InvariantCulture); // Argument of Perigee in degrees
            double meanAnomaly = double.Parse(line2.Substring(43, 8).Trim(), CultureInfo.InvariantCulture); // Mean Anomaly in degrees
            double meanMotion = double.Parse(line2.Substring(52, 12).Trim(), CultureInfo.InvariantCulture); // Mean motion (revolutions per day)

            // Parse the epoch
            string epochStr = line1.Substring(18, 12).Trim(); // Get epoch from line 1
            DateTime epoch = ParseEpoch(epochStr);

            var satellite = new Node.Satellite(name, inclination, rightAscension, eccentricity, argumentOfPerigee, meanAnomaly, meanMotion, epoch);
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