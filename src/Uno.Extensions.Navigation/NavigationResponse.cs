using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public record NavigationResultResponse(Route Route, Task<IOption> UntypedResult, bool Success = true) : NavigationResponse(Route, Success)
{
}

public record NavigationResultResponse<TResult>(Route Route, Task<Option<TResult>> Result, bool Success = true)
	: NavigationResultResponse(
		Route,
		AsOption(Result),
		Success)
{

	private static async Task<IOption> AsOption(Task<Option<TResult>> result)
	{
		var resultData =  await result;
		return resultData;
	}
}

public record NavigationResponse(Route? Route = null, bool Success = true)
{
}

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

