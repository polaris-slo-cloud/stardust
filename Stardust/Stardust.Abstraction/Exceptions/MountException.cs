using System;

namespace Stardust.Abstraction.Exceptions;

public class MountException : ApplicationException
{
    public MountException()
    {
    }

    public MountException(string message) : base(message)
    {
    }
}
