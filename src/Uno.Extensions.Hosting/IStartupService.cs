using System.Threading.Tasks;

namespace Uno.Extensions.Hosting;

public interface IStartupService
{
	Task StartupComplete();
}
