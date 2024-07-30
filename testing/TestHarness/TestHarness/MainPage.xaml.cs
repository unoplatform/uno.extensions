
namespace TestHarness;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();

		this.GetThemeService();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		var attributedSections = (from type in this.GetType().Assembly.GetTypes()
								  let attributes = type.GetCustomAttributes<TestSectionRootAttribute>()
								  from sectionAttribute in attributes
								  where sectionAttribute is not null
								  select new TestSection(sectionAttribute.Name, sectionAttribute.Section, sectionAttribute.HostInitializer, type)).ToArray();
		var allSections = typeof(TestSections).GetEnumValues().OfType<TestSections>().OrderBy(x => (int)x).ToArray();

		var testSections = (from t in allSections
							let section = attributedSections.FirstOrDefault(x => x.Section == t) ?? new TestSection("--invalid", t, default!, default!)
							select section).ToArray();

		TestSectionsListView.ItemsSource = testSections;
	}


	private void TestSectionSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (TestSectionsListView.SelectedItem is TestSection section &&
			section.MainPage is not null)
		{
			var hostInit = section.HostInitializer is not null ?
				Activator.CreateInstance(section.HostInitializer) :
				default;
			this.Frame.Navigate(section.MainPage, hostInit);
		}
	}
}


