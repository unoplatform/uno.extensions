namespace Uno.Extensions.Hosting.Internal;

internal interface IConfigureContainerAdapter
{
	void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder);
}
