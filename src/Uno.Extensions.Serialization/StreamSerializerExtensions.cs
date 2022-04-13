namespace Uno.Extensions.Serialization;

public static class StreamSerializerExtensions
{
	public static T? FromStream<T>(this IStreamSerializer serializer, Stream stream)
	{
		return (serializer.FromStream(stream, typeof(T)) is T tvalue) ?
					tvalue :
					default;
	}

	public static IStreamSerializer ToStream<T>(this IStreamSerializer serializer, Stream stream, T value)
	{
		if (value is not null)
		{
			serializer.ToStream(stream, value, typeof(T));
		}
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

	internal static async Task<TData?> ReadFromFile<TData>(this IStreamSerializer serializer, IStorage storage, string dataFile)
	{
		using var stream = await storage.OpenApplicationFileAsync(dataFile);
		return serializer.FromStream<TData>(stream);
	}
}
