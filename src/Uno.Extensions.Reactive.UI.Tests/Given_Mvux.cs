using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_Mvux
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Feed_Loads_Data()
	{
		var vm = new MvuxFeedViewModel();
		var nameText = new TextBlock();
		var idText = new TextBlock();
		var feedView = new FeedView();

		var ui = new StackPanel
		{
			DataContext = vm,
			Children =
			{
				feedView,
				nameText,
				idText,
			}
		};

		feedView.SetBinding(FeedView.SourceProperty, new Binding { Path = new PropertyPath("CurrentItem") });
		nameText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentItem.Name") });
		idText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentItem.Id") });

		await UIHelper.Load(ui, default);

		await TestHelper.WaitFor(() => nameText.Text == "Test Item", default);

		nameText.Text.Should().Be("Test Item");
		idText.Text.Should().Be("1");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ListFeed_Loads_Collection()
	{
		var vm = new MvuxListFeedViewModel();
		var lv = new ListView();

		var ui = new StackPanel
		{
			DataContext = vm,
			Children = { lv }
		};

		lv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });

		await UIHelper.Load(ui, default);

		await TestHelper.WaitFor(() => lv.Items.Count == 3, default);

		lv.Items.Count.Should().Be(3);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_State_Mutated_Via_Command()
	{
		var vm = new MvuxStateViewModel();
		var counterText = new TextBlock();
		var incrementBtn = new Button();

		var ui = new StackPanel
		{
			DataContext = vm,
			Children = { counterText, incrementBtn }
		};

		counterText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Counter") });
		incrementBtn.SetBinding(ButtonBase.CommandProperty, new Binding { Path = new PropertyPath("IncrementCounter") });

		await UIHelper.Load(ui, default);

		await TestHelper.WaitFor(() => counterText.Text == "0", default);
		counterText.Text.Should().Be("0");

		incrementBtn.Command.Execute(null);
		await TestHelper.WaitFor(() => counterText.Text == "1", default);
		counterText.Text.Should().Be("1");

		incrementBtn.Command.Execute(null);
		await TestHelper.WaitFor(() => counterText.Text == "2", default);
		counterText.Text.Should().Be("2");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_State_TwoWay_Binding()
	{
		var vm = new MvuxStateViewModel();
		var nameDisplay = new TextBlock();
		var nameInput = new TextBox();

		var ui = new StackPanel
		{
			DataContext = vm,
			Children = { nameDisplay, nameInput }
		};

		nameDisplay.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Name") });
		nameInput.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

		await UIHelper.Load(ui, default);

		await TestHelper.WaitFor(() => nameDisplay.Text == "Initial Item", default);
		nameDisplay.Text.Should().Be("Initial Item");

		nameInput.Text = "Updated Name";
		await TestHelper.WaitFor(() => nameDisplay.Text == "Updated Name", default);
		nameDisplay.Text.Should().Be("Updated Name");
	}
}
