using Microsoft.Extensions.Options;
using System.Linq;

namespace Stardust.Abstraction.Computing;

public class ComputingBuilder(ComputingConfiguration computingConfiguration) : IComputingBuilder
{
    private readonly ComputingConfiguration computingConfiguration = computingConfiguration;
    private Computing useComputing = Computing.None;

    public Computing Build()
    {
        return useComputing;
    }

    public IComputingBuilder WithComputingType(ComputingType computingType)
    {
        useComputing = computingConfiguration.Configurations.FirstOrDefault(c => c.Type == computingType)?.Clone() ?? Computing.None;
        return this;
    }
}
