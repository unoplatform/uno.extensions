using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.UI.Xaml.Data;

namespace ApplicationTemplate.Client
{
	//[Bindable]
	[GeneratedImmutable]
	public partial class UserProfileData
	{
		[EqualityKey]
		public string Id { get; }

		public string FirstName { get; }

		public string LastName { get; }
	}
}
