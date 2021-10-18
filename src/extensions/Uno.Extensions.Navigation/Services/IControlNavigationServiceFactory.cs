using System;

namespace Uno.Extensions.Navigation.Services;

public interface IControlNavigationServiceFactory
{
    Type ControlType { get; }

    INavigationService Create(IServiceProvider services);
}
