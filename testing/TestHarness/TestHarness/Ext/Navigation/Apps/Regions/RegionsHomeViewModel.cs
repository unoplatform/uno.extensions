using System.ComponentModel;

namespace TestHarness.Ext.Navigation.Apps.Regions;

public class RegionsHomeViewModel : INotifyPropertyChanged
{
	private string _myText = "Type Something and go to Third Page";

	public string MyText
	{
		get => _myText;
		set
		{
			if (_myText != value)
			{
				_myText = value;
				OnPropertyChanged(nameof(MyText));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
