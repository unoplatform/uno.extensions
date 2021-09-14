namespace Uno.Extensions.Navigation.Controls;

public interface IStackNavigation<TControl> : ISimpleNavigation<TControl>
{
    void RemoveLastFromBackStack();

    void ClearBackStack();
}
