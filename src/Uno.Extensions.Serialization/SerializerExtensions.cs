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

	public static T? FromStream<T>(this ISerializer serializer, Stream stream)
	{
		return (serializer.FromStream(stream, typeof(T)) is T tvalue) ?
					tvalue :
					default;
	}

	public static ISerializer ToStream<T>(this ISerializer serializer, Stream stream, T value)
	{
		if (value is not null)
		{
			serializer.ToStream(stream, value, typeof(T));
		}
		return serializer;
	}

	public static Stream? ToStream<T>(this ISerializer serializer, T value)
	{
		if (value is not null)
		{
			return serializer.ToStream(value, typeof(T));
		}

		return default;
	}

	public static Stream ToStream(this ISerializer serializer, object value, Type valueType)
	{
		var memoryStream = new MemoryStream();

		serializer?.ToStream(memoryStream, value, valueType);
		memoryStream.Seek(0, SeekOrigin.Begin);

		return memoryStream;
	}
}
