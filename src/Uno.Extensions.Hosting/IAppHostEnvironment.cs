using System.Reflection;

namespace Uno.Extensions.Hosting;

public interface IAppHostEnvironment : IHostEnvironment
{
    public string? AppDataPath { get; }
	public Assembly? HostAssembly { get; }
}
