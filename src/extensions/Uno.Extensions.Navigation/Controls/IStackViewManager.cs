using System;

namespace Uno.Extensions.Navigation.Controls;

public interface IStackViewManager : IViewManager
{
    void GoBack(Type view, object data, object viewModel);

    void RemoveLastFromBackStack();

    void ClearBackStack();
}
