using Microsoft.EntityFrameworkCore;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public enum ContextBehavior
    {
        Readonly,
        Writable
    }

    public static class ContextBehaviourExtensions
    {
        public static QueryTrackingBehavior GetQueryTrackingBehavior(this ContextBehavior target)
        {
            return target == ContextBehavior.Writable
                ? QueryTrackingBehavior.TrackAll
                : QueryTrackingBehavior.NoTracking;
        }
    }
}