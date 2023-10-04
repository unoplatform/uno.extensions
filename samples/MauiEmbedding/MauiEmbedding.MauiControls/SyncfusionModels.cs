using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Syncfusion.Maui.Charts;
using Font = Microsoft.Maui.Graphics.Font;
using IImage = Microsoft.Maui.Graphics.IImage;
#if IOS || ANDROID || MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#elif WINDOWS
using Microsoft.Maui.Graphics.Win2D;
#endif

namespace MauiEmbedding.MauiControls;

public class ChartDataModel
{
	public string? Name { get; set; }

	public double Data { get; set; }

	public string? Label { get; set; }
	public DateTime Date { get; set; }

	public double Value { get; set; }

	public double Value1 { get; set; }

	public double Size { get; set; }

	public double High { get; set; }

	public double Low { get; set; }

	public bool IsSummary { get; set; }

	public string? Department { get; set; }

	public string? Image { get; set; }

	public List<double>? EmployeeAges { get; set; }

	public Brush? Color { get; set; }

	public double Percentage { get; set; }

	public ChartDataModel(string department, List<double> employeeAges)
	{
		Department = department;
		EmployeeAges = employeeAges;
	}

	public ChartDataModel(string name, double value)
	{
		Name = name;
		Value = value;
	}

	public ChartDataModel(string name, double value, Brush color, double percentage)
	{
		Name = name;
		Value = value;
		Color = color;
		Percentage = percentage;
	}

	public ChartDataModel(string name, double value, string image)
	{
		Name = name;
		Value = value;
		Image = image;
	}

	public ChartDataModel(string name, double value, double horizontalErrorValue, double verticalErrorValue)
	{
		Name = name;
		Value = value;
		High = horizontalErrorValue;
		Low = verticalErrorValue;
	}

	public ChartDataModel(string name, double value, double size)
	{
		Name = name;
		Value = value;
		Size = size;
	}

	public ChartDataModel()
	{

	}
	public ChartDataModel(DateTime date, double value, double size)
	{
		Date = date;
		Value = value;
		Size = size;
	}

	public ChartDataModel(double value, double value1, double size)
	{
		Value1 = value;
		Value = value1;
		Size = size;
	}

	public ChartDataModel(double value1, double value, double size, string label)
	{
		Value1 = value1;
		Value = value;
		Size = size;
		Label = label;
	}

	public ChartDataModel(string name, double high, double low, double open, double close)
	{
		Name = name;
		High = high;
		Low = low;
		Value = open;
		Size = close;
	}

	public ChartDataModel(double name, double high, double low, double open, double close)
	{
		Data = name;
		High = high;
		Low = low;
		Value = open;
		Size = close;
	}

	public ChartDataModel(DateTime date, double high, double low, double open, double close)
	{
		Date = date;
		High = high;
		Low = low;
		Value = open;
		Size = close;
	}
	public ChartDataModel(double value, double size)
	{
		Value = value;
		Size = size;
	}
	public ChartDataModel(DateTime dateTime, double value)
	{
		Date = dateTime;
		Value = value;
	}

	public ChartDataModel(string name, double value, bool isSummary)
	{
		Name = name;
		Value = value;
		IsSummary = isSummary;
	}
}
public class PieSeriesViewModel : BaseViewModel
{
	public ObservableCollection<ChartDataModel> PieSeriesData { get; set; }
	public ObservableCollection<ChartDataModel> SemiCircularData { get; set; }
	public ObservableCollection<ChartDataModel> GroupToData { get; set; }

	public PieSeriesViewModel()
	{
		PieSeriesData = new ObservableCollection<ChartDataModel>
			{
				new ChartDataModel("David", 16.6),
				new ChartDataModel("Steve", 14.6),
				new ChartDataModel("Jack", 18.6),
				new ChartDataModel("John", 20.5),
				new ChartDataModel("Regev", 39.5),
		   };

		SemiCircularData = new ObservableCollection<ChartDataModel>
			{
				new ChartDataModel("Product A", 750),
				new ChartDataModel("Product B", 463),
				new ChartDataModel("Product C", 389),
				new ChartDataModel("Product D", 697),
				new ChartDataModel("Product E", 251)
			};

		GroupToData = new ObservableCollection<ChartDataModel>
			{
				new ChartDataModel( "US",22.90,0.244),
				new ChartDataModel("China",16.90,0.179),
				new ChartDataModel( "Japan",5.10,0.054),
				new ChartDataModel("Germany",4.20,0.045),
				new ChartDataModel("UK",3.10,0.033),
				new ChartDataModel("India",2.90,0.031),
				new ChartDataModel("France",2.90,0.031),
				new ChartDataModel( "Italy",2.10,0.023),
				new ChartDataModel( "Canada",2.00,0.021),
				new ChartDataModel( "Korea",1.80,0.019),
				new ChartDataModel("Russia",1.60,0.017),
				new ChartDataModel("Brazil",1.60,0.017),
				new ChartDataModel("Australia",1.60,0.017),
				new ChartDataModel("Spain",1.40,0.015),
				new ChartDataModel("Mexico",1.30,0.014),
				new ChartDataModel("Indonesia",1.20,0.012),
				new ChartDataModel("Iran",1.10,0.011),
				new ChartDataModel("Netherlands",1.00,0.011),
				new ChartDataModel("Saudi Arabia",0.80,0.009),
				new ChartDataModel("Switzerland",0.80,0.009),
				new ChartDataModel("Turkey",0.80,0.008),
				new ChartDataModel("Taiwan",0.80,0.008),
				new ChartDataModel("Poland",0.70,0.007),
				new ChartDataModel("Sweden",0.60,0.007),
				new ChartDataModel("Belgium",0.60,0.006),
				new ChartDataModel("Thailand",0.50,0.006),
				new ChartDataModel("Ireland",0.50,0.005),
				new ChartDataModel("Austria",0.50,0.005),
				new ChartDataModel("Nigeria",0.50,0.005),
				new ChartDataModel("Israel",0.50,0.005),
				new ChartDataModel("Argentina",0.50,0.005),
				new ChartDataModel("Norway",0.40,0.005),
				new ChartDataModel("South Africa",0.40,0.004),
				new ChartDataModel("UAE",0.40,0.004),
				new ChartDataModel("Denmark",0.40,0.004),
				new ChartDataModel("Egypt",0.40,0.004),
				new ChartDataModel("Philippines",0.40,0.004),
				new ChartDataModel("Singapore",0.40,0.004),
				new ChartDataModel("Malaysia",0.40,0.004),
				new ChartDataModel("Hong Kong SAR",0.40,0.004),
				new ChartDataModel("Vietnam",0.40,0.004),
				new ChartDataModel("Bangladesh",0.40,0.004),
				new ChartDataModel("Chile",0.30,0.004),
				new ChartDataModel("Colombia",0.30,0.003),
				new ChartDataModel("Finland",0.30,0.003),
				new ChartDataModel("Romania",0.30,0.003),
				new ChartDataModel("Czech Republic",0.30,0.003),
				new ChartDataModel("Portugal",0.30,0.003),
				new ChartDataModel("Pakistan",0.30,0.003),
				new ChartDataModel("New Zealand",0.20,0.003),
			};
	}
}



