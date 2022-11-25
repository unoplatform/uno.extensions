// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Templates.Core;
using Microsoft.Templates.SharedResources;

namespace Microsoft.Templates.UI.Extensions
{
    public static class TemplateTypeExtensions
    {
        private const string _newProjectStepPages = "03Pages";
        private const string _newProjectStepFeatures = "04Features";
        private const string _newProjectStepServices = "05Services";
        private const string _newProjectStepTests = "06Tests";
		private const string _newProjectStepPlatform = "07Platform";
		private const string _newProjectStepUnoExtensions = "08UnoExtensions";

		public static string GetNewProjectStepId(this TemplateType templateType)
        {
            switch (templateType)
            {
                case TemplateType.Page:
                    return _newProjectStepPages;
                case TemplateType.Feature:
                    return _newProjectStepFeatures;
                case TemplateType.Service:
                    return _newProjectStepServices;
				case TemplateType.Testing:
					return _newProjectStepTests;
				case TemplateType.Platform:
					return _newProjectStepPlatform;
				case TemplateType.UnoExtensions:
					return _newProjectStepUnoExtensions;
				default:
                    return string.Empty;
            }
        }

        public static string GetNewProjectStepTitle(this TemplateType templateType)
        {
            switch (templateType)
            {
                case TemplateType.Page:
                    return Resources.NewProjectStepPages;
                case TemplateType.Feature:
                    return Resources.NewProjectStepFeatures;
                case TemplateType.Service:
                    return Resources.NewProjectStepServices;
				case TemplateType.Testing:
					return Resources.NewProjectStepTesting;
				case TemplateType.Platform:
					return Resources.NewProjectStepPlatform;
				case TemplateType.UnoExtensions:
					return Resources.NewProjectStepUnoExtensions;
				case TemplateType.Architecture:
					return Resources.NewProjectStepArchitecture;
				default:
                    return string.Empty;
            }
        }

        public static string GetStepPageTitle(this TemplateType templateType)
        {
            switch (templateType)
            {
                case TemplateType.Page:
                    return Resources.AddPagesTitle;
                case TemplateType.Feature:
                    return Resources.AddFeaturesTitle;
                case TemplateType.Service:
                    return Resources.AddServiceTitle;
                case TemplateType.Testing:
                    return Resources.AddTestingTitle;
				case TemplateType.Platform:
					return Resources.AddPlatformTitle;
				case TemplateType.UnoExtensions:
					return Resources.AddUnoExtensionsTitle;
				default:
                    return string.Empty;
            }
        }
    }
}
