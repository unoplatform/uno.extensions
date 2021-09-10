namespace Uno.Extensions.Navigation.Controls;

public interface IFrameWrapper : IControlNavigation
{
    void RemoveLastFromBackStack();

    void ClearBackStack();
}
