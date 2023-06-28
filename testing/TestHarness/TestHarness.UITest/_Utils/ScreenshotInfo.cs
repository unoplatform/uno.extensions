namespace TestHarness.UITest;

public partial class ScreenshotInfo : IDisposable
{
	private PlatformBitmap? _bitmap;
	public FileInfo File { get; }

	public string StepName { get; }

	public ScreenshotInfo(FileInfo file, string stepName)
	{
		File = file;
		StepName = stepName;
	}

	public static implicit operator FileInfo(ScreenshotInfo si) => si.File;

	public static implicit operator ScreenshotInfo(FileInfo fi) => new ScreenshotInfo(fi, fi.Name);

	public PlatformBitmap GetBitmap() => _bitmap ??= new PlatformBitmap(File.FullName);
	public void Dispose()
	{
		_bitmap?.Dispose();
		_bitmap = null;
	}

	~ScreenshotInfo()
	{
		Dispose();
	}
}
