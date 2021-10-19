using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse(NavigationRequest Request, Task<Options.Option> Result, bool Success = true) : NavigationResponseBase(Request, Success)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse<TResult>(NavigationRequest Request, Task<Options.Option<TResult>> Result, bool Success = true) : NavigationResponseBase(Request, Success)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponseBase(NavigationRequest Request, bool Success = true)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
