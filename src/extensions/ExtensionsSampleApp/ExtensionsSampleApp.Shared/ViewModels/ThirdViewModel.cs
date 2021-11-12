using ExtensionsSampleApp.Views;

namespace ExtensionsSampleApp.ViewModels
{
    public class ThirdViewModel
    {

        public Widget[] Widgets { get; } = new[]
       {
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
            new Widget(),
        };

        public string Title => "Third";
    }
}
