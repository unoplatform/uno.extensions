using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResultResponse(NavigationRequest Request, Task<Options.Option> Result, bool Success = true) : NavigationResponse(Request, Success)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResultResponse<TResult>(NavigationRequest Request, Task<Options.Option<TResult>> Result, bool Success = true) : NavigationResponse(Request, Success)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse(NavigationRequest Request, bool Success = true)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
