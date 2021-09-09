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

public class ContentWrapper : BaseWrapper<ContentControl>, IContentWrapper
{
    private ContentControl Host => Control;

    public override NavigationContext CurrentContext => (Host.Content as FrameworkElement).GetContext();

    public object ShowContent(NavigationContext context, Type contentControl, object viewModel)
    {
        var content = Activator.CreateInstance(contentControl) ;
        if (content is FrameworkElement fe)
        {
            fe.SetContext(context);
            if (viewModel is not null)
            {
                fe.DataContext = viewModel;
            }
        }
        Host.Content = content;
        return content;
    }
}
