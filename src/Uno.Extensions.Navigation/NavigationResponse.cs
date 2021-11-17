using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public record NavigationResultResponse(Route Route, Task<Options.Option> Result, bool Success = true) : NavigationResponse(Route, Success)
{
}

public record NavigationResultResponse<TResult>(Route Route, Task<Options.Option<TResult>> Result, bool Success = true) : NavigationResponse(Route, Success)
{
}

public record NavigationResponse(Route? Route = null, bool Success = true)
{
}

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

