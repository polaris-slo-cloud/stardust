using System;

namespace Stardust.Abstraction.Deployment;

public class DeployableService
{
    public string ServiceName { get; }
    public double Cpu { get; }
    public double Memory { get; }

    public DeployableService(string serviceName, double cpu, double memory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cpu);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(memory);

        ServiceName = serviceName;
        Cpu = cpu;
        Memory = memory;
    }
}