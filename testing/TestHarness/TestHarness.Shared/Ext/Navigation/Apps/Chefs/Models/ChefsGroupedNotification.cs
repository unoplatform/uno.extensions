using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public partial record ChefsGroupedNotification
{
	private readonly IImmutableList<ChefsNotification> _all;

	//public ChefsGroupedNotification(IEnumerable<ChefsNotification> notifications)
	//{
	//	_all = notifications.ToImmutableList();
	//	Today = _all.Where(x => x.Date.IsSameDate(DateTime.Today)).ToImmutableList();
	//	Yesterday = _all.Where(x => x.Date.IsSameDate(DateTime.Now.AddDays(-1))).ToImmutableList();
	//	Older = _all.Where(x => x.Date < DateTime.Now.AddDays(-1)).ToImmutableList();
	//}

	public IImmutableList<ChefsNotification> Today { get; }
	public bool HasTodayNotifications => Today.Any();
	public IImmutableList<ChefsNotification> Yesterday { get; }
	public bool HasYesterdayNotifications => Yesterday.Any();
	public IImmutableList<ChefsNotification> Older { get; }
	public bool HasOlderNotifications => Older.Any();

	public IImmutableList<ChefsNotification> GetAll() => _all;
}
