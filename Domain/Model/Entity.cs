using System;

namespace N17Solutions.Microphobia.Domain.Model
{
    public abstract class Entity<TId> : ITimestampedEntity
    {
        /// <summary>
        /// The database identifier of this entity.
        /// </summary>
        /// <remarks>
        /// This is a *PERSISTENCE ONLY* concern. It must not be exposed to clients of this domain. It must not be provided to *ANYONE*.
        /// It must not be used for cross aggregate references (use the Aggregate's Resource Id instead).
        /// </remarks>
        public virtual TId Id { get; set; }

        /// <inheritdoc cref="ITimestampedEntity" />
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /// <inheritdoc cref="ITimestampedEntity" />
        public DateTime DateLastUpdated { get; set; } = DateTime.UtcNow;
    }
}