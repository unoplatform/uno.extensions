namespace Uno.Extensions.Maui;

public class MauiEmbeddingException : Exception
{
	internal MauiEmbeddingException(string message)
		: base(message)
	{
	}

	internal MauiEmbeddingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
