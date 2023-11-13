using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Playground.ViewModels;

public class SimpleViewModel
{
	private INavigator Navigator { get; }

	public ICommand AddCommand { get; }

	public string? Name { get; set; }

	public SimpleViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		AddCommand = new AsyncRelayCommand(Add);
	}

	public async Task Add()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: new Widget { Name = Name });
	}
}
