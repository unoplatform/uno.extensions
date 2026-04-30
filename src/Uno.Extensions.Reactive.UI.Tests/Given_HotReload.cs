#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_HotReload
{
	[TestInitialize]
	public void Setup()
	{
		// Allow more time for the dev-server to load the Roslyn workspace (solution can be large)
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		// Allow more time for the first metadata update (delta compilation can be slow on CI)
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);
	}

	[TestMethod]
	public async Task When_UpdateMethodBody_Then_MetadataUpdateReceived(CancellationToken ct)
	{
		Assert.AreEqual("original", HotReloadTarget.GetValue());

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		Assert.AreEqual("updated", HotReloadTarget.GetValue());
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateMvuxFeedSource_Then_NewViewModelReflectsUpdate(CancellationToken ct)
	{
		var vm = new MvuxHotReloadViewModel();
		var text = new TextBlock();
		var ui = new StackPanel { DataContext = vm, Children = { text } };
		text.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui, default);
		await TestHelper.WaitFor(() => text.Text == "original", default);

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// New ViewModel instance picks up the HR'd method body
		var vm2 = new MvuxHotReloadViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, default);
		await TestHelper.WaitFor(() => text2.Text == "updated", default);

		Assert.AreEqual("updated", text2.Text);
	}
}
#endif
