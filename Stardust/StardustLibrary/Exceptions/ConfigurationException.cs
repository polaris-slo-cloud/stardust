using System;

namespace StardustLibrary.Exceptions;

public class ConfigurationException : ApplicationException
{
    public ConfigurationException(string message) : base(message)
    {
    }
}
