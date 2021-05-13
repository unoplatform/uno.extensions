using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Chinook.DynamicMvvm;
//using MessageDialogService;

namespace ApplicationTemplate.Presentation
{
    public class CultureDiagnosticsViewModel : ViewModel
    {
        public string Culture
        {
            get => this.Get(CultureInfo.CurrentCulture.Name);
            set => this.Set(value);
        }

        public IDynamicCommand SaveCulture => this.GetCommandFromTask(async ct =>
        {
            var newCulture = new CultureInfo(Culture);

            this.GetService<ThreadCultureOverrideService>().SetCulture(newCulture);

            if (CultureInfo.CurrentCulture.Name != newCulture.Name)
            {
                await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                    .Title("Diagnostics")
                    .Content("The changes will be applied on the next app restart.")
                    .OkCommand()
                );
            }
        }, c => c.CatchErrors(OnSaveCultureError));

        private async Task OnSaveCultureError(CancellationToken ct, IDynamicCommand c, Exception e)
        {
            await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                .Title("Diagnostics")
                .Content("Couldn't set the culture. Make sure this is a valid culture.")
                .OkCommand()
            );
        }
    }
}