public class BaseViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<Brush> PaletteBrushes { get; set; }
	public ObservableCollection<Brush> SelectionBrushes { get; set; }
	public ObservableCollection<Brush> CustomColor1 { get; set; }

	public ObservableCollection<Brush> CustomColor2 { get; set; }
	public ObservableCollection<Brush> AlterColor1 { get; set; }
	public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

	public Array PieGroupMode
	{
		get { return Enum.GetValues(typeof(PieGroupMode)); }
	}

	private bool enableAnimation = true;
	public bool EnableAnimation
	{
		get { return enableAnimation; }
		set
		{
			enableAnimation = value;
			OnPropertyChanged("EnableAnimation");
		}
	}

	public void OnPropertyChanged(string name)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	public BaseViewModel()
	{
		PaletteBrushes = new ObservableCollection<Brush>()
			{
			   new SolidColorBrush(Color.FromArgb("#314A6E")),
				 new SolidColorBrush(Color.FromArgb("#48988B")),
				 new SolidColorBrush(Color.FromArgb("#5E498C")),
				 new SolidColorBrush(Color.FromArgb("#74BD6F")),
				 new SolidColorBrush(Color.FromArgb("#597FCA"))
			};

		SelectionBrushes = new ObservableCollection<Brush>()
			{
				new SolidColorBrush(Color.FromArgb("#40314A6E")),
				new SolidColorBrush(Color.FromArgb("#4048988B")),
				new SolidColorBrush(Color.FromArgb("#405E498C")),
				new SolidColorBrush(Color.FromArgb("#4074BD6F")),
				new SolidColorBrush(Color.FromArgb("#40597FCA"))
			};

		CustomColor2 = new ObservableCollection<Brush>()
			{
				new SolidColorBrush(Color.FromArgb("#519085")),
				new SolidColorBrush(Color.FromArgb("#F06C64")),
				new SolidColorBrush(Color.FromArgb("#FDD056")),
				new SolidColorBrush(Color.FromArgb("#81B589")),
				new SolidColorBrush(Color.FromArgb("#88CED2"))
			};

		CustomColor1 = new ObservableCollection<Brush>()
			{
				new SolidColorBrush(Color.FromArgb("#95DB9C")),
				new SolidColorBrush(Color.FromArgb("#B95375")),
				new SolidColorBrush(Color.FromArgb("#56BBAF")),
				new SolidColorBrush(Color.FromArgb("#606D7F")),
				new SolidColorBrush(Color.FromArgb("#E99941")),
				new SolidColorBrush(Color.FromArgb("#327DBE")),
				new SolidColorBrush(Color.FromArgb("#E7695A")),
			};

		AlterColor1 = new ObservableCollection<Brush>()
			{
				new SolidColorBrush(Color.FromArgb("#346bf5")),
				new SolidColorBrush(Color.FromArgb("#ff9d00")),
			};
	}
}


public class CornerRadiusConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			return new CornerRadius((double)value / 2);
		}

		return 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}
}




internal class LineDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 6;
		// canvas.StrokeDashPattern = new float[] { 2, 2 };
		canvas.DrawLine(10, 10, 90, 100);
	}
}

internal class EllipseDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 4;
		canvas.DrawEllipse(10, 10, 150, 50);
	}
}

internal class FilledEllipseDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Red;
		canvas.FillEllipse(10, 10, 100, 50);
	}
}

internal class CircleDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 4;
		canvas.DrawEllipse(10, 10, 100, 100);
	}
}

internal class RectangleDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.DarkBlue;
		canvas.StrokeSize = 4;
		canvas.DrawRectangle(10, 10, 100, 50);
	}
}

