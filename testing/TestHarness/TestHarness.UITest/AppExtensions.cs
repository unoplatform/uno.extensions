namespace TestHarness.UITest;

public static class AppExtensions
{
	private const int UIWaitTimeInMilliseconds = 1000;
	public static async Task TapAndWait(this IApp app, string elementToTap, string elementToWaitFor )
	{
		app.WaitForElement(elementToTap);
		await Task.Delay(UIWaitTimeInMilliseconds);

		app.Tap(elementToTap);
		app.WaitForElement(elementToWaitFor);

		await Task.Delay(UIWaitTimeInMilliseconds);
	}

	public static async Task SelectListViewIndexAndWait(this IApp app, string listName, string indexToSelect, string elementToWaitFor)
	{
		app.WaitForElement(listName);
		await Task.Delay(UIWaitTimeInMilliseconds);

		var list = app.Marked(listName);
		list.SetDependencyPropertyValue("SelectedIndex", indexToSelect);
		await Task.Delay(UIWaitTimeInMilliseconds);

		app.WaitForElement(elementToWaitFor);
		await Task.Delay(UIWaitTimeInMilliseconds);
	}
}
