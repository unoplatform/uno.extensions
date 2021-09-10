using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IControlNavigation : IInjectable
{
    void Navigate(NavigationContext context, bool isBackNavigation, object viewModel);
}

public interface IInjectable
{
    void Inject(object control);
}