internal class SquareDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.DarkBlue;
		canvas.StrokeSize = 4;
		canvas.DrawRectangle(10, 10, 100, 100);
	}
}

internal class FilledRectangleDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.DarkBlue;
		canvas.FillRectangle(10, 10, 100, 50);
	}
}

internal class RoundedRectangleDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Green;
		canvas.StrokeSize = 4;
		canvas.DrawRoundedRectangle(10, 10, 100, 50, 12);
	}
}

internal class FilledRoundedRectangleDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Green;
		canvas.FillRoundedRectangle(10, 10, 100, 50, 12);
	}
}

internal class ArcDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Teal;
		canvas.StrokeSize = 4;
		canvas.DrawArc(10, 10, 100, 100, 0, 180, true, false);
	}
}

internal class FilledArcDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Teal;
		canvas.FillArc(10, 10, 100, 100, 0, 180, true);
	}
}

internal class PathDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		PathF path = new PathF();
		path.MoveTo(40, 10);
		path.LineTo(70, 80);
		path.LineTo(10, 50);
		path.Close();
		canvas.StrokeColor = Colors.Green;
		canvas.StrokeSize = 6;
		canvas.DrawPath(path);
	}
}

internal class FilledPathDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		PathF path = new PathF();
		path.MoveTo(40, 10);
		path.LineTo(70, 80);
		path.LineTo(10, 50);
		canvas.FillColor = Colors.SlateBlue;
		canvas.FillPath(path);
	}
}

internal class ImageDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		IImage? image = default;
		Assembly assembly = GetType().GetTypeInfo().Assembly;
		using (Stream stream = assembly.GetManifestResourceStream("GraphicsViewDemos.Resources.Images.dotnet_bot.png")!)
		{
#if IOS || ANDROID || MACCATALYST
                // PlatformImage isn't currently supported on Windows.
                image = PlatformImage.FromStream(stream);
#elif WINDOWS
            image = new W2DImageLoadingService().FromStream(stream);
#endif
		}

		if (image != null)
		{
			canvas.DrawImage(image, 10, 10, image.Width, image.Height);
		}
	}
}

internal class StringDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FontColor = Colors.Blue;
		canvas.FontSize = 18;

		canvas.Font = Font.Default;
		canvas.DrawString("Text is left aligned.", 20, 20, 380, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
		canvas.DrawString("Text is centered.", 20, 60, 380, 100, HorizontalAlignment.Center, VerticalAlignment.Top);
		canvas.DrawString("Text is right aligned.", 20, 100, 380, 100, HorizontalAlignment.Right, VerticalAlignment.Top);

		canvas.Font = Font.DefaultBold;
		canvas.DrawString("This text is displayed using the bold system font.", 20, 140, 350, 100, HorizontalAlignment.Left, VerticalAlignment.Top);

		canvas.Font = new Font("Arial");
		canvas.FontColor = Colors.Black;
		canvas.SetShadow(new SizeF(6, 6), 4, Colors.Gray);
		canvas.DrawString("This text has a shadow.", 20, 200, 300, 100, HorizontalAlignment.Left, VerticalAlignment.Top);
	}
}

internal class AttributedTextDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		//canvas.FontName = "Arial";
		//canvas.FontSize = 18;
		//canvas.FontColor = Colors.Blue;

		//string markdownText = @"This is *italic text*, **bold text**, __underline text__, and ***bold italic text***.";
		//IAttributedText attributedText = MarkdownAttributedTextReader.Read(markdownText); // Requires the Microsoft.Maui.Graphics.Text.Markdig package
		//canvas.DrawText(attributedText, 10, 10, 400, 400);
	}
}

internal class FillAndStrokeDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 4;

		PathF path = new PathF();
		path.AppendCircle(dirtyRect.Center.X, dirtyRect.Center.Y, radius);

		canvas.StrokeColor = Colors.Blue;
		canvas.StrokeSize = 10;
		canvas.FillColor = Colors.Red;

		canvas.FillPath(path);
		canvas.DrawPath(path);
	}
}

internal class ShadowDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Red;
		canvas.SetShadow(new SizeF(10, 10), 4, Colors.Grey);
		canvas.FillRectangle(10, 10, 90, 100);

		canvas.FillColor = Colors.Green;
		canvas.SetShadow(new SizeF(10, -10), 4, Colors.Grey);
		canvas.FillEllipse(110, 10, 90, 100);

		canvas.FillColor = Colors.Blue;
		canvas.SetShadow(new SizeF(-10, 10), 4, Colors.Grey);
		canvas.FillRoundedRectangle(210, 10, 90, 100, 25);
	}
}

internal class RegularDashedObjectDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 4;
		canvas.StrokeDashPattern = new float[] { 2, 2 };
		canvas.DrawRectangle(10, 10, 90, 100);
	}
}

internal class IrregularDashedObjectDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeSize = 4;
		canvas.StrokeDashPattern = new float[] { 4, 4, 1, 4 };
		canvas.DrawRectangle(10, 10, 90, 100);
	}
}

internal class LineEndsDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeSize = 10;
		canvas.StrokeColor = Colors.Red;
		canvas.StrokeLineCap = LineCap.Round;
		canvas.DrawLine(10, 10, 110, 110);
	}
}

internal class LineJoinsDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		PathF path = new PathF();
		path.MoveTo(10, 10);
		path.LineTo(110, 50);
		path.LineTo(10, 110);

		canvas.StrokeSize = 20;
		canvas.StrokeColor = Colors.Blue;
		canvas.StrokeLineJoin = LineJoin.Round;
		canvas.DrawPath(path);
	}
}

