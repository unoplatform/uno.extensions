using Microsoft.Extensions.Hosting;

namespace TestHarness.Ext.Navigation.PageNavigation;

public interface IHostInitialization
{
	IHost InitializeHost();
}
