# Stardust
A simulator for the 3D Continuum

## Stardust
A console application using HostApplicationBuilder, which manages dependency injection, services, configuration, ...

### Run the Stardust Simulator

Use `dotnet run --project Stardust.csproj -c Release` in `./Stardust/Stardust` to run the application. \
See `appsettings.json` to modify the configuration. The most interesting properties might are 
- `SimulationConfiguration.SatelliteDataSource` set to the file path of a data source for the satellite constellation
- `SimulationConfiguration.SimulationStartTime` set to a timestamp to start the simulation from (simulation time can be set and controlled programatically too)
- `InterSatelliteLinkConfig.Protocol` set to `mst`/`mst_loop`/`mst_smart_loop`/`pst`/`pst_loop`/`pst_smart_loop` 
- `RouterConfig.Protocol` set to `a-star`/`dijkstra`


### Write your own program with Stardust

Stardust allows you to easily simulate a 3D Continum programmatically.

```c#
// This class needs to implement IHostedService (in this case BackgroundService is implementing it)
// Via dependency injection in the constructor you get access to eg the ISimulationController
public class YourOwnService(ISimulationController simulationController, ...) : BackgroundService
{
  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // you can write your own program here eg:

    // get all nodes or only specific nodes to use or inspect
    var nodes = await simulationController.GetAllNodesAsync();
    var satellites = await simulationController.GetAllNodesAsync<Satellite>();
    var groundStations = await simulationController.GetAllNodesAsync<GroundStation>();
    for (int step = 0; step < NUM_STEPS; step++)
    {
      // simulate the constellation and network 60 seconds from current simulation time
      await simulationController.StepAsync(60);
      
      // OR simulate the constellation and network for some DateTime
      await simulationController.StepAsync(DateTime.Now);

      // do something after constallation/links/routing are calculated for this step
    }
  }
}
```

Add your created service to the `HostApplicationBuilder` in `Program.cs`:

```c#
builder.Services.AddHostedService<YourOwnService>();
```

### Stardust components

To add your own components, you need to implement corresponding interfaces and register the new components if needed. Most interesting interfaces might are:

- For a new routing protocol implement the `IRouter` interface (and add the new implementation to `RouterBuilder`, so you can change routing protocols via `appsettings.json`)
- For a new inter satellite protocol, implement the `IInterSatelliteLinkProtocol` interface (and add the new implementation to `IslProtocolBuilder`)
- For a new orchestrator, implement the `IDeploymentOrchestrator` interface
- For new node types (eg Car, Airplain, GEO-Satellite) the abstract class `Node` has to be implemented and then integrated into the simulation controller.


## StardustLibrary
Contains all the interfaces, classes, implementation, etc

## StardustVisualization
A simple WPF application which can visualize earth, ground stations and satellites.
