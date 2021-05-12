using System;
using System.Reactive;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
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
using Windows.UI.Core;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// This class is used for view services.
    /// - Configures view services.
    /// </summary>
    public static class ViewServicesConfiguration
    {
        public static IServiceCollection AddViewServices(this IServiceCollection services)
        {
            return services
                .AddSingleton(s => App.Instance.NavigationFrame.Dispatcher)
                //.AddSingleton(s => Shell.Instance.ExtendedSplashScreen)
                .AddSingleton<IDispatcherScheduler>(s => new MainDispatcherScheduler(
                    s.GetRequiredService<CoreDispatcher>(),
                    CoreDispatcherPriority.Normal
                ));
        }



        public class MainDispatcherScheduler : IDispatcherScheduler, IScheduler
        {
            private readonly IScheduler _coreDispatcherScheduler;

            public DateTimeOffset Now => _coreDispatcherScheduler.Now;

            public MainDispatcherScheduler(CoreDispatcher dispatcher)
            {
                //IL_0016: Unknown result type (might be due to invalid IL or missing references)
                //IL_0020: Expected O, but got Unknown
                if (dispatcher == null)
                {
                    throw new ArgumentNullException("dispatcher");
                }
                _coreDispatcherScheduler = (IScheduler)new CoreDispatcherScheduler(dispatcher);
            }

            public MainDispatcherScheduler(CoreDispatcher dispatcher, CoreDispatcherPriority priority)
            {
                //IL_0017: Unknown result type (might be due to invalid IL or missing references)
                //IL_0021: Expected O, but got Unknown
                if (dispatcher == null)
                {
                    throw new ArgumentNullException("dispatcher");
                }
                _coreDispatcherScheduler = (IScheduler)new CoreDispatcherScheduler(dispatcher, priority);
            }

            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                return _coreDispatcherScheduler.Schedule<TState>(state, action);
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return _coreDispatcherScheduler.Schedule<TState>(state, dueTime, action);
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return _coreDispatcherScheduler.Schedule<TState>(state, dueTime, action);
            }
        }
    }
}
