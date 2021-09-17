using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IViewManager<TControl>
{
    void Show(string path, Type view, object data, object viewModel, bool setFocus);
}
