using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace Stockfish.NET.Models
{
    public static class EnumExtensions
    {
        public static T ToEnumByAttributes<T>(this string value)
            where T : Enum
        {
            var enumType = typeof(T);
            foreach (var name in Enum.GetNames(enumType))
            {
                var field = enumType.GetField(name);
                if (field == null) continue;

                var enumMemberAttribute = GetEnumMemberAttribute(field);
                if (enumMemberAttribute != null && enumMemberAttribute.Value == value)
                {
                    return (T)Enum.Parse(enumType, name);
                }

                var descriptionAttribute = GetDescriptionAttribute(field);
                if (descriptionAttribute != null && descriptionAttribute.Description == value)
                {
                    return (T)Enum.Parse(enumType, name);
                }

                if (name == value)
                {
                    return (T)Enum.Parse(enumType, name);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, $"The value could not be mapped to type {enumType.FullName}");
        }

        public static string ToStringByAttributes(this Enum value)
        {
            var field = value
                .GetType()
                .GetField(value.ToString());

            if (field == null) return string.Empty;

            var enumMemberAttribute = GetEnumMemberAttribute(field);
            if (enumMemberAttribute != null)
            {
                return enumMemberAttribute.Value ?? string.Empty;
            }

            var descriptionAttribute = GetDescriptionAttribute(field);
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description;
            }

            return value.ToString();
        }

        private static DescriptionAttribute? GetDescriptionAttribute(FieldInfo field)
        {
            return field
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .OfType<DescriptionAttribute>()
                .SingleOrDefault();
        }

        private static EnumMemberAttribute? GetEnumMemberAttribute(FieldInfo field)
        {
            return field
                .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .OfType<EnumMemberAttribute>()
                .SingleOrDefault();
        }
    }
}
