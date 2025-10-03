using System;

namespace COMPASS.Common.Exceptions;

public class LoadException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="collectionId">The id of the collection that failed to load</param>
    /// <param name="message"></param>
    public LoadException(string collectionId, string? message = null) : base(message)
    {
        CollectionId = collectionId;
    }
    
    public string CollectionId { get; }
}