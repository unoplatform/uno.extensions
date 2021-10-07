using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;
public class DialogNavigationService : CompositeNavigationService
{
    public IRegion Region { get; set; }

    public DialogNavigationService(ILogger<RegionNavigationService> logger) : base(logger)
    {
    }

    public override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        return Region.NavigateAsync(request);
    }

}
