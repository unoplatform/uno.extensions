using System;

namespace Uno.Extensions.Navigation.Adapters;

public interface IAdapterFactory
{
    Type ControlType { get; }

    INavigationAdapter Create();
}
