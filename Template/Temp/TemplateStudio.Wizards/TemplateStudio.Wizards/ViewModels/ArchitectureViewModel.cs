// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.ViewModels.Common;
using Microsoft.Templates.UI.ViewModels.NewProject;

namespace TemplateStudio.Wizards.ViewModels
{
    public class ArchitectureViewModel : SelectableGroup<ArchitectureMetaDataViewModel>
    {
        public ArchitectureViewModel(Func<bool> isSelectionEnabled, Func<Task> onSelected)
            : base(isSelectionEnabled, onSelected)
        {
			
        }

        public async Task LoadDataAsync(MainViewModel MainViewModel)
        {
			MetadataInfo metadataInfo = new MetadataInfo()
			{
				Name = "metadataInfo.Name",
				DisplayName = "metadataInfo.DisplayName",
				Summary = "metadataInfo.Summary",
				Description = "metadataInfo.Description",
				Author = "metadataInfo.Author",
				Icon = "metadataInfo.Icon",
				Order = 0,
				MetadataType = 0,
				Licenses = null,

			};
			this.Items.Add(new ArchitectureMetaDataViewModel(metadataInfo));
			await BaseMainViewModel.BaseInstance.ProcessItemAsync(Items.First());
		}
    }
}
