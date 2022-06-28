namespace Uno.Extensions.Hosting;

public interface IAppHostEnvironment : IHostEnvironment
{
    public string? AppDataPath { get; }
}
