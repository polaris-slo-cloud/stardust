using System.Threading.Tasks;

namespace StardustLibrary.Node.Computing;

public abstract class Computing(double cpu, double memory)
{
    public double Cpu { get; } = cpu;
    public double Memory { get; } = memory;

    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }

    public abstract Task ScheduleWorkload(Workload workload);
}
