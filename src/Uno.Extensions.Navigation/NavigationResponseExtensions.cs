namespace Uno.Extensions.Navigation;

public static class NavigationResponseExtensions
{
	public static NavigationResultResponse? AsResultResponse(this NavigationResponse response)
	{

		if (response is NavigationResultResponse genericResultResponse)
		{
			return genericResultResponse;
		}

		return null;
	}

	public static NavigationResultResponse<TResult>? AsResultResponse<TResult>(this NavigationResponse response)
    {
        if (response is NavigationResultResponse<TResult> resultResponse)
        {
            return resultResponse;
        }

        return null;
    }

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
