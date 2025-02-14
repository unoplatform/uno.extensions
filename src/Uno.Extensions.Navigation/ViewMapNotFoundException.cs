namespace Uno.Extensions.Navigation;

/// <summary>
/// Represents an exception that is thrown when a view map is not found in the view registry.
/// </summary>
public class ViewMapNotFoundException : Exception
{
	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class.</summary>
	private ViewMapNotFoundException() { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message.</summary>
	/// <param name="message">The message that describes the error.</param>
	public ViewMapNotFoundException(string message) : base(message) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ViewMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Represents an exception that is thrown when a view map is not found in the view registry.
/// </summary>
public class ViewMapNotFoundByViewException : ViewMapNotFoundException
{
	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message.</summary>
	/// <param name="viewType">The view type that was not found in the view registry.</param>
	public ViewMapNotFoundByViewException(Type viewType) : base(CreateViewTypeNotFoundMessage(viewType)) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="viewType">The view type that was not found in the view registry.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ViewMapNotFoundByViewException(Type viewType, Exception innerException) : base(CreateViewTypeNotFoundMessage(viewType), innerException) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByViewException"></see> class with a specified error message.</summary>
	/// <param name="message">The message that describes the error.</param>
	private ViewMapNotFoundByViewException(string message) : base(message) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByViewException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	private ViewMapNotFoundByViewException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// Creates a message that describes the error when a view model type is not found in the view registry.
	/// </summary>
	/// <param name="viewType"></param>
	/// <returns></returns>
	public static string CreateViewTypeNotFoundMessage(Type viewType) => $"The view type {viewType} was not found in the view registry.";
}

/// <summary>
/// Represents an exception that is thrown when a view map is not found in the view registry.
/// </summary>
public class ViewMapNotFoundByViewModelException : ViewMapNotFoundException
{
	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="viewModelType">The view model type that was not found in the view registry.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ViewMapNotFoundByViewModelException(Type viewModelType, Exception innerException) : base(CreateViewModelTypeNotFoundMessage(viewModelType), innerException) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundException"></see> class with a specified error message.</summary>
	/// <param name="viewType">The view type that was not found in the view registry.</param>
	public ViewMapNotFoundByViewModelException(Type viewType) : base(CreateViewModelTypeNotFoundMessage(viewType)) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByViewModelException"></see> class with a specified error message.</summary>
	/// <param name="message">The message that describes the error.</param>
	private ViewMapNotFoundByViewModelException(string message) : base(message) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByViewModelException"></see> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	private ViewMapNotFoundByViewModelException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	///	Creates a message that describes the error when a view model type is not found in the view registry.
	///	</summary>
	public static string CreateViewModelTypeNotFoundMessage(Type viewModelType) => $"The view model type {viewModelType} was not found in the view registry.";
}

/// <summary>
/// Represents an exception that is thrown when a view map is not found in the view registry.
/// </summary>
public class ViewMapNotFoundByDataException : ViewMapNotFoundException
{
	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByDataException"/> class with a specified error message.</summary>
	/// <param name="dataType">The data type that was not found in the view registry.</param>
	public ViewMapNotFoundByDataException(Type dataType) : base(CreateDataTypeNotFoundMessage(dataType)) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByDataException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="dataType">The data type that was not found in the view registry.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ViewMapNotFoundByDataException(Type dataType, Exception innerException) : base(CreateDataTypeNotFoundMessage(dataType), innerException) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByDataException"/> class with a specified error message.</summary>
	/// <param name="message">The message that describes the error.</param>
	private ViewMapNotFoundByDataException(string message) : base(message) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByDataException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	private ViewMapNotFoundByDataException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// Creates a message that describes the error when a data type is not found in the view registry.
	/// </summary>
	/// <param name="dataType">The data type that was not found in the view registry.</param>
	/// <returns>The error message.</returns>
	public static string CreateDataTypeNotFoundMessage(Type dataType) => $"The data type {dataType} was not found in the view registry.";
}

/// <summary>
/// Represents an exception that is thrown when a view map is not found in the view registry.
/// </summary>
public class ViewMapNotFoundByResultDataException : ViewMapNotFoundException
{
	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByResultDataException"/> class with a specified error message.</summary>
	/// <param name="resultDataType">The result data type that was not found in the view registry.</param>
	public ViewMapNotFoundByResultDataException(Type resultDataType) : base(CreateResultDataTypeNotFoundMessage(resultDataType)) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByResultDataException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="resultDataType">The result data type that was not found in the view registry.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ViewMapNotFoundByResultDataException(Type resultDataType, Exception innerException) : base(CreateResultDataTypeNotFoundMessage(resultDataType), innerException) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByResultDataException"/> class with a specified error message.</summary>
	/// <param name="message">The message that describes the error.</param>
	private ViewMapNotFoundByResultDataException(string message) : base(message) { }

	/// <summary>Initializes a new instance of the <see cref="ViewMapNotFoundByResultDataException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	private ViewMapNotFoundByResultDataException(string message, Exception innerException) : base(message, innerException) { }

	/// <summary>
	/// Creates a message that describes the error when a result data type is not found in the view registry.
	/// </summary>
	/// <param name="resultDataType">The result data type that was not found in the view registry.</param>
	/// <returns>The error message.</returns>
	public static string CreateResultDataTypeNotFoundMessage(Type resultDataType) => $"The result data type {resultDataType} was not found in the view registry.";
}
