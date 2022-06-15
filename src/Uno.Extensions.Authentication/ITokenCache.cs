
namespace Uno.Extensions.Authentication;

public interface ITokenCache
{
	Task Save(IDictionary<string, string> tokens);
	Task Clear();
	Task<IDictionary<string, string>> Get();
	event EventHandler? Cleared;
}
