using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class DateTimeMaterializerSource : EntityMaterializerSource
    {
        private static readonly MethodInfo NormalizeMethod = typeof(DateTimeMapper).GetTypeInfo().GetMethod(nameof(DateTimeMapper.Normalize), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo NormalizeNullableMethod = typeof(DateTimeMapper).GetTypeInfo().GetMethod(nameof(DateTimeMapper.NormalizeNullable), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        public override Expression CreateReadValueExpression(Expression valueBuffer, Type type, int index, IPropertyBase property)
        {
            if (type == typeof(DateTime))
                return Expression.Call(NormalizeMethod, base.CreateReadValueExpression(valueBuffer, type, index, property));

            return type == typeof(DateTime?) 
                ? Expression.Call(NormalizeNullableMethod, base.CreateReadValueExpression(valueBuffer, type, index, property)) 
                : base.CreateReadValueExpression(valueBuffer, type, index, property);
        }

        private static class DateTimeMapper
        {
            public static DateTime Normalize(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

            public static DateTime? NormalizeNullable(DateTime? value)
            {
                if (!value.HasValue)
                    return null;

                return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            }
        }
    }
}