using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

/// <summary>
/// Fixture for the positional-record shape on the metadata path — the form the reference documentation
/// uses. The service reaches the feed through the record's synthesized property rather than through a
/// constructor-body assignment, which is the case the classifier used to miss.
/// </summary>
public partial record BasketModel(IBasketService Service)
{
	public IListFeed<string> Lines => ListFeed.Async(async ct => await Service.GetLines(ct));
}
