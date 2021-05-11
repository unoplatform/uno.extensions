using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;
using Uno;

namespace ApplicationTemplate.Presentation
{
    public partial class MenuViewModel : ViewModel
    {
        /// <summary>
        /// The list of ViewModel types on which the bottom menu should be visible.
        /// </summary>
        private Type[] _viewModelsWithBottomMenu = new Type[]
        {
            typeof(HomePageViewModel),
            typeof(PostsPageViewModel),
            typeof(SettingsPageViewModel),
        };

        public IDynamicCommand ShowHome => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Home", () => new HomePageViewModel());
        });

        public IDynamicCommand ShowPosts => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Posts", () => new PostsPageViewModel());
        });

        public IDynamicCommand ShowSettings => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<ISectionsNavigator>().SetActiveSection(ct, "Settings", () => new SettingsPageViewModel());
        });

        public string MenuState
        {
            get => this.GetFromObservable(ObserveMenuState(), initialValue: "Closed");
        }

        private IObservable<string> ObserveMenuState()
        {
            return this.GetService<ISectionsNavigator>()
                .ObserveCurrentState()
                .Select(state =>
                {
                    var vmType = GetViewModelType(state);
                    return _viewModelsWithBottomMenu.Contains(vmType) ? "Open" : "Closed";
                })
                .DistinctUntilChanged()
                // On iOS, when Visual states are changed too fast, they break. This is a workaround for this bug.
                .ThrottleOrImmediate(TimeSpan.FromMilliseconds(350), Scheduler.Default);

            Type GetViewModelType(SectionsNavigatorState currentState)
            {
                switch (currentState.LastRequestState)
                {
                    case NavigatorRequestState.Processing:
                        return currentState.GetNextViewModelType();
                    case NavigatorRequestState.Processed:
                    case NavigatorRequestState.FailedToProcess:
                        return currentState.GetLastViewModelType();
                    default:
                        throw new NotSupportedException($"The request state {currentState.LastRequestState} is not supported.");
                }
            }
        }
    }
}
