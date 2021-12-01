namespace Uno.Extensions.Navigation;

public static class NavigationResponseExtensions
{
	public static NavigationResultResponse? AsResult(this NavigationResponse response)
	{

		if (response is NavigationResultResponse genericResultResponse)
		{
			return genericResultResponse;
		}

		return null;
	}

	public static NavigationResultResponse<TResult>? AsResult<TResult>(this NavigationResponse response)
    {
        if (response is NavigationResultResponse<TResult> resultResponse)
        {
            return resultResponse;
        }

        return null;
    }
}
