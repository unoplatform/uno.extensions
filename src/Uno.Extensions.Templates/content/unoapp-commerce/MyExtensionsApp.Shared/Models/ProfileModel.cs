//-:cnd:noEmit
using System;
using System.Collections.Generic;
using System.Text;

namespace MyExtensionsApp.Models
{
    public record ProfileModel(Profile Profile)
    {
		public string FullName => $"{Profile.FirstName} {Profile.LastName}";

		public string Avatar => Profile.Avatar;
    }
}
