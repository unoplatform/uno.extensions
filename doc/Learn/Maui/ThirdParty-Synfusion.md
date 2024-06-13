---
uid: Uno.Extensions.Maui.ThirdParty.Syncfusion
---
# .NET MAUI Embedding - Syncfusion .NET MAUI Controls

The controls from Syncfusion .NET MAUI Controls can be used in an Uno Platform application via .NET MAUI Embedding.

## Sample App

An existing sample app that showcases the controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MauiEmbedding/SyncfusionApp).

## Installation

In order to use the Syncfusion controls, you will need to create an account and purchase a Syncfusion license [here](https://help.syncfusion.com/maui/licensing/overview). This sample can be run in development without a license key.

## Getting Started

1. Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case, we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

    ```dotnetcli
    dotnet new unoapp -preset blank -maui -o MauiEmbeddingApp
    ```

1. Next, add a reference to the [Syncfusion.Maui.Charts package](https://www.nuget.org/packages/Syncfusion.Maui.Charts) to the `MauiEmbeddingApp.MauiControls` project.

1. In the `AppBuilderExtensions` class, on `MauiEmbeddingApp.MauiControls` project, update the `UseMauiControls` extension method to call the `ConfigureSyncfusionCore` method.

    ```cs
    using Syncfusion.Maui.Core.Hosting;

        namespace MauiEmbeddingApp;

        public static class AppBuilderExtensions
        {
            public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder)
                => builder
                    .ConfigureSyncfusionCore()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                        fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
                    });
        }
    ```

## Adding SfCircularChart

1. Update the EmbeddedControl.xaml in the  `MauiEmbeddingApp.MauiControls` project with the following XAML that includes the `SfCircularChart` control, which will be used to display a doughnut shaped chart.

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
                x:Class="MauiEmbeddingApp.MauiControls.EmbeddedControl"
                xmlns:chart="http://schemas.syncfusion.com/maui"
                xmlns:local="clr-namespace:MauiEmbeddingApp.MauiControls.ViewModels"
                HorizontalOptions="Fill"
                VerticalOptions="Fill">
        <chart:SfCircularChart x:Name="chart"
                            HorizontalOptions="Fill"
                            VerticalOptions="Fill">
            <chart:SfCircularChart.Title>
                <Label Text="Project Cost Breakdown"
                    Margin="0"
                    HorizontalOptions="Fill"
                    HorizontalTextAlignment="Center"
                    VerticalOptions="Center"
                    FontSize="16"
                    TextColor="Black" />
            </chart:SfCircularChart.Title>
            <chart:SfCircularChart.Legend>
                <chart:ChartLegend />
            </chart:SfCircularChart.Legend>
            <chart:SfCircularChart.Series>
                <chart:DoughnutSeries x:Name="series"
                                    ExplodeIndex="{Binding SelectedIndex}"
                                    ExplodeOnTouch="True"
                                    ShowDataLabels="True"
                                    Radius="0.9"
                                    PaletteBrushes="{Binding PaletteBrushes}"
                                    ItemsSource="{Binding DoughnutSeriesData}"
                                    XBindingPath="Name"
                                    YBindingPath="Value"
                                    EnableAnimation="False"
                                    StrokeWidth="1"
                                    Stroke="White"
                                    LegendIcon="SeriesType">
                    <chart:DoughnutSeries.CenterView>
                        <StackLayout x:Name="layout"
                                    HeightRequest="{Binding CenterHoleSize}"
                                    WidthRequest="{Binding CenterHoleSize}">
                            <Label Text="{Binding Name,Source={x:Reference embeddedViewModel}}"
                                FontSize="13"
                                HorizontalOptions="Center"
                                VerticalOptions="EndAndExpand"
                                Margin="5" />
                            <Label Text="{Binding Value,Source={x:Reference embeddedViewModel},StringFormat='{0}%'}"
                                FontSize="12"
                                HorizontalOptions="Center"
                                VerticalOptions="StartAndExpand"
                                Margin="5" />
                        </StackLayout>
                    </chart:DoughnutSeries.CenterView>
                    <chart:DoughnutSeries.DataLabelSettings>
                        <chart:CircularDataLabelSettings>
                            <chart:CircularDataLabelSettings.LabelStyle>
                                <chart:ChartDataLabelStyle LabelFormat="0'M" />
                            </chart:CircularDataLabelSettings.LabelStyle>
                        </chart:CircularDataLabelSettings>
                    </chart:DoughnutSeries.DataLabelSettings>
                </chart:DoughnutSeries>
            </chart:SfCircularChart.Series>
        </chart:SfCircularChart>
    </ContentView>
    ```

1. Update the EmbeddedControl.xaml.cs with the following code.

    ```cs
    namespace MauiEmbeddingApp.MauiControls;

    public partial class EmbeddedControl : ContentView
    {
        public EmbeddedControl()
        {
            InitializeComponent();
        }
    }
    ```

1. It's time to create the ViewModel that will hold the properties that will be data bound to the `SfCircularChart` control. In `MauiEmbeddingApp.MauiControls` project, create a new folder called `ViewModels` and add a new class called `EmbeddedViewModel`. This class will have the following code:

    ```cs
    using MauiEmbeddingApp.MauiControls.Models;
    using Syncfusion.Maui.Charts;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;

    namespace MauiEmbeddingApp.MauiControls.ViewModels;

    public class EmbeddedViewModel : INotifyPropertyChanged
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


        public ObservableCollection<ChartDataModel> DoughnutSeriesData { get; set; }
        public ObservableCollection<ChartDataModel> SemiCircularData { get; set; }
        public ObservableCollection<ChartDataModel> CenterElevationData { get; set; }
        public ObservableCollection<ChartDataModel> GroupToData { get; set; }

        private int selectedIndex = 1;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                UpdateIndex(value);
                OnPropertyChanged("SelectedIndex");
            }
        }
        private string name = "";
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        private int value1;
        public int Value
        {
            get { return value1; }
            set
            {
                value1 = value;
                OnPropertyChanged("Value");
            }
        }

        private int total = 357580;
        public int Total
        {
            get { return total; }
            set
            {
                total = value;
            }
        }

        private void UpdateIndex(int value)
        {
            if (value >= 0 && value < DoughnutSeriesData.Count)
            {
                var model = DoughnutSeriesData[value];
                if (model != null && model.Name != null)
                {
                    Name = model.Name;
                    double sum = DoughnutSeriesData.Sum(item => item.Value);
                    double SelectedItemsPercentage = model.Value / sum * 100;
                    SelectedItemsPercentage = Math.Floor(SelectedItemsPercentage * 100) / 100;
                    Value = (int)SelectedItemsPercentage;
                }
            }
        }

        public EmbeddedViewModel()
        {
            DoughnutSeriesData = new ObservableCollection<ChartDataModel>
                {
                    new ChartDataModel("Labor", 10),
                    new ChartDataModel("Legal", 8),
                    new ChartDataModel("Production", 7),
                    new ChartDataModel("License", 5),
                    new ChartDataModel("Facilities", 10),
                    new ChartDataModel("Taxes", 6),
                    new ChartDataModel("Insurance", 18)
            };

            SemiCircularData = new ObservableCollection<ChartDataModel>
                {
                    new ChartDataModel("Product A", 750),
                    new ChartDataModel("Product B", 463),
                    new ChartDataModel("Product C", 389),
                    new ChartDataModel("Product D", 697),
                    new ChartDataModel("Product E", 251)
                };

            CenterElevationData = new ObservableCollection<ChartDataModel>
                {
                    new ChartDataModel("Agriculture",51),
                    new ChartDataModel("Forest",30),
                    new ChartDataModel("Water",5.2),
                    new ChartDataModel("Others",14),
                };

            GroupToData = new ObservableCollection<ChartDataModel>
                {
                    new ChartDataModel("US",51.30,0.39),
                    new ChartDataModel("China",20.90,0.16),
                    new ChartDataModel("Japan",11.00,0.08),
                    new ChartDataModel("France",4.40,0.03),
                    new ChartDataModel("UK",4.30,0.03),
                    new ChartDataModel ("Canada",4.00,0.03),
                    new ChartDataModel("Germany",3.70,0.03),
                    new ChartDataModel("Italy",2.90,0.02),
                    new ChartDataModel("KY",2.70,0.02),
                    new ChartDataModel("Brazil",2.40,0.02),
                    new ChartDataModel("South Korea",2.20,0.02),
                    new ChartDataModel("Australia",2.20,0.02),
                    new ChartDataModel("Netherlands",1.90,0.01),
                    new ChartDataModel("Spain",1.90,0.01),
                    new ChartDataModel("India",1.30,0.01),
                    new ChartDataModel("Ireland",1.00,0.01),
                    new ChartDataModel("Mexico",1.00,0.01),
                    new ChartDataModel("Luxembourg",0.90,0.01),
                };

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
    ```

1. The `EmbeddedViewModel` class is dependent on a `ChartDataModel` class which needs to be created. In `MauiEmbeddingApp.MauiControls` project, create a new folder called `Models` and add a new class called `ChartDataModel`. This class will have the following code:

    ```cs
    namespace MauiEmbeddingApp.MauiControls.Models;

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
    ```

1. The next step is to add the `EmbeddedViewModel` as the `DataContext` of the `SfCircularChart` in the `EmbeddedControl.xaml`:

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentView x:Class="MauiEmbeddingApp.MauiControls.EmbeddedControl"
                ...
                >
        <chart:SfCircularChart x:Name="chart"
                            HorizontalOptions="Fill"
                            VerticalOptions="Fill">
            <chart:SfCircularChart.BindingContext>
                <local:EmbeddedViewModel x:Name="embeddedViewModel" />
            </chart:SfCircularChart.BindingContext>
            ...
    ```

1. Now the project is good to go! Press F5 and you should see the `SfCircularChart` control. Without specifying a license key, you'll see a prompt informing you that you're running the application using a trial version.

    > [!NOTE]
    > There is a known issue with the trial version prompt that can cause the application to crash on startup. If this is encountered running this sample, the `MainPage.xaml` can be updated to remove the `Source` property on the `MauiHost`. The `Source` property can then be set in the code behind file in an event handler for the `Loaded` event of the `Page`.

    ```cs
    namespace MauiEmbeddingApp;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            MauiHostElement.Source = typeof(EmbeddedControl);
        }
    }
    ```

## App Render Output

- **Android:**
  - ![Android Syncfusion](Assets/Screenshots/Android/Syncfusion.png)

- **Windows:**
  - ![Windows Syncfusion](Assets/Screenshots/Windows/Syncfusion.png)
