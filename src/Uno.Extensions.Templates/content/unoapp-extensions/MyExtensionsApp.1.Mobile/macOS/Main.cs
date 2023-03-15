using AppKit;

namespace MyExtensionsApp._1.macOS;

public static class MainClass
{
	public static void Main(string[] args)
	{
		NSApplication.Init();
		NSApplication.SharedApplication.Delegate = new AppHead();
		NSApplication.Main(args);
	}
}
