using System;
//using Chinook.DynamicMvvm;

namespace ApplicationTemplate.Presentation
{
    public class ShellViewModel : ViewModel
    {
        //public IViewModel DiagnosticsOverlay => this.GetChild<DiagnosticsOverlayViewModel>();

        public ViewModel Menu { get; }// => this.GetChild<MenuViewModel>();

        public ShellViewModel(MenuViewModel menu)
        {
            Menu = menu;
        }
    }
}
