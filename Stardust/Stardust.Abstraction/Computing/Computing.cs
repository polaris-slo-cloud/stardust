using System.Threading.Tasks;

namespace Stardust.Abstraction.Computing;

public abstract class Computing(double cpu, double memory)
{
    public ComputingType Type { get; set; }
    public double Cpu { get; } = cpu;
    public double Memory { get; } = memory;

    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }

    public abstract Task ScheduleWorkload(Workload workload);
}
