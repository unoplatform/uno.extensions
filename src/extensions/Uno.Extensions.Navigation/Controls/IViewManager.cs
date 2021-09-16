using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IViewManager<TControl> : IInjectable
{
    void ChangeView(INavigationService navigation, string path, Type view, bool isBackNavigation, object data, object viewModel, bool setFocus);
}
