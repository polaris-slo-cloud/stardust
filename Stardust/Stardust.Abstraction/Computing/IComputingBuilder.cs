namespace Stardust.Abstraction.Computing;

public interface IComputingBuilder
{
    public IComputingBuilder WithComputingType(ComputingType computingType);
    public Computing Build();
}