internal class ClippingDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		IImage? image = default;

		var assembly = GetType().GetTypeInfo().Assembly;
		using (var stream = assembly.GetManifestResourceStream("GraphicsViewDemos.Resources.Images.dotnet_bot.png"))
		{
#if IOS || ANDROID || MACCATALYST
                // PlatformImage isn't currently supported on Windows.
                image = PlatformImage.FromStream(stream);
#elif WINDOWS
            image = new W2DImageLoadingService().FromStream(stream);
#endif
		}

		if (image != null)
		{
			PathF path = new PathF();
			path.AppendCircle(100, 90, 80);
			canvas.ClipPath(path);  // Must be called before DrawImage
			canvas.DrawImage(image, 10, 10, image.Width, image.Height);
		}
	}
}

internal class SubtractClippingDrawable : IDrawable
{
	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		IImage? image = default;
		var assembly = GetType().GetTypeInfo().Assembly;
		using (var stream = assembly.GetManifestResourceStream("GraphicsViewDemos.Resources.Images.dotnet_bot.png"))
		{
#if IOS || ANDROID || MACCATALYST
                // PlatformImage isn't currently supported on Windows.
                image = PlatformImage.FromStream(stream);
#elif WINDOWS
            image = new W2DImageLoadingService().FromStream(stream);
#endif
		}

		if (image != null)
		{
			canvas.SubtractFromClip(60, 60, 90, 90);
			canvas.DrawImage(image, 10, 10, image.Width, image.Height);
		}
	}
}


public class TeamViewModel : INotifyPropertyChanged
{
	#region Fields

	private ObservableCollection<Team> data;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the GettingStartedViewModel class.
	/// </summary>
	public TeamViewModel()
	{
		this.data = new ObservableCollection<Team>();
		this.AddRows();
	}

	#endregion
	/// <summary>
	/// Represents the method that will handle the <see cref="E:System.ComponentModel.INotifyPropertyChanged.PropertyChanged"></see> event raised when a property is changed on a component
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Gets the Data.
	/// </summary>
	/// <value>The Data.</value>
	public ObservableCollection<Team> Data
	{
		get { return this.data; }
	}

	#region updating code

	/// <summary>
	/// Adds the rows.
	/// </summary>
	private void AddRows()
	{
		this.data.Add(new Team("Cavaliers", .616, 0, 93, 58, "cavaliers.png", "East"));
		this.data.Add(new Team("Clippers", .550, 10, 82, 67, "clippers.png", "West"));
		this.data.Add(new Team("Denver", .514, 15, 76, 72, "denvernuggets.png", "Central"));
		this.data.Add(new Team("Detroit", .513, 15, 77, 73, "detroitpistons.png", "East"));
		this.data.Add(new Team("Golden State", .347, 40, 52, 98, "goldenstate.png", "West"));
		this.data.Add(new Team("Los Angeles", .560, 0, 84, 66, "losangeles.png", "Central"));
		this.data.Add(new Team("Mavericks", .547, 2, 82, 68, "mavericks.png", "East"));
		this.data.Add(new Team("Memphis", .540, 3, 81, 69, "memphis.png", "West"));
		this.data.Add(new Team("Miami", .464, 14, 70, 81, "miami.png", "Central"));
		this.data.Add(new Team("Milwakke", .433, 19, 65, 85, "milwakke.png", "East"));
		this.data.Add(new Team("New York", .642, 0, 97, 54, "newyork.png", "West"));
		this.data.Add(new Team("Orlando", .510, 20, 77, 74, "orlando.png", "Central"));
		this.data.Add(new Team("Thunder", .480, 24, 72, 78, "thunder_logo.png", "East"));
	}

	#endregion

	#region INotifyPropertyChanged implementation

	/// <summary>
	/// Triggers when Items Collections Changed.
	/// </summary>
	/// <param name="name">string type of name</param>
	private void RaisePropertyChanged(string name)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}

	#endregion
}


public class Team : INotifyPropertyChanged
{
	#region Private Members

	private string? team;
	private string? location;
	private int wins;
	private int losses;
	private string? logo;
	private double pct;
	private int gb;

	#endregion

	/// <summary>
	/// Represents the method that will handle the <see cref="E:System.ComponentModel.INotifyPropertyChanged.PropertyChanged"></see> event raised when a property is changed on a component
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	#region Public properties

	/// <summary>
	/// Gets or sets the Team.
	/// </summary>
	/// <value>The Team.</value>
	public string? TeamName
	{
		get
		{
			return team;
		}

		set
		{
			team = value;
			RaisePropertyChanged("TeamName");
		}
	}

	/// <summary>
	/// Gets or sets the PCT.
	/// </summary>
	/// <value>The PCT.</value>
	public double PCT
	{
		get
		{
			return pct;
		}

		set
		{
			pct = value;
			RaisePropertyChanged("PCT");
		}
	}

	/// <summary>
	/// Gets or sets the GB.
	/// </summary>
	/// <value>The GB.</value>
	public int GB
	{
		get
		{
			return gb;
		}

		set
		{
			gb = value;
			RaisePropertyChanged("GB");
		}
	}

	/// <summary>
	/// Gets or sets the Wins.
	/// </summary>
	/// <value>The Wins.</value>
	public int Wins
	{
		get
		{
			return wins;
		}

		set
		{
			wins = value;
			RaisePropertyChanged("Wins");
		}
	}

