using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stardust.Abstraction.Deployment;

namespace Stardust.Abstraction.Computing;

public class Computing(double cpu, double memory, ComputingType type)
{
    internal static readonly Computing None = new(0, 0, ComputingType.None);

    public double Cpu { get; } = cpu;
    public double Memory { get; } = memory;
    public ComputingType Type { get; } = type;

    public double CpuUsage { get; private set; }
    public double MemoryUsage { get; private set; }

    public double CpuAvailable => Cpu - CpuUsage;
    public double MemoryAvailable => Memory - MemoryUsage;

    public List<DeployableService> Services { get; } = [];

    public virtual Task PlaceDeploymentAsync(DeployableService service)
    {
        lock (Services)
        {
            if (service.Cpu > Cpu)
            {
                throw new ApplicationException("Deployment consumes too much cpu");
            }
            if (service.Memory > Memory)
            {
                throw new ApplicationException("Deployment consumes too much memory");
            }
            if (Services.Contains(service))
            {
                throw new ApplicationException("Deployment already placed at this computing unit");
            }

            Services.Add(service);
            CpuUsage += service.Cpu;
            MemoryUsage += service.Memory;
        }
        return Task.CompletedTask;
    }

    public virtual Task RemoveDeploymentAsync(DeployableService service)
    {
        lock (Services)
        {
            if (!Services.Remove(service))
            {
                return Task.CompletedTask;
            }

            CpuUsage -= service.Cpu;
            MemoryUsage -= service.Memory;
        }
        return Task.CompletedTask;
    }

    internal Computing Clone()
    {
        return new Computing(Cpu, Memory, Type);
    }
}
