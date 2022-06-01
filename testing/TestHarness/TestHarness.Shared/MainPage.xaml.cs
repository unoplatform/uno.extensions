
namespace TestHarness;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		var testSections = (from type in this.GetType().Assembly.GetTypes()
							let sectionAttribute = type.GetCustomAttribute<TestSectionRootAttribute>()
							where sectionAttribute is not null
							select new TestSection(sectionAttribute.Name, type, sectionAttribute.HostInitializer)).ToArray();
		TestSectionsComboBox.ItemsSource = testSections;
	}


	private void TestSectionSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (TestSectionsComboBox.SelectedItem is TestSection section)
		{

			this.Frame.Navigate(section.MainPage, Activator.CreateInstance(section.HostInitializer));
		}
	}
}


