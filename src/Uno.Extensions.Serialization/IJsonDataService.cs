using System.Threading.Tasks;

namespace Uno.Extensions.Serialization;

public interface IJsonDataService<TData>
{
	string? DataFile { get; set; }
	Task<TData[]?> GetEntities();
}
