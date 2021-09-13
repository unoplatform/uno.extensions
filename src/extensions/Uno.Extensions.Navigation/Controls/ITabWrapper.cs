namespace Uno.Extensions.Navigation.Controls;

public interface ITabWrapper : IControlNavigation
{
    string CurrentTabName { get; }

    bool ContainsTab(string tabName);
}
