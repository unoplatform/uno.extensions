﻿
namespace TestHarness.UITest;

public static class QueryExtensions
{
#if !IS_RUNTIME_UI_TESTS
	/// <summary>
	/// Wait for element to have the expected value for its Text property.
	/// </summary>
	public static void WaitForText(this IApp app, QueryEx element, string expectedText) =>
		app.WaitForDependencyPropertyValue(element, "Text", expectedText);

	/// <summary>
	/// Wait for element to be available and to have the expected value for its Text property.
	/// </summary>
	public static void WaitForText(this IApp app, string elementName, string expectedText, TimeSpan? timeout = null)
	{
		var element = app.MarkedAnywhere(elementName);
		app.WaitForElement(element, timeout: timeout);
		app.WaitForText(element, expectedText);
	}

	/// <summary>
	/// Wait for the named element to have received focus (ie after being tapped).
	/// </summary>
	public static void WaitForFocus(this IApp app, string elementName)
	{
		var element = app.MarkedAnywhere(elementName);
		app.WaitForElement(element);
		app.WaitForDependencyPropertyValue(element, "FocusState", "Pointer");
	}

	/// <summary>
	/// Calls the <see cref="IApp.WaitForElement(string, string, TimeSpan?, TimeSpan?, TimeSpan?)"/> method with a timeout message that specifies
	/// the element name, which is useful when multiple elements are waited upon in the same test.
	/// </summary>
	public static IAppResult[] WaitForElementWithMessage(this IApp app, string elementName, string? additionalMessage = default)
	{
		var timeoutMessage = $"Timed out waiting for element '{elementName}'";

		if (additionalMessage != null)
		{
			timeoutMessage = $"{timeoutMessage} - {additionalMessage}";
		}

		return app.WaitForElement(elementName, timeoutMessage: timeoutMessage);
	}
#endif

	/// <summary>
	/// ONLY checks the visibility property on the specified element
	/// This doesn't validate that the element is visible on the screen
	/// </summary>
	public static bool IsVisible(this QueryEx element)
	{
		var isVisible = element.GetDependencyPropertyValue("Visibility");
		return isVisible is null ||
			isVisible.ToString() == "Visible";
	}

	/// <summary>
	/// Get the value of the Text property for an element.
	/// </summary>
	public static string GetText(this QueryEx element) => element.GetDependencyPropertyValue<string>("Text");

	/// <summary>
	/// Get the value of the Text property for an element, once it's available.
	/// </summary>
	public static string GetText(this IApp app, string elementName)
	{
		var element = app.MarkedAnywhere(elementName);
		app.WaitForElement(element);
		return element.GetText();
	}

	/// <summary>
	/// Get bounds rect for an element.
	/// </summary>
	[Obsolete("Use _app.GetPhysicalRect() or _app.GetLogicalRect() to clarify your needs.")]
	public static IAppRect GetRect(this IApp app, string elementName)
	{
		return app.WaitForElement(elementName).Single().Rect;
	}
	[Obsolete("Use _app.GetPhysicalRect() or _app.GetLogicalRect() to clarify your needs.")]
	public static IAppRect GetRect(this IApp app, QueryEx query)
	{
		return app.WaitForElement(query).Single().Rect;
	}
	[Obsolete("Use _app.GetPhysicalRect() or _app.GetLogicalRect() to clarify your needs.")]
	public static IAppRect GetRect(this IApp app, Func<IAppQuery, IAppQuery> query)
	{
		return app.WaitForElement(query).Single().Rect;
	}

#pragma warning disable 618 // Disable [Obsolete] warnings
	/// <summary>
	/// This will return a Rect in the physical coordinates space (same as screenshots)
	/// </summary>
	public static IAppRect GetPhysicalRect(this IApp app, string elementName) => ToPhysicalRect(app, app.GetRect(elementName));

	/// <summary>
	/// This will return a Rect in the physical coordinates space (same as screenshots)
	/// </summary>
	public static IAppRect GetPhysicalRect(this IApp app, QueryEx query) => ToPhysicalRect(app, app.GetRect(query));

	/// <summary>
	/// This will return a Rect in the physical coordinates space (same as screenshots)
	/// </summary>
	public static IAppRect GetPhysicalRect(this IApp app, Func<IAppQuery, IAppQuery> query) => ToPhysicalRect(app, app.GetRect(query));

	/// <summary>
	/// This will return a Rect in the logical coordinates space (same as XAML size units)
	/// </summary>
	public static IAppRect GetLogicalRect(this IApp app, string elementName) => ToLogicalRect(app, app.GetRect(elementName));

	/// <summary>
	/// This will return a Rect in the logical coordinates space (same as XAML size units)
	/// </summary>
	public static IAppRect GetLogicalRect(this IApp app, QueryEx query) => ToLogicalRect(app, app.GetRect(query));

	/// <summary>
	/// This will return a Rect in the logical coordinates space (same as XAML size units)
	/// </summary>
	public static IAppRect GetLogicalRect(this IApp app, Func<IAppQuery, IAppQuery> query) => ToLogicalRect(app, app.GetRect(query));

#pragma warning restore 618
	// ************************
	// Physical vs Logical Rect
	// ************************
	//
	// On Android. _app.GetRect() will return values in the PHYSICAL coordinate space.
	// On iOS, _app.GetRect() will return values in the LOGICAL coordinate space.
	// On Wasm (Browser), _app.GetRect() will be 1:1 for PHYSICAL:LOGICAL, so no need to convert.
	//
	// ************************
	private static IAppRect ToPhysicalRect(IApp app, IAppRect rect)
	{
#if IS_RUNTIME_UI_TESTS
		return rect;
#else
		return AppInitializer.GetLocalPlatform() switch
		{
			Platform.Android => rect,
			Platform.iOS => rect.LogicalToPhysicalPixels(app),
			Platform.Browser => rect,
			_ => throw new InvalidOperationException("Unknown current platform.")
		};
#endif
	}

	private static IAppRect ToLogicalRect(IApp app, IAppRect rect)
	{
#if IS_RUNTIME_UI_TESTS
		return rect;
#else
		return AppInitializer.GetLocalPlatform() switch
		{
			Platform.Android => rect.PhysicalToLogicalPixels(app),
			Platform.iOS => rect,
			Platform.Browser => rect,
			_ => throw new InvalidOperationException("Unknown current platform.")
		};
#endif
	}

	public static void FastTap(this IApp app, string elementName)
	{
		var tapPosition = app.GetLogicalRect(elementName);
		app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
	}

	public static void FastTap(this IApp app, QueryEx query)
	{
		var tapPosition = app.GetLogicalRect(query);
		app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
	}

	public static void FastTap(this IApp app, Func<IAppQuery, IAppQuery> query)
	{
		var tapPosition = app.GetLogicalRect(query);
		app.TapCoordinates(tapPosition.CenterX, tapPosition.CenterY);
	}

	public static QueryEx FastTap(this QueryEx query)
	{
		Helpers.App.FastTap(query);
		return query;
	}

	public static FileInfo GetInAppScreenshot(this IApp app)
	{
		var byte64Image = app.InvokeGeneric("browser:SampleRunner|GetScreenshot", "0")?.ToString() ?? string.Empty;

		var array = Convert.FromBase64String(byte64Image);

		var outputFile = Path.GetTempFileName();
		File.WriteAllBytes(outputFile, array);

		var finalPath = Path.ChangeExtension(outputFile, ".png");

		File.Move(outputFile, finalPath);

		return new FileInfo(finalPath);
	}
}
