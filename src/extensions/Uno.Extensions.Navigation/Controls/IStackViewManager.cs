namespace Uno.Extensions.Navigation.Controls;

public interface IStackViewManager : IViewManager
{
    void GoBack(object data, object viewModel);

    void RemoveLastFromBackStack();

    void ClearBackStack();
}
