namespace Uno.Extensions.Serialization;

public static class StreamSerializerExtensions
{
    public static T? ReadFromStream<T>(this IStreamSerializer serializer, Stream stream)
    {
        return serializer is not null ?
            (serializer.FromStream(stream, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
    }

    public static T? FromStream<T>(this IStreamSerializer serializer, Stream stream)
    {
        if (stream == null)
        {
            return default;
        }

        var pos = stream.Position;
        var value = serializer is not null ?
            (serializer.FromStream(stream, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
        stream.Seek(pos, SeekOrigin.Begin);
        return value;
    }

    public static IStreamSerializer WriteToStream<T>(this IStreamSerializer serializer, Stream stream, T value)
    {
        if (value is not null)
        {
            serializer.ToStream(stream, value, typeof(T));
        }
        return serializer;
    }

    public static IStreamSerializer WriteToStream(this IStreamSerializer serializer, Stream stream, object value)
    {
        serializer.ToStream(stream, value, value.GetType());
        return serializer;
    }

    public static Stream? ToStream<T>(this IStreamSerializer serializer, T value)
    {
        if (value is not null)
        {
            return serializer.ToStream(value, typeof(T));
        }

        return default;
    }

    public static Stream ToStream(this IStreamSerializer serializer, object value, Type valueType)
    {
        var memoryStream = new MemoryStream();

        serializer?.ToStream(memoryStream, value, valueType);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

	internal static async Task<TData?> ReadFromFile<TData>(this IStreamSerializer serializer, IStorageProxy storage, string dataFile)
	{
		using var stream = await storage.OpenApplicationFile(dataFile);
		return serializer.FromStream<TData>(stream);
	}
}
