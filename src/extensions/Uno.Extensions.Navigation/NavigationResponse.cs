using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse(NavigationRequest Request, Task<Options.Option> Result) : BaseNavigationResponse(Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse<TResult>(NavigationRequest Request, Task<Options.Option<TResult>> Result) : BaseNavigationResponse(Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
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

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record BaseNavigationResponse(NavigationRequest Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
