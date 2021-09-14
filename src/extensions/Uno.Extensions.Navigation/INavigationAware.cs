namespace Uno.Extensions.Navigation;

public interface INavigationAware
{
    INavigationService Navigation { get; set; }
}
