
namespace Uno.Extensions.Authentication;

public interface ITokenRepository
{
	Task Save(IDictionary<string, string> tokens);
	Task Clear();
	Task<IDictionary<string, string>> Get();
	event EventHandler? Cleared;
}
