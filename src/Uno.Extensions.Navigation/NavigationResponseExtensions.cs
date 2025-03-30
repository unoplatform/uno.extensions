namespace Uno.Extensions.Navigation;

public static class NavigationResponseExtensions
{
	/// <summary>
	/// Converts a navigation response to a generic result response.
	/// </summary>
	/// <param name="response">The navigation response.</param>
	/// <returns>A generic result response if the conversion is successful; otherwise, null.</returns>
	public static NavigationResultResponse? AsResultResponse(this NavigationResponse response)
	{
		if (response is NavigationResultResponse genericResultResponse)
		{
			return genericResultResponse;
		}

		return null;
	}

	/// <summary>
	/// Converts a navigation response to a typed result response.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="response">The navigation response.</param>
	/// <returns>A typed result response if the conversion is successful; otherwise, null.</returns>
	public static NavigationResultResponse<TResult>? AsResultResponse<TResult>(this NavigationResponse response)
    {
		if (response is NavigationResultResponse genericResultResponse)
		{
			return genericResultResponse.AsResultResponse<TResult>();
		}

		return null;
    }

	/// <summary>
	/// Converts a navigation result response task to an option of the result type.
	/// </summary>
	/// <typeparam name="TResult">The type of the expected result.</typeparam>
	/// <param name="navigationResponse">The task representing the navigation result response.</param>
	/// <returns>An option containing the result if available; otherwise, an empty option.</returns>
	public static async ValueTask<Option<TResult>> AsResult<TResult>(this Task<NavigationResultResponse<TResult>?> navigationResponse)
	{
		var response = await navigationResponse.ConfigureAwait(false);
		if (response?.Result is not null)
		{
			return await response.Result;
		}

		return Option.None<TResult>();
	}
}
