using System;

namespace Uno.Extensions.Navigation.Services;

public interface IControlNavigationServiceFactory
{
    Type ControlType { get; }

    IRegionNavigationService Create(IServiceProvider services);
}
