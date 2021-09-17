namespace Uno.Extensions.Navigation.Controls;

public interface IStackViewManager<TControl> : IViewManager<TControl>
{
    void GoBack(object data, object viewModel);

    void RemoveLastFromBackStack();

    void ClearBackStack();
}