	/// <summary>
	/// Gets or sets the Losses.
	/// </summary>
	/// <value>The Losses.</value>
	public int Losses
	{
		get
		{
			return losses;
		}

		set
		{
			losses = value;
			RaisePropertyChanged("Losses");
		}
	}

	/// <summary>
	/// Gets or sets the team image source.
	/// </summary>
	/// <value>The image source for team.</value>
	public string? Logo
	{
		get
		{
			return logo;
		}

		set
		{
			logo = value;
			RaisePropertyChanged("Logo");
		}
	}

	/// <summary>
	/// Gets or sets the Location.
	/// </summary>
	/// <value>The Location.</value>
	public string? Location
	{
		get
		{
			return location;
		}

		set
		{
			location = value;
			RaisePropertyChanged("Location");
		}
	}

	#endregion

	public Team(string? teamname, double pct, int gb, int wins, int losses, string? logo, string? location)
	{
		this.TeamName = teamname;
		this.PCT = pct;
		this.GB = gb;
		this.Wins = wins;
		this.Losses = losses;
		this.Logo = logo;
		this.Location = location;
	}

	#region INotifyPropertyChanged implementation

	/// <summary>
	/// Triggers when Items Collections Changed.
	/// </summary>
	/// <param name="propertyName">string type of parameter propertyName</param>
	public void RaisePropertyChanged(string propertyName)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	#endregion
}


public class OrderInfoViewModel : INotifyPropertyChanged
{
	#region private variables

	private string? filtertext = string.Empty;
	private string? selectedcolumn = "All Columns";
	private string? selectedcondition = "Contains";

	private ObservableCollection<object>? dataGridSelectedItem;
	private List<DateTime>? orderedDates;
	private Random random = new Random();
	private ObservableCollection<OrderInfo>? ordersInfo;
	public FilterChanged? Filtertextchanged;

	#endregion

	#region MainGrid DataSource
	private string[] genders = new string[]
	{
			"Male",
			"Female",
			"Female",
			"Female",
			"Male",
			"Male",
			"Male",
			"Male",
			"Male",
			"Male",
			"Male",
			"Male",
			"Female",
			"Female",
			"Female",
			"Male",
			"Male",
			"Male",
			"Female",
			"Female",
			"Female",
			"Male",
			"Male",
			"Male",
			"Male"
	};

	private string[] firstNames = new string[]
	{
			"Kyle",
			"Gina",
			"Irene",
			"Katie",
			"Michael",
			"Oscar",
			"Ralph",
			"Torrey",
			"William",
			"Bill",
			"Daniel",
			"Frank",
			"Brenda",
			"Danielle",
			"Fiona",
			"Howard",
			"Jack",
			"Larry",
			"Holly",
			"Jennifer",
			"Liz",
			"Pete",
			"Steve",
			"Vince",
			"Zeke",
			 "Gary",
				"Maciej",
				"Shelley",
				"Linda",
				"Carla",
				"Carol",
				"Shannon",
				"Jauna",
				"Michael",
				"Terry",
				"John",
				"Gail",
				"Mark",
				"Martha",
				"Julie",
				"Janeth",
				"Twanna",
				"Frank",
				"Crowley",
			"Waddell",
			"Irvine",
			"Keefe",
			"Ellis",
			"Gable",
			"Mendoza",
			"Rooney",
			"Lane",
			"Landry",
			"Perry",
			"Perez",
			"Newberry",
			"Betts",
			"Fitzgerald",
			"Adams",
			"Owens",
			"Thomas",
			"Doran",
			"Jefferson",
			"Spencer",
			"Vargas",
			"Grimes",
			"Edwards",
			"Stark",
			"Cruise",
			"Fitz",
			"Chief",
			"Blanc",
			"Stone",
			"Williams",
			"Jobs",
			"Holmes"
	};
	private string[] lastNames = new string[]
	{
			"Adams",
			"Crowley",
			"Ellis",
			"Gable",
			"Irvine",
			"Keefe",
			"Mendoza",
			"Owens",
			"Rooney",
			"Waddell",
			"Thomas",
			"Betts",
			"Doran",
			"Holmes",
			"Jefferson",
			"Landry",
			"Newberry",
			"Perez",
			"Spencer",
			"Vargas",
			"Grimes",
			"Edwards",
			"Stark",
			"Cruise",
			"Fitz",
			"Chief",
			"Blanc",
			"Perry",
			"Stone",
			"Williams",
			"Lane",
			"Jobs"
	};

	private string[] customerID = new string[]
	{
			"Alfki",
			"Frans",
			"Merep",
			"Folko",
			"Simob",
			"Warth",
			"Vaffe",
			"Furib",
			"Seves",
			"Linod",
			"Riscu",
			"Picco",
			"Blonp",
			"Welli",
			"Folig"
	};

	private string[] shipCountry = new string[]
	{
			"Argentina",
			"Austria",
			"Belgium",
			"Brazil",
			"Canada",
			"Denmark",
			"Finland",
			"France",
			"Germany",
			"Ireland",
			"Italy",
			"Mexico",
			"Norway",
			"Poland",
			"Portugal",
			"Spain",
			"Sweden",
			"UK",
			"USA",
	};

	public event PropertyChangedEventHandler? PropertyChanged;

	private Dictionary<string, string[]> shipCity = new Dictionary<string, string[]>();

	#endregion

