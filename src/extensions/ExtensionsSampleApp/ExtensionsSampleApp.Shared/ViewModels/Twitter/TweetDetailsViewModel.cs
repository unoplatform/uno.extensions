using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;

namespace ExtensionsSampleApp.ViewModels.Twitter
{
    public class TweetDetailsViewModel : ObservableObject, IViewModelStart
    {
        private Tweet tweet;
        public Tweet Tweet { get => tweet; set => SetProperty(ref tweet, value); }
        public TweetDetailsViewModel(Tweet tweet)
        {
            Tweet = tweet;
        }

        public async Task Start(NavigationRequest context)
        {
            if (Tweet is null)
            {
                Tweet = new Tweet { Id = int.Parse(context.Route.Data["TweetId"] + ""), Author = "Ned", Text = "Tweet loaded on start" };
            }
        }
    }
}
