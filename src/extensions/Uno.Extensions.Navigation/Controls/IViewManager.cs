using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IViewManager
{
    void Show(string path, Type viewType, object data, object viewModel);
}
