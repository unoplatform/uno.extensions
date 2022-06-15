namespace Uno.Extensions.Hosting;

public interface IBuilder
{
	IHostBuilder? HostBuilder { get; init; }
}

public interface IBuilder<T> : IBuilder
	where T:new()
{
	T Settings { get; set; }
}