	/// <summary>
	/// Initializes a new instance of the OrderInfoRepository class.
	/// </summary>
	public OrderInfoViewModel()
	{
		this.OrdersInfo = this.GetOrderDetails(100);
		this.DataGridSelectedItems = new ObservableCollection<object>();
		this.DataGridSelectedItems!.Add(this.OrdersInfo[0]);
		this.DataGridSelectedItems!.Add(this.OrdersInfo[1]);
		this.DataGridSelectedItems!.Add(this.OrdersInfo[3]);
	}

	#region GetOrderDetails

	/// <summary>
	/// Generates record rows with given count
	/// </summary>
	/// <param name="count">integer type of count parameter</param>
	/// <returns>stored Items Values</returns>
	public ObservableCollection<OrderInfo> GetOrderDetails(int count)
	{
		this.SetShipCity();
		this.orderedDates = this.GetDateBetween(2000, 2014, count);
		ObservableCollection<OrderInfo> orderDetails = new ObservableCollection<OrderInfo>();
		int index = 0;
		for (int i = 10001; i <= count + 10000; i++)
		{
			index = index + 1;
			var shipcountry = this.shipCountry[this.random.Next(5)];
			var shipcitycoll = this.shipCity[shipcountry];

			var ord = new OrderInfo()
			{
				OrderID = i,
				CustomerID = this.customerID[this.random.Next(15)],
				EmployeeID = i - 10000 + 2700,
				FirstName = index > 72 ? this.firstNames[this.random.Next(40)] : this.firstNames[index],
				LastName = this.lastNames[this.random.Next(15)],
				Gender = this.genders[this.random.Next(5)],
				ShipCountry = shipcountry,
				ShippingDate = this.orderedDates[i - 10001],
				Freight = Math.Round(this.random.Next(1000) + this.random.NextDouble(), 2),
				Price = Math.Round(this.random.Next(1000) + this.random.NextDouble(), 3),
				IsClosed = (i % this.random.Next(1, 10) > 2) ? true : false,
				ShipCity = shipcitycoll[this.random.Next(shipcitycoll.Length - 1)],
			};
			orderDetails.Add(ord);
		}

		return orderDetails;
	}


	#endregion
	/// <summary>
	/// Used to send a Notification while Filter Changed
	/// </summary>
	public delegate void FilterChanged();

	/// <summary>
	/// Gets or sets the value of FilterText and notifies user when value gets changed
	/// </summary>
	public string? FilterText
	{
		get
		{
			return this.filtertext;
		}

		set
		{
			this.filtertext = value;
			this.OnFilterTextChanged();
			this.RaisePropertyChanged("FilterText");
		}
	}

	public ObservableCollection<object>? DataGridSelectedItems
	{
		get
		{
			return dataGridSelectedItem;
		}
		set
		{
			this.dataGridSelectedItem = value;
			RaisePropertyChanged("DataGridSelectedItems");
		}
	}


	/// <summary>
	/// Gets or sets the value of SelectedCondition
	/// </summary>
	public string? SelectedCondition
	{
		get { return this.selectedcondition; }
		set { this.selectedcondition = value; }
	}

	/// <summary>
	/// Gets or sets the value of SelectedColumn
	/// </summary>
	public string? SelectedColumn
	{
		get { return this.selectedcolumn; }
		set { this.selectedcolumn = value; }
	}

	/// Gets or sets the value of OrdersInfo and notifies user when value gets changed
	public ObservableCollection<OrderInfo>? OrdersInfo
	{
		get
		{
			return this.ordersInfo;
		}

		set
		{
			this.ordersInfo = value;
			this.RaisePropertyChanged("OrdersInfo");
		}
	}

	/// <summary>
	/// used to decide generate records or not
	/// </summary>
	/// <param name="o">object type parameter</param>
	/// <returns>true or false value</returns>
	public bool FilerRecords(object o)
	{
		double res;
		bool checkNumeric = double.TryParse(this.FilterText, out res);
		var item = o as OrderInfo;
		if (item != null && this.FilterText!.Equals(string.Empty) && !string.IsNullOrEmpty(this.FilterText))
		{
			return true;
		}
		else
		{
			if (item != null)
			{
				if (checkNumeric && !this.SelectedColumn!.Equals("All Columns") && !this.SelectedCondition!.Equals("Contains"))
				{
					bool result = this.MakeNumericFilter(item, this.SelectedColumn, this.SelectedCondition);
					return result;
				}
				else if (this.SelectedColumn!.Equals("All Columns"))
				{
					if (item.OrderID!.ToString().ToLower().Contains(this.FilterText!.ToLower()) ||
						item.FirstName!.ToString().ToLower().Contains(this.FilterText.ToLower()) ||
						item.CustomerID!.ToString().ToLower().Contains(this.FilterText.ToLower()) ||
						item.ShipCity!.ToString().ToLower().Contains(this.FilterText.ToLower()) ||
						item.ShipCountry!.ToString().ToLower().Contains(this.FilterText.ToLower()))
					{
						return true;
					}

					return false;
				}
				else
				{
					bool result = this.MakeStringFilter(item, this.SelectedColumn, this.SelectedCondition!);
					return result;
				}
			}
		}

		return false;
	}


	/// <summary>
	/// Used to call the filter text changed()
	/// </summary>
	private void OnFilterTextChanged()
	{
		if (this.Filtertextchanged != null)
		{
			this.Filtertextchanged();
		}
	}

