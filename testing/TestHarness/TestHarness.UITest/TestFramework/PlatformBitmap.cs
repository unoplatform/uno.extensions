using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace TestHarness.UITest.TestFramework
{
	public class PlatformBitmap : IDisposable
	{
		SKBitmap _bitmap;

		public PlatformBitmap(SKBitmap bitmap)
		{
			_bitmap = bitmap;
		}

		public PlatformBitmap(Stream bitmap)
		{
			using var inputStream = new SKManagedStream(bitmap);
			_bitmap = SKBitmap.Decode(inputStream);
		}

		public PlatformBitmap(string filePath)
		{
			using var netStream = File.OpenRead(filePath);
			using var inputStream = new SKManagedStream(netStream);
			_bitmap = SKBitmap.Decode(inputStream);
		}

		public int Width => _bitmap.Width;

		public int Height => _bitmap.Height;

		public System.Drawing.Size Size => new(_bitmap.Width, _bitmap.Height);

		public System.Drawing.Color GetPixel(int x, int y)
		{
			var p = _bitmap.GetPixel(x, y);

			return Color.FromArgb(p.Alpha, p.Red, p.Green, p.Blue);
		}

		public void Dispose()
			=> _bitmap.Dispose();
	}

}
