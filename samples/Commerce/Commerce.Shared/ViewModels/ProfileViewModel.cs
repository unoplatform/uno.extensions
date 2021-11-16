using Commerce.Models;

namespace Commerce.ViewModels
{
	public class ProfileViewModel
    {
		private Person _person;
		public ProfileViewModel()
		{
			_person = new Person { FirstName = "Fred", LastName = "Jobs" };
		}

		public string FullName => $"{_person.FirstName} {_person.LastName}";
    }
}
