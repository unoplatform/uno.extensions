using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chinook.BackButtonManager;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;

namespace Chinook.DynamicMvvm
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "We prefer to have a more readable file name for extension methods.")]
    public static class ViewModelExtensions
    {
        /// <summary>
        /// Registers a custom back button handler for this ViewModel.
        /// </summary>
        /// <param name="vm">The ViewModel.</param>
        /// <param name="handle">The async handler method.</param>
        /// <param name="canHandle">The optional canHandle function. When null is provided, the default behavior is applied (canHandle returns true only when the current ViewModel is active in the <see cref="ISectionsNavigator"/>).</param>
        /// <param name="handlerName">The optional name of the handler. When null is provided, the ViewModel type name is used.</param>
        /// <param name="priority">The optional priority. (See <see cref="IBackButtonManager.AddHandler(IBackButtonHandler, int?)"/> for more info.)</param>
        public static void RegisterBackHandler(this IViewModel vm, Func<CancellationToken, Task> handle, Func<bool> canHandle = null, string handlerName = null, int? priority = null)
        {
            vm = vm ?? throw new ArgumentNullException(nameof(vm));
            handlerName = handlerName ?? vm.GetType().Name;
            handle = handle ?? throw new ArgumentNullException(nameof(handle));
            canHandle = canHandle ?? DefaultCanHandle;

            var backButtonManager = vm.GetService<IBackButtonManager>();
            var handler = new BackButtonHandler(handlerName, canHandle, handle);
            var registration = backButtonManager.RegisterHandler(handler, priority);

            vm.AddDisposable("BackButtonHandler_" + handlerName, registration);

            bool DefaultCanHandle()
            {
                var navigator = vm.GetService<ISectionsNavigator>();

                // The handler can handle the back only if the associated ViewModel is the one currently active in the navigator.
                return navigator.GetActiveViewModel() == vm;
            }
        }
    }
}
