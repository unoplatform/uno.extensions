using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public class ContentWrapper : IContentWrapper
{
    private ContentControl Host { get; set; }

    public void Inject(ContentControl control) => Host = control;

    public object ShowContent(Type contentControl, object viewModel)
    {
        var content = Activator.CreateInstance(contentControl) ;
        if (viewModel is not null && content is FrameworkElement fe)
        {
            fe.DataContext = viewModel;
        }
        Host.Content = content;
        return content;
    }
}
