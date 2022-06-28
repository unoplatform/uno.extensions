
namespace Uno.Extensions.Authentication;

public interface ITokenCache
{
	Task<bool> HasTokenAsync();
	Task SaveAsync(IDictionary<string, string> tokens);
	Task ClearAsync();
	Task<IDictionary<string, string>> GetAsync();
	event EventHandler? Cleared;
}
