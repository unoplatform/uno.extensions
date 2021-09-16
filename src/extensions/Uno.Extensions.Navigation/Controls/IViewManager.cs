using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IViewManager<TControl>
{
    void ChangeView(string path, Type view, bool isBackNavigation, object data, object viewModel, bool setFocus);
}
