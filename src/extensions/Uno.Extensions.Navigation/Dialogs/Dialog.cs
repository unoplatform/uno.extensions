using Windows.Foundation;

namespace Uno.Extensions.Navigation.Dialogs;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Dialog(IAsyncInfo ShowTask, NavigationContext Context) { }
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
