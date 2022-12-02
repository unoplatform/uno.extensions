﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.ViewModels.Common;
using TemplateTesteWPF.ViewModels;

namespace Microsoft.Templates.UI.ViewModels.NewProject
{
    public class FrameworkViewModel : SelectableGroup<FrameworkMetaDataViewModel>
    {
        public FrameworkViewModel(Func<bool> isSelectionEnabled, Func<Task> onSelected)
            : base(isSelectionEnabled, onSelected)
        {
        }

        public async Task LoadDataAsync(UserSelectionContext context)
        {
			//if (DataService.LoadFrameworks(Items, context))
			//{
			//    await BaseMainViewModel.BaseInstance.ProcessItemAsync(Items.First());
			//}
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
			this.Items.Add(new FrameworkMetaDataViewModel(metadataInfo));
			await BaseMainViewModel.BaseInstance.ProcessItemAsync(Items.First());
		}
    }
}
