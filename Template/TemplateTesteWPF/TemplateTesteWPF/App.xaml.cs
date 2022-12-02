using Microsoft.Templates.UI.Converters;
using Microsoft.Templates.UI.Styles;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TemplateTesteWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());
            Resources.Add("HasItemsVisibilityConverter", new HasItemsVisibilityConverter());
            Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());
        }
       
    }
}
