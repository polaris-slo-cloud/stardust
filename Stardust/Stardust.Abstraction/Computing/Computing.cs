using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stardust.Abstraction.Deployment;
using Stardust.Abstraction.Exceptions;

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

    private Node.Node? node;

    public virtual void Mount(Node.Node node)
    {
        if (this.node != null)
        {
            throw new MountException("Computing is already mounted to node");
        }
        this.node = node;
    }

    public virtual async Task<bool> TryPlaceDeploymentAsync(DeployableService service)
    {
        if (this.node == null)
        {
            throw new MountException("Computing must be mounted to node, before it can be used");
        }

        lock (Services)
        {
            if (!CanPlace(service))
            {
                return false;
            }

            Services.Add(service);
            CpuUsage += service.Cpu;
            MemoryUsage += service.Memory;
        }

        await this.node.Router.AdvertiseNewServiceAsync(service.ServiceName);
        return true;
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

    public virtual bool HostsService(string serviceName)
    {
        lock (Services)
        {
            return Services.Any(s => s.ServiceName == serviceName);
        }
    }

    internal Computing Clone()
    {
        return new Computing(Cpu, Memory, Type);
    }
}
