namespace Uno.Extensions.Configuration;

/// <summary>
/// This implementation of <see cref="IOptionsSnapshot{T}"/> extends
/// the read-only pattern with a method to update the settings.
/// </summary>
/// <typeparam name="T">Options type.</typeparam>
public interface IWritableOptions<T> : IOptionsSnapshot<T>
	where T : class, new()
{
	/// <summary>
	/// Writes added or modified option values to the underlying source.
	/// </summary>
	/// <param name="applyChanges">
	/// A function that returns a new instance of the options class containing updated values.
	/// </param>
	/// <returns>An instance of T.</returns>
	Task UpdateAsync(Func<T, T> applyChanges);
}
