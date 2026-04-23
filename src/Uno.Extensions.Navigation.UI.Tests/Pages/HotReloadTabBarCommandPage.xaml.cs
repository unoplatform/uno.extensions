using System;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML page for testing #2912: re-adding a Button Command binding via HR breaks TabBar navigation.
/// The page has a TabBar with Region.Attached and a Button with a Command binding.
/// </summary>
public sealed partial class HotReloadTabBarCommandPage : Page
{
	public HotReloadTabBarCommandPage()
	{
		this.InitializeComponent();
		DataContext = this;
	}

	public Grid ContentGrid => (Grid)FindName("_contentGrid");
	public TabBar TabBar => (TabBar)FindName("TB");
	public Button NavButton => (Button)FindName("_navButton");

	/// <summary>
	/// Simple no-op command. The bug is about the presence of the Command binding
	/// disrupting TabBar navigation after HR remove/re-add, not about what the command does.
	/// </summary>
	public ICommand TestCommand { get; } = new NoOpCommand();

	private sealed class NoOpCommand : ICommand
	{
#pragma warning disable CS0067 // event is never used
		public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
		public bool CanExecute(object? parameter) => true;
		public void Execute(object? parameter) { }
	}
}
