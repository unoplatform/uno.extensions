namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public record NavigationResultResponse(Route? Route, Task<IOption> UntypedResult, bool Success = true) : NavigationResponse(Route, Success)
{
	internal NavigationResultResponse<TResult> AsResultResponse<TResult>()
	{
		if(this is NavigationResultResponse<TResult> typedResult)
		{
			return typedResult;
		}
		else
		{
			return new NavigationResultResponse<TResult>(Route, AsTypedOption<TResult>(UntypedResult), Success);
		}
	}

	private static async Task<Option<TResult>> AsTypedOption<TResult>(Task<IOption> result)
	{
		var resultData = await result;
		if (resultData is Option<TResult> optionResult)
		{
			return optionResult;
		}

		if(resultData.SomeOrDefault() is TResult someResult)
		{
			return Option.Some(someResult);
		}

		return Option.None<TResult>();
	}
}

public record NavigationResultResponse<TResult>(Route? Route, Task<Option<TResult>> Result, bool Success = true)
	: NavigationResultResponse(
		Route,
		AsOption(Result),
		Success)
{

	private static async Task<IOption> AsOption(Task<Option<TResult>> result)
	{
		var resultData = await result;
		return resultData;
	}
}

public record NavigationResponse(Route? Route = null, bool Success = true)
{
}

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

