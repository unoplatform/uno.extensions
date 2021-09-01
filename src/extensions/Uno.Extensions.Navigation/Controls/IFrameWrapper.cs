using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls
{

    public interface IFrameWrapper : IInjectable<Frame>
    {
        void GoBack(object parameter, object viewModel);

        bool Navigate(Type sourcePageType, object parameter, object viewModel);

        void RemoveLastFromBackStack();

        void ClearBackStack();
    }
}
