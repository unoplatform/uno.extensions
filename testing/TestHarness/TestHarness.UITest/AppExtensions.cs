namespace TestHarness.UITest;

public static class AppExtensions
{
	public const int UIWaitTimeInMilliseconds = 1000;
	public static async Task TapAndWait(this IApp app, string elementToTap, string elementToWaitFor )
	{
		app.WaitElement(elementToTap);
		await Task.Delay(UIWaitTimeInMilliseconds);

		app.Tap(elementToTap);
		app.WaitElement(elementToWaitFor);

		await Task.Delay(UIWaitTimeInMilliseconds);
	}

	public static async Task SelectListViewIndexAndWait(this IApp app, string listName, string indexToSelect, string elementToWaitFor)
	{
		app.WaitElement(listName);
		await Task.Delay(UIWaitTimeInMilliseconds);
		var list = new QueryEx(q => q.All().Marked(listName));
		list.SetDependencyPropertyValue("SelectedIndex", indexToSelect);
		await Task.Delay(UIWaitTimeInMilliseconds);

		app.WaitElement(elementToWaitFor);
		await Task.Delay(UIWaitTimeInMilliseconds);
	}
}
