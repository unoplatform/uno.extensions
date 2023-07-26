namespace Uno.Extensions.Navigation;

/// <summary>
/// Navigation response with result
/// </summary>
/// <param name="Route">The Route that was navigated to</param>
/// <param name="UntypedResult">The untyped result of navigation</param>
/// <param name="Success">Whether or not navigation was successful </param>
/// <param name="Navigator">The INavigator instance that processed the final segment of the Route</param>
public record NavigationResultResponse(
	Route? Route, Task<IOption> UntypedResult,
	bool Success = true, INavigator? Navigator = default) : NavigationResponse(Route, Success, Navigator)
{
	internal NavigationResultResponse<TResult> AsResultResponse<TResult>()
	{
		if (this is NavigationResultResponse<TResult> typedResult)
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

		if (resultData.SomeOrDefault() is TResult someResult)
		{
			return Option.Some(someResult);
		}

		return Option.None<TResult>();
	}
}

/// <summary>
/// Navigation response with Typed result
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <param name="Route">The Route that was navigated to</param>
/// <param name="Result">The result of navigation</param>
/// <param name="Success">Whether or not navigation was successful </param>
/// <param name="Navigator">The INavigator instance that processed the final segment of the Route</param>
public record NavigationResultResponse<TResult>(Route? Route, Task<Option<TResult>> Result, bool Success = true, INavigator? Navigator = default)
	: NavigationResultResponse(
		Route,
		AsOption(Result),
		Success,
		Navigator)
{

	private static async Task<IOption> AsOption(Task<Option<TResult>> result)
	{
		var resultData = await result;
		return resultData;
	}
}

/// <summary>
/// Navigation response
/// </summary>
/// <param name="Route">The Route that was navigated to</param>
/// <param name="Success">Whether or not navigation was successful </param>
/// <param name="Navigator">The INavigator instance that processed the final segment of the Route</param>
public record NavigationResponse(Route? Route = null, bool Success = true, INavigator? Navigator = default);

