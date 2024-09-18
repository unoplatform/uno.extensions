using System.Text.RegularExpressions;
using TestContext = NUnit.Framework.TestContext;

namespace TestHarness.UITest;

[TestFixture]
public abstract class TestBase
{
	private IApp? _app;

	private readonly string _screenShotPath = Environment.GetEnvironmentVariable("UNO_UITEST_SCREENSHOT_PATH");
	private DateTime _startTime;


	static TestBase()
	{
		AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
		AppInitializer.TestEnvironment.WebAssemblyDefaultUri = Constants.WebAssemblyDefaultUri;
		AppInitializer.TestEnvironment.iOSAppName = Constants.iOSAppName;
		AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
		AppInitializer.TestEnvironment.iOSDeviceNameOrId = Constants.iOSDeviceNameOrId;
		AppInitializer.TestEnvironment.CurrentPlatform = Constants.CurrentPlatform;

#if DEBUG
		AppInitializer.TestEnvironment.WebAssemblyHeadless = false;
#endif

	}

	protected IApp App
	{
		get => _app!;
		private set
		{
			_app = value;
			Uno.UITest.Helpers.Queries.Helpers.App = value;
		}
	}

	[SetUp]
	public void SetUpTest()
	{
		_startTime = DateTime.Now;
		AppInitializer.ColdStartApp();
		App = AppInitializer.AttachToApp();
	}

	[TearDown]
	public void TearDownTest()
	{
		if (
			TestContext.CurrentContext.Result.Outcome != ResultState.Success
			&& TestContext.CurrentContext.Result.Outcome != ResultState.Skipped
			&& TestContext.CurrentContext.Result.Outcome != ResultState.Ignored
		)
		{
			TakeScreenshot($"{TestContext.CurrentContext.Test.Name} - Tear down on error", ignoreInSnapshotCompare: true);
		}

		WriteSystemLogs(GetCurrentStepTitle("log"));


		Console.WriteLine($"Test completed - {TestContext.CurrentContext.Result.Outcome}");

		// TODO: Update AppInitializer to correctly dispose currentApp rather than reusing it
		App.Dispose();
		var field = typeof(AppInitializer).GetField("_currentApp",
						System.Reflection.BindingFlags.Static |
						System.Reflection.BindingFlags.NonPublic);
		field?.SetValue(null, null);
	}


	private void WriteSystemLogs(string fileName)
	{
		if (_app != null && AppInitializer.GetLocalPlatform() == Platform.Browser)
		{
			var outputPath = string.IsNullOrEmpty(_screenShotPath)
				? Environment.CurrentDirectory
				: _screenShotPath;

			using (var logOutput = new StreamWriter(Path.Combine(outputPath, $"{fileName}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss.fff}.txt")))
			{
				foreach (var log in _app.GetSystemLogs(_startTime.ToUniversalTime()))
				{
					logOutput.WriteLine($"{log.Timestamp}/{log.Level}: {log.Message}");
				}
			}
		}
	}

	public ScreenshotInfo TakeScreenshot(string stepName, bool? ignoreInSnapshotCompare = null)
		=> TakeScreenshot(
			stepName,
			ignoreInSnapshotCompare != null
				? new ScreenshotOptions { IgnoreInSnapshotCompare = ignoreInSnapshotCompare.Value }
				: null
		);

	public ScreenshotInfo TakeScreenshot(string stepName, ScreenshotOptions? options)
	{
		if (_app == null)
		{
			Console.WriteLine($"Skipping TakeScreenshot _app is not available");
			throw new NotSupportedException("_app is not available to take screenshot");
		}

		var title = GetCurrentStepTitle(stepName);

		var fileInfo = GetNativeScreenshot(title);

		var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
		if (fileNameWithoutExt != title)
		{
			var outputPath = string.IsNullOrEmpty(_screenShotPath) ? Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty : _screenShotPath;

			var destFileName = Path
				.Combine(outputPath, title + Path.GetExtension(fileInfo.Name))
				.GetNormalizedLongPath();

			if (File.Exists(destFileName))
			{
				File.Delete(destFileName);
			}

			File.Move(fileInfo.FullName, destFileName);

			TestContext.AddTestAttachment(destFileName, stepName);

			fileInfo = new FileInfo(destFileName);
		}
		else
		{
			TestContext.AddTestAttachment(fileInfo.FullName, stepName);
		}

		if (options != null)
		{
			SetOptions(fileInfo, options);
		}

		return new ScreenshotInfo(fileInfo, stepName);
	}

	private static string GetCurrentStepTitle(string stepName)
		=> Regex.Replace($"{TestContext.CurrentContext.Test.Name}_{stepName}", "[^A-z0-9]", "_");

	public void SetOptions(FileInfo screenshot, ScreenshotOptions options)
	{
		var fileName = Path
			.Combine(screenshot.DirectoryName, Path.GetFileNameWithoutExtension(screenshot.FullName) + ".metadata")
			.GetNormalizedLongPath();

		File.WriteAllText(fileName, $"IgnoreInSnapshotCompare={options.IgnoreInSnapshotCompare}");
	}

	private FileInfo GetNativeScreenshot(string title)
	{
		if (AppInitializer.GetLocalPlatform() == Platform.Android)
		{
			return _app.GetInAppScreenshot();
		}
		else
		{
			return _app.Screenshot(title);
		}
	}
}