	private bool MakeStringFilter(OrderInfo o, string option, string condition)
	{
		var value = o.GetType().GetProperty(option);
		var exactValue = value!.GetValue(o, null);
		exactValue = exactValue!.ToString()!.ToLower();
		string text = this.FilterText!.ToLower();
		var methods = typeof(string).GetMethods();

		if (methods.Count() != 0)
		{
			if (condition == "Contains")
			{
				var methodInfo = methods.FirstOrDefault(method => method.Name == condition);
				bool result1 = (bool)methodInfo!.Invoke(exactValue!, new object[] { text })!;
				return result1;
			}
			else if (exactValue.ToString() == text.ToString())
			{
				bool result1 = string.Equals(exactValue.ToString(), text.ToString());
				if (condition == "Equals")
				{
					return result1;
				}
				else if (condition == "NotEquals")
				{
					return false;
				}
			}
			else if (condition == "NotEquals")
			{
				return true;
			}

			return false;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Used decide to make the numeric filter
	/// </summary>
	/// <param name="o">o</param>
	/// <param name="option">option</param>
	/// <param name="condition">condition</param>
	/// <returns>true or false value</returns>
	private bool MakeNumericFilter(OrderInfo o, string option, string condition)
	{
		var value = o.GetType().GetProperty(option);
		var exactValue = value!.GetValue(o, null);
		double res;
		bool checkNumeric = double.TryParse(exactValue!.ToString(), out res);
		if (checkNumeric)
		{
			switch (condition)
			{
				case "Equals":
					try
					{
						if (exactValue.ToString() == this.FilterText)
						{
							if (Convert.ToDouble(exactValue) == Convert.ToDouble(this.FilterText))
							{
								return true;
							}
						}
					}
					catch (Exception e)
					{
						Debug.WriteLine(e.Message);
					}

					break;
				case "NotEquals":
					try
					{
						if (Convert.ToDouble(this.FilterText) != Convert.ToDouble(exactValue))
						{
							return true;
						}
					}
					catch (Exception e)
					{
						Debug.WriteLine(e.Message);
						return true;
					}

					break;
			}
		}

		return false;
	}

	private void RaisePropertyChanged(string name)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}

	/// <summary>
	/// Used to generate DateTime and returns the value
	/// </summary>
	/// <param name="startYear">integer type of parameter startYear</param>
	/// <param name="endYear">integer type of parameter endYear</param>
	/// <param name="count">integer type of parameter count</param>
	/// <returns>returns the generated DateTime</returns>
	private List<DateTime> GetDateBetween(int startYear, int endYear, int count)
	{
		List<DateTime> date = new List<DateTime>();
		Random d = new Random(1);
		Random m = new Random(2);
		Random y = new Random(startYear);
		for (int i = 0; i < count; i++)
		{
			int year = y.Next(startYear, endYear);
			int month = m.Next(3, 13);
			int day = d.Next(1, 31);

			date.Add(new DateTime(year, month, day));
		}

		return date;
	}

	/// <summary>
	/// This method used to store the string items collections Value
	/// </summary>
	private void SetShipCity()
	{
		string[] argentina = new string[]
		{
				"Rosario"
		};

		string[] austria = new string[]
		{
				"Graz",
				"Salzburg"
		};

		string[] belgium = new string[]
		{
				"Bruxelles",
				"Charleroi"
		};

		string[] brazil = new string[]
		{
				"Campinas",
				"Resende",
				"Recife",
				"Manaus"
		};

		string[] canada = new string[]
		{
				"Montréal",
				"Tsawassen",
				"Vancouver"
		};

		string[] denmark = new string[]
		{
				"Århus",
				"København"
		};

		string[] finland = new string[]
		{
				"Helsinki",
				"Oulu"
		};

		string[] france = new string[]
		{
				"Lille",
				"Lyon",
				"Marseille",
				"Nantes",
				"Paris",
				"Reims",
				"Strasbourg",
				"Toulouse",
				"Versailles"
		};

		string[] germany = new string[]
		{
				"Aachen",
				"Berlin",
				"Brandenburg",
				"Cunewalde",
				"Frankfurt",
				"Köln",
				"Leipzig",
				"Mannheim",
				"München",
				"Münster",
				"Stuttgart"
		};

		string[] ireland = new string[]
		{
				"Cork"
		};

		string[] italy = new string[]
		{
				"Bergamo",
				"Reggio",
				"Torino"
		};

		string[] mexico = new string[]
		{
				"México D.F."
		};

		string[] norway = new string[]
		{
				"Stavern"
		};

		string[] poland = new string[]
		{
				"Warszawa"
		};

		string[] portugal = new string[]
		{
				"Lisboa"
		};

		string[] spain = new string[]
		{
				"Barcelona",
				"Madrid",
				"Sevilla"
		};

		string[] sweden = new string[]
		{
				"Bräcke",
				"Luleå"
		};

		string[] switzerland = new string[]
		{
				"Bern",
				"Genève"
		};

		string[] uk = new string[]
		{
				"Colchester",
				"Hedge End",
				"London"
		};

		string[] usa = new string[]
		{
				"Albuquerque",
				"Anchorage",
				"Boise",
				"Butte",
				"Elgin",
				"Eugene",
				"Kirkland",
				"Lander",
				"Portland",
				"San Francisco",
				"Seattle",
		};

		string[] venezuela = new string[]
		{
				"Barquisimeto",
				"Caracas", "I. de Margarita",
				"San Cristóbal"
		};

		this.shipCity.Add("Argentina", argentina);
		this.shipCity.Add("Austria", austria);
		this.shipCity.Add("Belgium", belgium);
		this.shipCity.Add("Brazil", brazil);
		this.shipCity.Add("Canada", canada);
		this.shipCity.Add("Denmark", denmark);
		this.shipCity.Add("Finland", finland);
		this.shipCity.Add("France", france);
		this.shipCity.Add("Germany", germany);
		this.shipCity.Add("Ireland", ireland);
		this.shipCity.Add("Italy", italy);
		this.shipCity.Add("Mexico", mexico);
		this.shipCity.Add("Norway", norway);
		this.shipCity.Add("Poland", poland);
		this.shipCity.Add("Portugal", portugal);
		this.shipCity.Add("Spain", spain);
		this.shipCity.Add("Sweden", sweden);
		this.shipCity.Add("Switzerland", switzerland);
		this.shipCity.Add("UK", uk);
		this.shipCity.Add("USA", usa);
		this.shipCity.Add("Venezuela", venezuela);
	}
}



public class OrderInfo : INotifyPropertyChanged
{
	#region private variables

