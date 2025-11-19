namespace TestHarness.Ext.Navigation.ForResult;

[TestSectionRoot("Navigation ForResult", TestSections.Navigation_ForResult, typeof(ForResultHostInit))]
public sealed partial class ForResultMainPage : BaseTestSectionPage
{
	public ForResultMainPage()
	{
		this.InitializeComponent();
	}

	protected override TestSection? Section => TestSection.From(TestSections.Navigation_ForResult, this.GetType());
}
