using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Uno.Extensions.Hosting;


public class AppHostingEnvironment : HostingEnvironment, IAppHostEnvironment
{
    public string? AppDataPath { get; set; }

    public static AppHostingEnvironment FromHostEnvironment(IHostEnvironment host, string? appDataPath)
    {
        return new AppHostingEnvironment
        {
            AppDataPath = appDataPath ?? host.ContentRootPath,
            ApplicationName = host.ApplicationName,
            ContentRootFileProvider = host.ContentRootFileProvider,
            ContentRootPath = host.ContentRootPath,
            EnvironmentName = host.EnvironmentName
        };
    }
}
