namespace Uno.Extensions.Configuration;

public interface IWritableOptions<T> : IOptionsSnapshot<T>
	where T : class, new()
{
	Task UpdateAsync(Func<T, T> applyChanges);
}
