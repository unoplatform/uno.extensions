namespace Commerce.Data;

public interface ICartEndpoint
{
	ValueTask<CartData> Get(CancellationToken ct);
}
