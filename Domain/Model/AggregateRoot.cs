using System;
using N17Solutions.Microphobia.Utilities.Extensions;

namespace N17Solutions.Microphobia.Domain.Model
{
    /// <summary>
    /// An instance of an Aggregate Root in the domain.
    /// </summary>
    public abstract class AggregateRoot : Entity<long>
    {
        /// <summary>
        /// The globally unique identifier of this aggregate.
        /// </summary>
        /// <remarks>
        /// This is the globally unique identifier that clients can use to reference this aggregate. This is also the identifier you must use for cross-aggregate references.
        /// </remarks>
        public Guid ResourceId { get; set; } = Guid.NewGuid();
        
        protected AggregateRoot() {}

        protected AggregateRoot(Guid resourceId)
        {
            if (resourceId.IsDefault() || resourceId == Guid.Empty)
                throw new ArgumentException($"Invalid {nameof(resourceId)} provided: '{resourceId}'. A Resource Identifier must be a valid, globally unique identifier.");

            ResourceId = resourceId;
        }
    }
}