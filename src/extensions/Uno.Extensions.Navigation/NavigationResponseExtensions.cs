using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public static class NavigationResponseExtensions
{
    public static NavigationResponse<TResult> As<TResult>(this NavigationResponse response)
    {
        if (response is null)
        {
            return null;
        }

        return new NavigationResponse<TResult>(response.Request,
            response.Result.ContinueWith(x =>
                (x.Result.MatchSome(out var val) && val is TResult tval) ?
                    Options.Option.Some(tval) :
                    Options.Option.None<TResult>(),
                TaskScheduler.Current));
    }
}
