using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace MauiEmbedding.MauiControls;

// All the code in this file is included in all platforms.
public class CustomEntry : Entry
{
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
				nameof(BorderColor), typeof(Color), typeof(CustomEntry), null);

	public Color BorderColor
	{
		get => (Color)GetValue(BorderColorProperty);
		set => SetValue(BorderColorProperty, value);
	}

	internal static void Init()
	{
		EntryHandler.Mapper.AppendToMapping(nameof(BorderColor), (h, v) =>
		{
			if (v is not CustomEntry entry)
			{
				return;
			}

#if IOS
			if (h.PlatformView is UIKit.UITextField textField)
			{
				var color = entry.BorderColor?.ToPlatform().CGColor ?? UIKit.UIColor.Black.CGColor;
				textField.Layer.BorderColor = color;
				textField.Layer.BorderWidth = 1;
			}

#elif ANDROID

			if (h.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText editText)
			{
				var color = entry.BorderColor?.ToPlatform() ?? Android.Graphics.Color.Black;
				var shape = new Android.Graphics.Drawables.ShapeDrawable(new Android.Graphics.Drawables.Shapes.RectShape());
				shape.Paint.Color = color;
				shape.Paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
				editText.Background = shape;
			}
#elif WINDOWS
			if (h.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
			{
				var color = entry.BorderColor.ToPlatform() ?? Colors.Black.ToPlatform();
				textBox.BorderBrush = color;
				textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
			}
#endif
		});
	}
}
