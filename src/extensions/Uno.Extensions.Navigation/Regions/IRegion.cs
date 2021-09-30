using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegion : IRegionNavigate
{
    Task NavigateAsync(NavigationRequest context, TaskCompletionSource<Options.Option> resultCompletion);
}

public interface IRegionNavigate
{
    void RegionNavigate(NavigationContext context);
}
