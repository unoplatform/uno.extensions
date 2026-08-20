using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_MockCommand_UI : FeedTests
{
	[TestMethod]
	public async Task When_CommandMockedExecuting_Then_BoundButtonIsDisabled()
	{
		await using var vm = Given_FeedView_Mocks.MockedPageViewModel.CreateMock(m => m.Save = MockCommand.Executing());
		var button = CreateBoundButton(vm);

		await UIHelper.Load(button, CT);

		await UIHelper.WaitFor(() => !button.IsEnabled, CT);
	}

	[TestMethod]
	public async Task When_CommandMockedDisabled_Then_BoundButtonIsDisabled()
	{
		await using var vm = Given_FeedView_Mocks.MockedPageViewModel.CreateMock(m => m.Save = MockCommand.Disabled());
		var button = CreateBoundButton(vm);

		await UIHelper.Load(button, CT);

		await UIHelper.WaitFor(() => !button.IsEnabled, CT);
	}

	[TestMethod]
	public async Task When_CommandNotMocked_Then_BoundButtonIsEnabled()
	{
		// An unconfigured command defaults to an enabled no-op.
		await using var vm = Given_FeedView_Mocks.MockedPageViewModel.CreateMock();
		var button = CreateBoundButton(vm);

		await UIHelper.Load(button, CT);

		await UIHelper.WaitFor(() => button.IsEnabled, CT);
	}

	private static Button CreateBoundButton(Given_FeedView_Mocks.MockedPageViewModel vm)
	{
		var button = new Button { Content = "Save", DataContext = vm };
		button.SetBinding(Button.CommandProperty, new Binding { Path = new PropertyPath("Save") });
		return button;
	}
}
