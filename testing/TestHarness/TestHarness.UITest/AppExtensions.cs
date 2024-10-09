﻿namespace TestHarness.UITest;

// TODO: Remove commented code if it works in CI
public static class AppExtensions
{
	public const int UIWaitTimeInMilliseconds = 1000;
	public static async Task TapAndWait(this IApp app, string elementToTap, string elementToWaitFor )
	{
		app.WaitElement(elementToTap);
		//await Task.Delay(UIWaitTimeInMilliseconds);

		app.Tap(elementToTap);
		app.WaitElement(elementToWaitFor);

		//await Task.Delay(UIWaitTimeInMilliseconds);
	}

	public static async Task SelectElementIndexAndWait(this IApp app, string listName, string indexToSelect, string elementToWaitFor)
	{
		app.WaitElement(listName);
		//await Task.Delay(UIWaitTimeInMilliseconds);
		var list = new QueryEx(q => q.All().Marked(listName));
		list.SetDependencyPropertyValue("SelectedIndex", indexToSelect);
		//await Task.Delay(UIWaitTimeInMilliseconds);

		app.WaitElement(elementToWaitFor);
		//await Task.Delay(UIWaitTimeInMilliseconds);
	}
}
