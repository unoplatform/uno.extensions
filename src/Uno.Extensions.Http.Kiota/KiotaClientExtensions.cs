using Microsoft.Kiota.Abstractions.Serialization;

namespace Uno.Extensions.Http.Kiota;

/// <summary>
/// Provides helpers to read and deserialize Kiota HTTP response streams.
/// </summary>
public static class KiotaStreamExtensions
{
	/// <summary>
	/// Awaits a <see cref="Task{Stream}"/>
	/// and deserializes the JSON payload into a single <typeparamref name="T"/> instance.
	/// </summary>
	/// <returns>
	/// The deserialized <typeparamref name="T"/>.
	/// </returns>
	public static async Task<T?> DeserializeResponseAsync<T>(
		this Task<Stream> streamTask,
		CancellationToken cancellationToken = default)
		where T : IParsable, new()
	{
		await using var stream = await streamTask.ConfigureAwait(false);
		using var reader = new StreamReader(stream);
		var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
		return await KiotaJsonSerializer
			.DeserializeAsync<T>(json, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Awaits a <see cref="Task{Stream}"/>
	/// and deserializes the JSON payload into a concrete <see cref="List{T}"/>.
	/// </summary>
	/// <returns>
	/// A <see cref="IList{T}"/> of deserialized items.
	/// </returns>
	public static async Task<IList<T>> DeserializeCollectionResponseAsync<T>(
		this Task<Stream> streamTask,
		CancellationToken cancellationToken = default)
		where T : IParsable, new()
	{
		await using var stream = await streamTask.ConfigureAwait(false);
		using var reader = new StreamReader(stream);
		var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

		var deserializedCollection = await KiotaJsonSerializer
			.DeserializeCollectionAsync<T>(json, cancellationToken)
			.ConfigureAwait(false);

		return deserializedCollection.ToList();
	}
}
