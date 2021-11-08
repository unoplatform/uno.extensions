using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Uno.Extensions.Navigation;


namespace ExtensionsSampleApp.ViewModels.Twitter
{
    public class TweetDetailsViewModel : ObservableObject
    {
        private Tweet tweet;
        public Tweet Tweet { get => tweet; set => SetProperty(ref tweet, value); }
        public TweetDetailsViewModel(Tweet tweet, IDictionary<string, object> data)
        {
            Tweet = tweet;
            if (Tweet is null)
            {
                Tweet = new Tweet { Id = int.Parse(data["TweetId"] + ""), Author = "Ned", Text = "Tweet loaded on start" };
            }
        }

    }
}
