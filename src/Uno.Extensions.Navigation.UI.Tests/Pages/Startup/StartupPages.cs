using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.Startup;

/// <summary>
/// Counts how many times each startup page and view model is constructed, so a test can tell
/// a page that was built once from one that was built, thrown away and built again.
/// </summary>
public static class StartupCreations
{
	private static readonly Dictionary<string, int> _counts = new();

	public static void Reset() => _counts.Clear();

	public static void Record(object instance)
	{
		var name = instance.GetType().Name;
		_counts[name] = Count(name) + 1;
	}

	public static int Count<T>() => Count(typeof(T).Name);

	public static string Summary()
		=> _counts.Count == 0 ? "(none)" : string.Join(", ", _counts.OrderBy(c => c.Key).Select(c => $"{c.Key}={c.Value}"));

	private static int Count(string name) => _counts.TryGetValue(name, out var count) ? count : 0;
}

public sealed partial class StartupMainPage : Page
{
	public StartupMainPage()
	{
		StartupCreations.Record(this);
		Content = new TextBlock { Text = "Main" };
	}
}

public sealed partial class StartupLoginPage : Page
{
	public StartupLoginPage()
	{
		StartupCreations.Record(this);
		Content = new TextBlock { Text = "Login" };
	}
}

/// <summary>
/// A page hosting a nested region, like a wizard host whose default child is a welcome page.
/// </summary>
public sealed partial class StartupHostPage : Page
{
	public StartupHostPage()
	{
		StartupCreations.Record(this);
		ChildRegion = new ContentControl
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
		};
		Region.SetAttached(ChildRegion, true);
		Content = ChildRegion;
	}

	public ContentControl ChildRegion { get; }
}

public sealed partial class StartupChildPage : Page
{
	public StartupChildPage()
	{
		StartupCreations.Record(this);
		Content = new TextBlock { Text = "Child" };
	}
}

public sealed class StartupShellModel
{
	public StartupShellModel() => StartupCreations.Record(this);
}

public sealed class StartupMainModel
{
	public StartupMainModel() => StartupCreations.Record(this);
}

public sealed class StartupLoginModel
{
	public StartupLoginModel() => StartupCreations.Record(this);
}

public sealed class StartupHostModel
{
	public StartupHostModel() => StartupCreations.Record(this);
}

public sealed class StartupChildModel
{
	public StartupChildModel() => StartupCreations.Record(this);
}
