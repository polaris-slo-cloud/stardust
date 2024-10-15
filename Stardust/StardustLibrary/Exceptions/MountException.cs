using System;

namespace StardustLibrary.Exceptions;

public class MountException : ApplicationException
{
    public MountException()
    {
    }

    public MountException(string message) : base(message)
    {
    }
}
