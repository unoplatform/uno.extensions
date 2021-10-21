namespace ExtensionsSampleApp.ViewModels.Twitter
{
    public class Tweet
    {
        public static int NextId = 1;
        public int Id { get; set; } = NextId++;
        public string Author { get; set; }
        public string Text { get; set; }
    }
}
