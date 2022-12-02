// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Templates.Core;
using TemplateTesteWPF.String;

namespace TemplateTesteWPF.Extensions
{
    public static class TemplateTypeExtensions
    {
        private const string _newProjectStepPages = "08Pages";
        private const string _newProjectStepFeatures = "02Features";
        //private const string _newProjectStepServices = "05Services";
        //private const string _newProjectStepTests = "06Tests";
		private const string _newProjectStepPlatform = "01Platform";
		private const string _newProjectStepUnoExtensions = "03UnoExtensions";
		private const string _newProjectStepCodingStyles = "04CodingStyles";
		private const string _newProjectStepUnoFrameworks = "05UnoFrameworkS";

		public static string GetNewProjectStepId(this TemplateType templateType)
        {
            switch (templateType)
            {
				case TemplateType.Page:
					return _newProjectStepPages;
				case TemplateType.Feature:
                    return _newProjectStepFeatures;
                //case TemplateType.Service:
                //    return _newProjectStepServices;
				//case TemplateType.Testing:
				//	return _newProjectStepTests;
				case TemplateType.Platform:
					return _newProjectStepPlatform;
				case TemplateType.UnoExtensions:
					return _newProjectStepUnoExtensions;
				case TemplateType.CodingStyle:
					return _newProjectStepCodingStyles;
				case TemplateType.UnoFramework:
					return _newProjectStepUnoFrameworks;
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
				case TemplateType.CodingStyle:
					return Resources.NewProjectStepCodingStyle;
				case TemplateType.UnoFramework:
					return Resources.NewProjectStepUnoFramework;
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
				case TemplateType.CodingStyle:
					return Resources.AddCodingStyleTitle;
				case TemplateType.UnoFramework:
					return Resources.AddUnoFrameworkTitle;
				default:
                    return string.Empty;
            }
        }
    }
}
