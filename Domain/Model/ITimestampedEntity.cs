using System;

namespace N17Solutions.Microphobia.Domain.Model
{
    /// <summary>
    /// Defines the contract of an entity that can track the time it was created and last updated
    /// </summary>
    public interface ITimestampedEntity
    {
        /// <summary>
        /// The Date this Entity was Created.
        /// </summary>
        DateTime DateCreated { get; set; }
        
        /// <summary>
        /// The Date this Entity was Last Updated.
        /// </summary>
        DateTime DateLastUpdated { get; set; }
    }
}