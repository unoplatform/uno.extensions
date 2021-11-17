//using CommunityToolkit.Mvvm.Messaging;
using System;

namespace Uno.Extensions.Navigation;

public interface IRouteNotifier
{
	event EventHandler RouteChanged;
}

