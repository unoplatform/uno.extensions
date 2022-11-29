using Uno.Toolkit.UI;

namespace UnoExtApp1.Views
{
	public sealed partial class Shell : UserControl, IContentControlProvider
	{
		public Shell()
		{
			this.InitializeComponent();
		}

		public ContentControl ContentControl => Splash;
	}
}