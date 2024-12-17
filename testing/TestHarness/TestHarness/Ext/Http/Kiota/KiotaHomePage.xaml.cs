namespace TestHarness.Ext.Http.Kiota
{
	[TestSectionRoot("Http: Kiota", TestSections.Http_Kiota, typeof(KiotaHostInit))]

	public sealed partial class KiotaHomePage : Page
	{
		internal KiotaHomeViewModel? ViewModel => DataContext as KiotaHomeViewModel;

		public KiotaHomePage()
		{
			this.InitializeComponent();
		}
	}
}
