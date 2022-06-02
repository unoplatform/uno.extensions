
namespace TestHarness.UITest;

public static class FluentExtensions
{
	public static T Apply<T>(this T target, Action<T> action)
	{
		action?.Invoke(target);

		return target;
	}

	public static T ApplyIf<T>(this T target, bool condition, Action<T>? action)
	{
		if (condition)
		{
			action?.Invoke(target);
		}

		return target;
	}

	public static void FailWithText(this AssertionScope scope, string text)
	{
		var t = text.Replace("{", "{{").Replace("}", "}}");

		scope.FailWith(t);
	}
}
