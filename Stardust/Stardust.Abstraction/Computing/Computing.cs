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
    public double CpuUsagePercent { get => CpuUsage / Cpu; }
    public double MemoryUsage { get; private set; }
    public double MemoryUsagePercent { get => MemoryUsage / Memory; }

    public double CpuAvailable => Cpu - CpuUsage;
    public double MemoryAvailable => Memory - MemoryUsage;

    public List<DeployableService> Services { get; } = [];

    public virtual Task PlaceDeploymentAsync(DeployableService service)
    {
        lock (Services)
        {
            if (!CanPlace(service))
            {
                throw new ApplicationException("Cannot place deployment");
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

    public virtual bool CanPlace(DeployableService service)
    {
        if (service.Cpu > CpuAvailable)
        {
            return false; throw new ApplicationException("Deployment consumes too much cpu");
        }
        if (service.Memory > MemoryAvailable)
        {
            return false; throw new ApplicationException("Deployment consumes too much memory");
        }
        if (Services.Contains(service))
        {
            return false; throw new ApplicationException("Deployment already placed at this computing unit");
        }
        return true;
    }

    internal Computing Clone()
    {
        return new Computing(Cpu, Memory, Type);
    }
}
