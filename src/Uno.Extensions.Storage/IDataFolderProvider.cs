namespace Uno.Extensions.Storage;

/// <summary>
/// Interface for providing the data folder path for the application
/// </summary>
public interface IDataFolderProvider
{
	/// <summary>
	/// The path to the application data folder
	/// </summary>
	string? AppDataPath { get; }
}
