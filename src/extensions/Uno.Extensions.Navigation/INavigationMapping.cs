namespace Uno.Extensions.Navigation
{
    public interface INavigationMapping
    {
        void Register(NavigationMap map);

        NavigationMap LookupByPath(string path);
    }
}
