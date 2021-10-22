using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtensionsSampleApp.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;

namespace ExtensionsSampleApp.ViewModels
{
    public class SecondViewModel : IViewModelStart, IViewModelStop
    {
       
        public string Title => "Second - " + Data;
        private Widget Data;
        private ILogger Logger { get; }
        public SecondViewModel(ILogger<SecondViewModel> logger, INavigator nav, Widget data)
        {
            Logger = logger;
            Data = data;
        }

        public async Task Start(NavigationRequest request)
        {
            Logger.LogTraceMessage("Starting view model (delay 5s)");
            try
            {
                await Task.Delay(5000, request.Cancellation ?? default);
                Logger.LogTraceMessage("View model started (5s delay completed)");
            }
            catch (Exception ex)
            {
                Logger.LogTraceMessage("View model started (5s delay interrupted - nav cancelled)");
            }
        }

        public async Task<bool> Stop(NavigationRequest request)
        {
            if ((request.Route.Base == typeof(ThirdPage).Name ||
                request.Route.Base == typeof(ThirdPage).Name.Replace("Page", "")) &&
                !((request.Route.Data as IDictionary<string, object>)?.Any() ?? false))
            {
                return false;
            }
            return true;
        }
    }


}
