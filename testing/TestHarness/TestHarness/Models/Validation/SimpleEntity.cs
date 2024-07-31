using System.ComponentModel;

namespace TestHarness.Models;
public class SimpleEntity : IValidatableObject, INotifyPropertyChanged
{
	private string _title;
	private string _titleErrors;

	public string Title
	{
		get { return _title; }
		set
		{
			_title = value;
			OnPropertyChanged(nameof(Title));
		}
	}

	public string TitleErrors
	{
		get { return _titleErrors; }
		set
		{
			_titleErrors = value;
			OnPropertyChanged(nameof(TitleErrors));
		}
	}
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		return string.IsNullOrWhiteSpace(Title) ?
			new ValidationResult[] {
					new ValidationResult(
						errorMessage: "Title should not be null",
						memberNames: new[] {nameof(Title) }
						)
			} :
			default;
	}

	#region PropertyChanged
	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged(string name)
	{
		PropertyChangedEventHandler handler = PropertyChanged;

		if (handler != null)
		{
			handler(this, new PropertyChangedEventArgs(name));
		}
	}
	#endregion
}
