//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate.Views.Content
{
    public sealed partial class DiagnosticsOverlay : UserControl
    {
        public DiagnosticsOverlay()
        {
            this.InitializeComponent();
        }
    }


    /// <summary>
    /// Taken from the CommunityToolkit - until there there is a Winui/uno compatible version
    /// </summary>
    public partial class AlignmentGrid : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="P:Microsoft.Toolkit.Uwp.DeveloperTools.AlignmentGrid.LineBrush" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register("LineBrush", typeof(Brush), typeof(AlignmentGrid), new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Microsoft.Toolkit.Uwp.DeveloperTools.AlignmentGrid.HorizontalStep" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalStepProperty = DependencyProperty.Register("HorizontalStep", typeof(double), typeof(AlignmentGrid), new PropertyMetadata(20.0, OnPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Microsoft.Toolkit.Uwp.DeveloperTools.AlignmentGrid.VerticalStep" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalStepProperty = DependencyProperty.Register("VerticalStep", typeof(double), typeof(AlignmentGrid), new PropertyMetadata(20.0, OnPropertyChanged));

        private readonly Canvas containerCanvas = new Canvas();

        /// <summary>
        /// Gets or sets the step to use horizontally.
        /// </summary>
        public Brush LineBrush
        {
            get
            {
                return (Brush)GetValue(LineBrushProperty);
            }
            set
            {
                SetValue(LineBrushProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the step to use horizontally.
        /// </summary>
        public double HorizontalStep
        {
            get
            {
                return (double)GetValue(HorizontalStepProperty);
            }
            set
            {
                SetValue(HorizontalStepProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the step to use horizontally.
        /// </summary>
        public double VerticalStep
        {
            get
            {
                return (double)GetValue(VerticalStepProperty);
            }
            set
            {
                SetValue(VerticalStepProperty, value);
            }
        }

        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as AlignmentGrid)?.Rebuild();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Toolkit.Uwp.DeveloperTools.AlignmentGrid" /> class.
        /// </summary>
        public AlignmentGrid()
        {
            base.SizeChanged += AlignmentGrid_SizeChanged;
            base.IsHitTestVisible = false;
            base.IsTabStop = false;
            base.Opacity = 0.5;
            base.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            base.VerticalContentAlignment = VerticalAlignment.Stretch;
            base.Content = containerCanvas;
        }

        private void Rebuild()
        {
            containerCanvas.Children.Clear();
            double horizontalStep = HorizontalStep;
            double verticalStep = VerticalStep;
            Brush brush = LineBrush ?? ((Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"]);
            if (horizontalStep > 0.0)
            {
                for (double x = 0.0; x < base.ActualWidth; x += horizontalStep)
                {
                    Rectangle line2 = new Rectangle
                    {
                        Width = 1.0,
                        Height = base.ActualHeight,
                        Fill = brush
                    };
                    Canvas.SetLeft(line2, x);
                    containerCanvas.Children.Add(line2);
                }
            }
            if (verticalStep > 0.0)
            {
                for (double y = 0.0; y < base.ActualHeight; y += verticalStep)
                {
                    Rectangle line = new Rectangle
                    {
                        Width = base.ActualWidth,
                        Height = 1.0,
                        Fill = brush
                    };
                    Canvas.SetTop(line, y);
                    containerCanvas.Children.Add(line);
                }
            }
        }

        private void AlignmentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Rebuild();
        }
    }
}
