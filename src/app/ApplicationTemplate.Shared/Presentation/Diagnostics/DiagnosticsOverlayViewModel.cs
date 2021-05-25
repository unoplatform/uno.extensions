using System;
using System.Collections.Generic;
using System.Reactive.Linq;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class DiagnosticsOverlayViewModel : ViewModel
    {
        //private DiagnosticsCountersService DiagnosticsCountersService => this.GetService<DiagnosticsCountersService>();

        //public IDynamicCommand CollectMemory => this.GetCommand(() =>
        //{
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        //    GC.WaitForPendingFinalizers();
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        //});

        //public IDynamicCommand NavigateToDiagnosticsPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().OpenModal(ct, () => new DiagnosticsPageViewModel());
        //});

        public bool IsDiagnosticsOverlayEnabled => true;
        //public bool IsDiagnosticsOverlayEnabled => DiagnosticsConfiguration.DiagnosticsOverlay.GetIsEnabled();
        public bool IsAlignmentGridEnabled => true;
        //public bool IsAlignmentGridEnabled
        //{
        //    get => this.Get<bool>();
        //    set => this.Set(value);
        //}

        //public IDynamicCommand ToggleAlignmentGrid => this.GetCommand(() =>
        //{
        //    IsAlignmentGridEnabled = !IsAlignmentGridEnabled;
        //});

        //public bool IsDiagnosticsExpanded
        //{
        //    get => this.Get<bool>();
        //    set => this.Set(value);
        //}

        //public IDynamicCommand ToggleMore => this.GetCommand(() =>
        //{
        //    IsDiagnosticsExpanded = !IsDiagnosticsExpanded;
        //});

        //public CountersData Counters => this.GetFromObservable(ObserveCounters, DiagnosticsCountersService.Counters);

        //private IObservable<CountersData> ObserveCounters =>
        //    Observable.FromEventPattern<EventHandler, EventArgs>(
        //        h => DiagnosticsCountersService.CountersChanged += h,
        //        h => DiagnosticsCountersService.CountersChanged -= h
        //    )
        //    .Select(_ => DiagnosticsCountersService.Counters);
    }
}
