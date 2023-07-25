namespace Uno.Extensions.Maui;

/// <summary>
/// Represents an <see cref="MauiEmbeddingException"/> that occurs during Maui embedding initialization.
/// </summary>
public sealed class MauiEmbeddingInitializationException : MauiEmbeddingException
{
	/// <summary>
	/// Gets the error message for the <see cref="Exception"/>.
	/// </summary>
	public static string ErrorMessage => Properties.Resources.MauiEmbeddingInitializationExceptionMessage;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiEmbeddingInitializationException"/> class with the error message.
	/// </summary>
	internal MauiEmbeddingInitializationException()
		: base(ErrorMessage)
	{
	}
}
