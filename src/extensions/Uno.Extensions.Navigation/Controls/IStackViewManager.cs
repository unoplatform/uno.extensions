namespace Uno.Extensions.Navigation.Controls;

public interface IStackViewManager<TControl> : IViewManager<TControl>
{
    void RemoveLastFromBackStack();

    void ClearBackStack();
}
