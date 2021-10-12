using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse(NavigationRequest Request, Task<Options.Option> Result) : NavigationResponseBase(Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse<TResult>(NavigationRequest Request, Task<Options.Option<TResult>> Result) : NavigationResponseBase(Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponseBase(NavigationRequest Request)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
