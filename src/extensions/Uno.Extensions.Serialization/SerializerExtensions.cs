namespace Uno.Extensions.Serialization;

public static class SerializerExtensions
{
    public static string ToString<T>(this ISerializer serializer, T value) =>
        value is not null ?
            serializer.ToString(value, typeof(T)) :
            string.Empty;

    public static T? FromString<T>(this ISerializer serializer, string valueAsString)
    {
        return serializer is not null ?
        (serializer.FromString(valueAsString, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
    }
}
