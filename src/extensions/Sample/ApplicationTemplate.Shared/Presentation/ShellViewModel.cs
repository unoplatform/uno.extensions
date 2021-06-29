using System;
//using Chinook.DynamicMvvm;

namespace ApplicationTemplate.Presentation
{
    public class ShellViewModel : ViewModel
    {
        public ViewModel DiagnosticsOverlay { get; }// => this.GetChild<DiagnosticsOverlayViewModel>();

        public ViewModel Menu { get; }// => this.GetChild<MenuViewModel>();

        public ShellViewModel(MenuViewModel menu, DiagnosticsOverlayViewModel diagnostic)
        {
            Menu = menu;
            DiagnosticsOverlay = diagnostic;
        }
    }
}
