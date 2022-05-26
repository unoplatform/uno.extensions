namespace Commerce.ViewModels;

public record DealsViewModel(IDealService DealService)
{
	public IFeed<Product[]> Items => Feed.Async(DealService.GetAll);
}
