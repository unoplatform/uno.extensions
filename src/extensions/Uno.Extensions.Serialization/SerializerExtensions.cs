namespace Uno.Extensions.Serialization;

public static class SerializerExtensions
{
    public static string ToString<T>(this ISerializer serializer, T value)
    {
        if (value is not null)
        {
            return serializer.ToString(value, typeof(T));
        }

        return string.Empty;
    }

    public static T? FromString<T>(this ISerializer serializer, string valueAsString)
    {
        return serializer is not null ?
        (serializer.FromString(valueAsString, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
    }
}
