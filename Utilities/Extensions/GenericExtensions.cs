using System;
using System.Reflection;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class GenericExtensions
    {
        public static bool IsDefault(this object target)
        {
            if (target == null)
                return true;

            var defaultValue = new MethodWrapper().GetDefault(target.GetType());
            return target.Equals(defaultValue);
        }

        private class MethodWrapper
        {
            public object GetDefault(Type t)
            {
                return GetType()
                    .GetMethod(nameof(GetDefaultGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    ?.MakeGenericMethod(t)
                    .Invoke(this, null);
            }

            private static T GetDefaultGeneric<T>()
            {
                return default(T);
            }
        }
    }
}