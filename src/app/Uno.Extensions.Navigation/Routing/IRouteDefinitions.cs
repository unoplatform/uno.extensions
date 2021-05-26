using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
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
    public interface IRouteDefinitions
    {
        IReadOnlyDictionary<string, (Type, Type)> Routes { get; }

        IReadOnlyDictionary<string, Func<IServiceProvider, string[], string, IDictionary<string, object>, string>> Redirections { get; }

        //IReadOnlyDictionary<Type, IRoute> Routes { get;}

        //IReadOnlyDictionary<Type, Type> ViewModelMappings { get; }
    }
}

