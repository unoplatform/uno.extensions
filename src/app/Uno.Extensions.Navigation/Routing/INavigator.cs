﻿using System;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace Uno.Extensions.Navigation
{
    public interface INavigator
    {
        void Navigate(Type destinationPage, object? viewModel=null);
        void GoBack();
        void Clear();
    }
}

