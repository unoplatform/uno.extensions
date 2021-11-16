using Commerce.Models;

namespace Commerce.ViewModels
{
	public class ProfileViewModel
    {
		private Profile _person;
		public ProfileViewModel()
		{
			_person = new Profile { FirstName = "Fred", LastName = "Jobs" };
		}

		public string FullName => $"{_person.FirstName} {_person.LastName}";
    }
}
