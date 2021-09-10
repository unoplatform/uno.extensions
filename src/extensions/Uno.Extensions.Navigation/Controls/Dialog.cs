using Windows.Foundation;

namespace Uno.Extensions.Navigation.Controls;

public record Dialog(IDialogManager Manager, IAsyncInfo ShowTask, NavigationContext Context) { }
