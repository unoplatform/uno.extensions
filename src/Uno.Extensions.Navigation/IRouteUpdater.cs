//using CommunityToolkit.Mvvm.Messaging;
using System;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

internal interface IRouteUpdater
{
	void StartNavigation();

	void EndNavigation();
}

