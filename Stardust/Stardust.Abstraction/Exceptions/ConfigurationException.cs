using System;

namespace Stardust.Abstraction.Exceptions;

public class ConfigurationException : ApplicationException
{
    public ConfigurationException(string message) : base(message)
    {
    }
}
