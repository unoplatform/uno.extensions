namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public partial record ChefsDialogInfo
{
	public ChefsDialogInfo(string title, string content)
	{
		Title = title;
		Content = content;
	}

	public string Title { get; init; }
	public string Content { get; init; }
}
