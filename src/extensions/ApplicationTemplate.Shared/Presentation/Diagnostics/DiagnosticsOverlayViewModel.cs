using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Input;
using ApplicationTemplate.Business;
using ApplicationTemplate.Views;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
//using Chinook.DynamicMvvm;
//using Chinook.SectionsNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class DiagnosticsOverlayViewModel : ViewModel
    {
        private IRouteMessenger Messenger { get; }
        private IWritableOptions<DiagnosticSettings> Settings { get; }
        public DiagnosticsOverlayViewModel(
            DiagnosticsCountersService counterService,
            IWritableOptions<DiagnosticSettings> settings,
            IRouteMessenger messenger)
        {
            DiagnosticsCountersService = counterService;
            Settings = settings;
                Messenger = messenger;
        }

        private DiagnosticsCountersService DiagnosticsCountersService { get; }// => this.GetService<DiagnosticsCountersService>();

        //public IDynamicCommand CollectMemory => this.GetCommand(() =>
        //{
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        //    GC.WaitForPendingFinalizers();
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        //});
        public ICommand CollectMemory => new RelayCommand(() =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        });

        //public IDynamicCommand NavigateToDiagnosticsPage => this.GetCommandFromTask(async ct =>
        //{
        //    await this.GetService<ISectionsNavigator>().OpenModal(ct, () => new DiagnosticsPageViewModel());
        //});
        public ICommand NavigateToDiagnosticsPage => new RelayCommand(() =>
           Messenger.Send(new RoutingMessage(this, typeof(DiagnosticsPageViewModel).AsRoute())));

        public bool IsDiagnosticsOverlayEnabled => Settings.Value.DiagnosticOverlayEnabled;
        //public bool IsDiagnosticsOverlayEnabled => DiagnosticsConfiguration.DiagnosticsOverlay.GetIsEnabled();

        private bool isAlignmentGridEnabled;
        public bool IsAlignmentGridEnabled
        {
            get => isAlignmentGridEnabled;
            set => SetProperty(ref isAlignmentGridEnabled, value);
        }
        //public bool IsAlignmentGridEnabled
        //{
        //    get => this.Get<bool>();
        //    set => this.Set(value);
        //}

        //public IDynamicCommand ToggleAlignmentGrid => this.GetCommand(() =>
        //{
        //    IsAlignmentGridEnabled = !IsAlignmentGridEnabled;
        //});
        public ICommand ToggleAlignmentGrid => new RelayCommand(() =>
        {
            IsAlignmentGridEnabled = !IsAlignmentGridEnabled;
        });

        //public bool IsDiagnosticsExpanded
        private bool isDiagnosticsExpanded;
        public bool IsDiagnosticsExpanded
        {
            get => isDiagnosticsExpanded;
            set => SetProperty(ref isDiagnosticsExpanded, value);
        }

        //public IDynamicCommand ToggleMore => this.GetCommand(() =>
        //{
        //    IsDiagnosticsExpanded = !IsDiagnosticsExpanded;
        //});
        public ICommand ToggleMore => new RelayCommand(() =>
        {
            IsDiagnosticsExpanded = !IsDiagnosticsExpanded;
        });


        //public CountersData Counters => this.GetFromObservable(ObserveCounters, DiagnosticsCountersService.Counters);

        //private IObservable<CountersData> ObserveCounters =>
        //    Observable.FromEventPattern<EventHandler, EventArgs>(
        //        h => DiagnosticsCountersService.CountersChanged += h,
        //        h => DiagnosticsCountersService.CountersChanged -= h
        //    )
        //    .Select(_ => DiagnosticsCountersService.Counters);
    }
}
