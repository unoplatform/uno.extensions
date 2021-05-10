using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;

namespace ApplicationTemplate.Presentation
{
	public class LicensesPageViewModel : ViewModel
	{
		private const string LicensesFileName = "ThirdPartySoftwareLicenses.txt";

		public string Licenses => this.GetFromTask(GetLicenses, initialValue: string.Empty);

		private async Task<string> GetLicenses(CancellationToken ct)
		{
			var assembly = GetType().Assembly;

			var actualResourceName = assembly
				.GetManifestResourceNames()?.FirstOrDefault(name => name
				.EndsWith(LicensesFileName, StringComparison.OrdinalIgnoreCase)
			);

			if (actualResourceName == null)
			{
//-:cnd:noEmit
#if __PRODUCTION__
//+:cnd:noEmit
				return string.Empty;
//-:cnd:noEmit
#else
//+:cnd:noEmit
				return $"Couldn't locate '{LicensesFileName}'. Please generate it using the GenerateThirdPartySoftwareLicenses script available in the project.";
//-:cnd:noEmit
#endif
//+:cnd:noEmit
			}

			var resourceStream = assembly.GetManifestResourceStream(actualResourceName);

			using (var streamReader = new StreamReader(resourceStream))
			{
				return await streamReader.ReadToEndAsync();
			}
		}
	}
}
