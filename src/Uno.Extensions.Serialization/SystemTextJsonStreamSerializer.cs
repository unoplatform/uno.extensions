namespace Uno.Extensions.Serialization;

public class SystemTextJsonStreamSerializer : ISerializer, IStreamSerializer
{
	private readonly JsonSerializerOptions? _serializerOptions;
	private readonly IServiceProvider _services;

	private IJsonTypeInfoWrapper? TypedSerializer(Type jsonType) => _services.GetServices<IJsonTypeInfoWrapper>().FirstOrDefault(x => x.JsonType == jsonType);

	public SystemTextJsonStreamSerializer(IServiceProvider services, JsonSerializerOptions? serializerOptions = null)
	{
		_services = services;
		_serializerOptions = serializerOptions;
	}

	public object? ReadFromStream(Stream source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		return typedSerializer is not null ? typedSerializer.ReadFromStream(source, targetType) : JsonSerializer.Deserialize(source, targetType, _serializerOptions);
	}

	public void WriteToStream(Stream stream, object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		if (typedSerializer is not null)
		{
			typedSerializer.WriteToStream(stream, value);
		}
		else
		{
			JsonSerializer.Serialize(stream, value, valueType, _serializerOptions);
		}
	}

	public string ToString(object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		return typedSerializer is not null ? typedSerializer.ToString(value, valueType) : JsonSerializer.Serialize(value, valueType, _serializerOptions);
	}

	public object? FromString(string source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		return typedSerializer is not null ? typedSerializer.FromString(source, targetType) : JsonSerializer.Deserialize(source, targetType, _serializerOptions);
	}
}

public class SystemTextJsonStreamSerializer<T> : SystemTextJsonStreamSerializer, ISerializer<T>, IStreamSerializer<T>
{
	public SystemTextJsonStreamSerializer(
		IServiceProvider services,
		JsonSerializerOptions? serializerOptions = null) : base(services, serializerOptions)
	{
	}

	public T? FromString(string source)
	{
		return FromString(source, typeof(T)) is T value ? value : default;
	}

	public T? ReadFromStream(Stream source)
	{
		return ReadFromStream(source, typeof(T)) is T value ? value : default;
	}

	public string ToString(T value)
	{
		if (value is null)
		{
			return String.Empty;
		}

		return ToString(value, typeof(T));
	}

	public void WriteToStream(Stream stream, T value)
	{
		if (value is null)
		{
			return;
		}

		WriteToStream(stream, value, typeof(T));
	}
}
