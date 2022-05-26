namespace Commerce.Business;

public interface IDealService
{
	ValueTask<Product[]> GetAll(CancellationToken ct);
}
