// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Templates.Core;
using Microsoft.Templates.UI.ViewModels.Common;

namespace TemplateStudio.Wizards.ViewModels
{
    public class ArchitectureMetaDataViewModel : BasicInfoViewModel
    {
        private MetadataType _metadataType;

        public MetadataType MetadataType
        {
            get => _metadataType;
            set => SetProperty(ref _metadataType, value);
        }

        public IEnumerable<LayoutViewModel> Layout { get; private set; }

        public ArchitectureMetaDataViewModel(MetadataInfo metadataInfo)
        {
            Name = metadataInfo.Name;
            Identity = metadataInfo.Name;
            Title = metadataInfo.DisplayName;
            Summary = metadataInfo.Summary;
            Description = metadataInfo.Description;
            Author = metadataInfo.Author;
            Icon = metadataInfo.Icon;
            Order = metadataInfo.Order;
            MetadataType = metadataInfo.MetadataType;
            Licenses = metadataInfo.LicenseTerms?.Select(l => new LicenseViewModel(l));
            Deprecated = bool.TryParse(metadataInfo.Tags.FirstOrDefault(t => t.Key.Equals("deprecated", StringComparison.Ordinal)).Value?.ToString(), out bool isDeprecated);
        }
    }
}
