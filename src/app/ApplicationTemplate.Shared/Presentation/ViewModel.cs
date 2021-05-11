using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;
using Microsoft.Extensions.DependencyInjection;
using Uno;

namespace ApplicationTemplate.Presentation
{
    /// <summary>
    /// ViewModel to use as a base for all other ViewModels.
    /// </summary>
    public class ViewModel : ViewModelBase, INavigableViewModel
    {
        // Add properties or commands you want to have on all your ViewModels

        public ViewModel()
        {
            (this as IInjectable)?.Inject((t, n) => this.GetService(t));
        }

        public IDynamicCommand NavigateBack => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct);
        });

        public IDynamicCommand CloseModal => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<ISectionsNavigator>().CloseModal(ct);
        });

        void INavigableViewModel.SetView(object view)
        {
            //-:cnd:noEmit
#if WINDOWS_UWP || __IOS__ || __ANDROID__ || __WASM__
            //+:cnd:noEmit
            var frameworkElement = view as Windows.UI.Xaml.FrameworkElement;
            View = new ViewModelView(frameworkElement);
            //-:cnd:noEmit
#endif
            //+:cnd:noEmit
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> on the dispatcher.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/>.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task RunOnDispatcher(CancellationToken ct, Func<CancellationToken, Task> action)
        {
            await this.GetService<IDispatcherScheduler>().Run(ct2 => action(ct2), ct);
        }
    }
}
