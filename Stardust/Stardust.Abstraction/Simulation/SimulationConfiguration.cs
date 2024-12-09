using System;

namespace StardustLibrary.Simulation;

public class SimulationConfiguration
{
    public double? StepMultiplier { get; set; }

    /// <summary>
    /// StepLength in seconds
    /// </summary>
    public double? StepLength { get; set; }

    /// <summary>
    /// Gets or sets the interval at which the simulation advances by one StepLength.
    /// </summary>
    /// <remarks>
    /// - `1` represents advancing the simulation by 1 StepLength per second. <br />
    /// - `0` stands for running the simulation as fast as possible. <br />
    /// - `-1` indicates that the simulation is controlled manually.
    /// </remarks>
    public double StepInterval { get; set; }

    public required string SatelliteDataSource { get; set; }

    public required string SatelliteDataSourceType { get; set; }

    public required bool UsePreRouteCalc {  get; set; }

    public required int MaxCpuCores { get; set; }

    public required DateTime SimulationStartTime { get; set; }
}
