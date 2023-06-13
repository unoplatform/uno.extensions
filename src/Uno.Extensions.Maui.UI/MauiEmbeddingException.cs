namespace Uno.Extensions.Maui;

/// <summary>
/// Represents an exception related to Maui embedding.
/// </summary>
public class MauiEmbeddingException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiEmbeddingException"/> class
	/// with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	internal MauiEmbeddingException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiEmbeddingException"/> class
	/// with a specified error message and inner exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	internal MauiEmbeddingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
