namespace MyExtensionsApp.UITests;

public class Given_MainPage : TestBase
{
	[Test]
	public void When_SmokeTest()
	{
		// NOTICE
		// To run UITests, Run the WASM target without debugger. Note
		// the port that is being used and update the Constants.cs file
		// in the UITests project with the correct port number.

#if (useExtensionsNavigation)
		// Query for the SecondPageButton and then tap it
		Query xamlButton = q => q.All().Marked("SecondPageButton");
		App.WaitForElement(xamlButton);
		App.Tap(xamlButton);

		// Take a screenshot and add it to the test results
		TakeScreenshot("After tapped");
#else
		// Query for the MainPage Text Block
		Query textBlock = q => q.All().Marked("Hello Uno Platform");
		App.WaitForElement(textBlock);

		// Take a screenshot and add it to the test results
		TakeScreenshot("After launch");
#endif
	}
}