	private int orderID;
	private int employeeID;
	private string? customerID;
	private string? firstname;
	private string? lastname;
	private string? gender;
	private string? shipCity;
	private string? shipCountry;
	private double freight;
	private DateTime shippingDate;
	private bool isClosed;
	private double price;

	#endregion

	/// <summary>
	/// Initializes a new instance of the OrderInfo class.
	/// </summary>
	public OrderInfo()
	{
	}

	/// <summary>
	/// Represents the method that will handle the <see cref="E:System.ComponentModel.INotifyPropertyChanged.PropertyChanged"></see> event raised when a property is changed on a component
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	#region Public Properties

	/// <summary>
	/// Gets or sets the value of OrderID and notifies user when value gets changed
	/// </summary>
	public int OrderID
	{
		get
		{
			return this.orderID;
		}

		set
		{
			this.orderID = value;
			this.RaisePropertyChanged("OrderID");
		}
	}

	/// <summary>
	/// Gets or sets the value of EmployeeID and notifies user when value gets changed
	/// </summary>
	public int EmployeeID
	{
		get
		{
			return this.employeeID;
		}

		set
		{
			this.employeeID = value;
			this.RaisePropertyChanged("EmployeeID");
		}
	}

	/// <summary>
	/// Gets or sets the value of CustomerID and notifies user when value gets changed
	/// </summary>
	public string? CustomerID
	{
		get
		{
			return this.customerID;
		}

		set
		{
			this.customerID = value;
			this.RaisePropertyChanged("CustomerID");
		}
	}

	/// <summary>
	/// Gets or sets the value of FirstName and notifies user when value gets changed
	/// </summary>
	public string? FirstName
	{
		get
		{
			return this.firstname;
		}

		set
		{
			this.firstname = value;
			this.RaisePropertyChanged("FirstName");
		}
	}

	/// <summary>
	/// Gets or sets the value of LastName and notifies user when value gets changed
	/// </summary>
	public string? LastName
	{
		get
		{
			return this.lastname;
		}

		set
		{
			this.lastname = value;
			this.RaisePropertyChanged("LastName");
		}
	}

	/// <summary>
	/// Gets or sets the value of Gender and notifies user when value gets changed
	/// </summary>
	public string? Gender
	{
		get
		{
			return this.gender;
		}

		set
		{
			this.gender = value;
			this.RaisePropertyChanged("Gender");
		}
	}

	/// <summary>
	/// Gets or sets the value of ShipCity and notifies user when value gets changed
	/// </summary>
	public string? ShipCity
	{
		get
		{
			return this.shipCity;
		}

		set
		{
			this.shipCity = value;
			this.RaisePropertyChanged("ShipCity");
		}
	}

	/// <summary>
	/// Gets or sets the value of ShipCountry and notifies user when value gets changed
	/// </summary>
	public string? ShipCountry
	{
		get
		{
			return this.shipCountry;
		}

		set
		{
			this.shipCountry = value;
			this.RaisePropertyChanged("ShipCountry");
		}
	}

	/// <summary>
	/// Gets or sets the value of Freight and notifies user when value gets changed
	/// </summary>
	public double Freight
	{
		get
		{
			return this.freight;
		}

		set
		{
			this.freight = value;
			this.RaisePropertyChanged("Freight");
		}
	}

	/// <summary>
	///
	/// </summary>
	public double Price
	{
		get
		{
			return this.price;
		}

		set
		{
			this.price = value;
			this.RaisePropertyChanged("Price");
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether IsClosed is true or false and notifies user when value gets changed
	/// </summary>
	public bool IsClosed
	{
		get
		{
			return this.isClosed;
		}

		set
		{
			this.isClosed = value;
			this.RaisePropertyChanged("IsClosed");
		}
	}

	/// <summary>
	/// Gets or sets the value of ShippingDate and notifies user when value gets changed
	/// </summary>
	public DateTime ShippingDate
	{
		get
		{
			return this.shippingDate;
		}

		set
		{
			this.shippingDate = value;
			this.RaisePropertyChanged("ShippingDate");
		}
	}

	#endregion

	#region INotifyPropertyChanged implementation

	/// <summary>
	/// Triggers when Items Collections Changed.
	/// </summary>
	/// <param name="name">string type parameter name</param>
	private void RaisePropertyChanged(string name)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}

	#endregion
}
