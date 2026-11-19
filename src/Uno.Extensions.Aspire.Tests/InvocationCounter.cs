namespace Uno.Extensions.Aspire.Tests;

internal sealed class InvocationCounter
{
	private int _count;

	public int Count => _count;

	public void Increment() => Interlocked.Increment(ref _count);
}
