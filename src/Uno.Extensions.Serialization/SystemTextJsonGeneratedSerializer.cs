using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Uno.Extensions.Serialization;

public class SystemTextJsonGeneratedSerializer<T> : ISerializer<T>, IStreamSerializer<T>
{
	public SystemTextJsonGeneratedSerializer(
		ISerializer nonTypedSerializer,
		IStreamSerializer nonTypedStreamSerializer,
		JsonTypeInfo<T>? typeInfo = null)
	{
		_nonTypedSerializer = nonTypedSerializer;
		_nonTypedStreamSerializer = nonTypedStreamSerializer;
		_typeInfo = typeInfo;
	}

	public string ToString(T value)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Serialize(value, _typeInfo);
		}

		return _nonTypedSerializer.ToString(value);
	}
	public T? FromString(string source)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Deserialize(source, _typeInfo);
		}

		return _nonTypedSerializer.FromString<T>(source);
	}
	public string ToString(object value, Type valueType) => _nonTypedSerializer.ToString(value, valueType);
	public object? FromString(string source, Type targetType) => _nonTypedSerializer.FromString(source, targetType);
	public T? ReadFromStream(Stream source)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Deserialize(source, _typeInfo);
		}

		return _nonTypedStreamSerializer.ReadFromStream<T>(source);
	}
	public void WriteToStream(Stream stream, T value)
	{
		if (_typeInfo is not null)
		{
			JsonSerializer.Serialize(stream, value, _typeInfo);
			return;
		}

		_nonTypedStreamSerializer.WriteToStream<T>(stream, value);
	}
	public object? ReadFromStream(Stream source, Type targetType) => _nonTypedStreamSerializer.ReadFromStream(source, targetType);
	public void WriteToStream(Stream stream, object value, Type valueType) => _nonTypedStreamSerializer.WriteToStream(stream, value, valueType);

	private readonly ISerializer _nonTypedSerializer;
	private readonly IStreamSerializer _nonTypedStreamSerializer;
	private readonly JsonTypeInfo<T> _typeInfo;
}
