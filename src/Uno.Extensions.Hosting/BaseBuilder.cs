namespace Uno.Extensions.Hosting;

public record BaseBuilder : IBuilder
{
	public IHostBuilder? HostBuilder { get; init; }
}

public record BaseBuilder<T> : BaseBuilder, IBuilder<T>
	where T : new()
{
	public T Settings { get; set; } = new T();
}
