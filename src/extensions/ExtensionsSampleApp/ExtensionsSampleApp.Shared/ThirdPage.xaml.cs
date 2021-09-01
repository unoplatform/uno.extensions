using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Navigation;

namespace ExtensionsSampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThirdPage
    {
        public ThirdPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if(e.Parameter is IDictionary<string,object> argsDict )
            {
                ParametersText.Text = string.Join(Environment.NewLine, (from p in argsDict
                                                                        select $"key '{p.Key}' val '{p.Value}'"));
            }
            else if(e.Parameter != null)
            {
                ParametersText.Text = e.Parameter.ToString();
            }
        }
    }
}
