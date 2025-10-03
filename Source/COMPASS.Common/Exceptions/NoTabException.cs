using System;

namespace COMPASS.Common.Exceptions;

/// <summary>
/// Thrown when at least 1 tab is expected to be opened but there is none
/// </summary>
public class NoTabException : Exception
{
    public NoTabException(string msg) : base(msg)
    {
        
    }
}