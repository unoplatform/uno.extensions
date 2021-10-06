using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public record NavigationResponse(NavigationRequest Request, Task<Options.Option> Result) : BaseNavigationResponse(Request)
{
}

public record NavigationResponse<TResult>(NavigationRequest Request, Task<Options.Option<TResult>> Result) : BaseNavigationResponse(Request)
{
    public static NavigationResponse<TResult> FromResponse(NavigationResponse response)
    {
        if (response is null)
        {
            return null;
        }

        return new NavigationResponse<TResult>(response.Request, response.Result.ContinueWith(x =>
  (x.Result.MatchSome(out var val) && val is TResult tval) ?
      Options.Option.Some(tval) :
      Options.Option.None<TResult>())
        );
    }
}

public record BaseNavigationResponse(NavigationRequest Request)
{
}
