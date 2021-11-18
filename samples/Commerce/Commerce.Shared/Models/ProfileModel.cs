using System;
using System.Collections.Generic;
using System.Text;

namespace Commerce.Models
{
    public record ProfileModel(Profile Profile)
    {
		public string FullName => $"{Profile.FirstName} {Profile.LastName}";

		public string Avatar => Profile.Avatar;
    }
}
