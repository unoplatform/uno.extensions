using System.Collections.Generic;

namespace ExtensionsSampleApp.ViewModels.Twitter
{
    public class NotificationsViewModel
    {
        public IList<Tweet> Notifications { get; } = new List<Tweet>()
        {
            new Tweet() {Author= "Fred", Text="First tweet"},
            new Tweet() {Author= "Fred2", Text="Second tweet"},
            new Tweet() {Author= "Fred3", Text="Third tweet"},
            new Tweet() {Author= "Fred4", Text="Fourth tweet"},
            new Tweet() {Author= "Fred5", Text="Fifth tweet"},
            new Tweet() {Author= "Fred5", Text="Sixth tweet"},
            new Tweet() {Author= "Fred6", Text="Seventh tweet"},

        };
    }
}
