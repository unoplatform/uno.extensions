namespace Commerce.Business;

public interface ICartService
{
	ValueTask<Cart> Get(CancellationToken ct);
}
