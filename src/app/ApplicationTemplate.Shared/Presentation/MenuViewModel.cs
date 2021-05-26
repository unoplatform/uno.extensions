using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;
//using Chinook.StackNavigation;
using Uno;
using Uno.Extensions.Navigation;

namespace ApplicationTemplate.Presentation
{
    public partial class MenuViewModel : ViewModel
    {

        private IRouteMessenger Messenger { get; }

        public MenuViewModel(IRouteMessenger messenger)
        {
            Messenger = messenger;
        }

        /// <summary>
        /// The list of ViewModel types on which the bottom menu should be visible.
        /// </summary>
        private Type[] _viewModelsWithBottomMenu = new Type[]
        {
            typeof(HomePageViewModel),
            typeof(PostsPageViewModel),
            typeof(SettingsPageViewModel),
        };

        //public IDynamicCommand ShowHome => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Home", () => new HomePageViewModel());
        //});
        public ICommand ShowHome => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(HomePageViewModel).AsRoute())));

        //public IDynamicCommand ShowPosts => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Posts", () => new PostsPageViewModel());
        //});
        public ICommand ShowPosts => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(PostsPageViewModel).AsRoute())));

        //public IDynamicCommand ShowSettings => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Settings", () => new SettingsPageViewModel());
        //});
        public ICommand ShowSettings => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(SettingsPageViewModel).AsRoute())));


        public string MenuState => "Open";
        //public string MenuState
        //{
        //    get => this.GetFromObservable(ObserveMenuState(), initialValue: "Closed");
        //}

        //private IObservable<string> ObserveMenuState()
        //{
        //    return this.GetService<ISectionsNavigator>()
        //        .ObserveCurrentState()
        //        .Select(state =>
        //        {
        //            var vmType = GetViewModelType(state);
        //            return _viewModelsWithBottomMenu.Contains(vmType) ? "Open" : "Closed";
        //        })
        //        .DistinctUntilChanged()
        //        // On iOS, when Visual states are changed too fast, they break. This is a workaround for this bug.
        //        .ThrottleOrImmediate(TimeSpan.FromMilliseconds(350), Scheduler.Default);

        //    Type GetViewModelType(SectionsNavigatorState currentState)
        //    {
        //        switch (currentState.LastRequestState)
        //        {
        //            case NavigatorRequestState.Processing:
        //                return currentState.GetNextViewModelType();
        //            case NavigatorRequestState.Processed:
        //            case NavigatorRequestState.FailedToProcess:
        //                return currentState.GetLastViewModelType();
        //            default:
        //                throw new NotSupportedException($"The request state {currentState.LastRequestState} is not supported.");
        //        }
        //    }
        //}
    }
}
