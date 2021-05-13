using System;
using System.Threading.Tasks;
//using Chinook.DynamicMvvm;
//using Chinook.StackNavigation;
//using MallardMessageHandlers;

namespace ApplicationTemplate.Presentation
{
    public partial class HomePageViewModel : ViewModel
    {
        public string Welcome { get; } = "Welcome to App Template";

        //public IDynamicCommand NavigateToPostsPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new PostsPageViewModel());
        //});

        //public IDynamicCommand NavigateToPostsPageWithNoNetwork => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new PostsPageViewModel(
        //        async () =>
        //        {
        //            await Task.Delay(TimeSpan.FromSeconds(2));

        //            throw new NoNetworkException("You don't have network.");
        //        })
        //    );
        //});

        //public IDynamicCommand NavigateToPostsPageWithOddError => this.GetCommandFromTask(async ct =>
        //{
        //    var executions = 0;

        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new PostsPageViewModel(
        //        async () =>
        //        {
        //            await Task.Delay(TimeSpan.FromSeconds(2));

        //            if (executions++ % 2 != 0)
        //            {
        //                throw new NoNetworkException("You don't have network.");
        //            }
        //        }
        //    ));
        //});

        //public IDynamicCommand NavigateToPostsPageWithEvenError => this.GetCommandFromTask(async ct =>
        //{
        //    var executions = 0;

        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new PostsPageViewModel(
        //        async () =>
        //        {
        //            await Task.Delay(TimeSpan.FromSeconds(2));

        //            if (executions++ % 2 == 0)
        //            {
        //                throw new NoNetworkException("You don't have network.");
        //            }
        //        }
        //    ));
        //});

        //public IDynamicCommand NavigateToChuckNorrisSearchPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new ChuckNorrisSearchPageViewModel());
        //});

        //public IDynamicCommand NavigateToSettingsPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<IStackNavigator>().Navigate(ct, () => new SettingsPageViewModel());
        //});
    }
}
