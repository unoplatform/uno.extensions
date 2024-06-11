namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsNotification
{
	//internal ChefsNotification(NotificationData notificationData)
	//{
	//	Title = notificationData.Title;
	//	Description = notificationData.Description;
	//	Read = notificationData.Read;
	//	Date = notificationData.Date;
	//}

	public string? Title { get; init; }
	public string? Description { get; init; }
	public bool Read { get; init; }
	public DateTime Date { get; init; }
}
