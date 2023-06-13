namespace Uno.Extensions.Maui;

public sealed class MauiEmbeddingInitializationException : MauiEmbeddingException
{
	public static string ErrorMessage => Properties.Resources.MauiEmbeddingInitializationExceptionMessage;

	internal MauiEmbeddingInitializationException()
		: base(ErrorMessage)
	{
	}
}
