// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Gen;
using TemplateTesteWPF.Helpers;

namespace Microsoft.Templates.UI.ViewModels.Common
{
    public abstract class BaseMainViewModel : Observable
    {
        public static BaseMainViewModel BaseInstance { get; private set; }

        public Window MainView { get; private set; }

        public WizardStatus WizardStatus { get; }

        public WizardNavigation Navigation { get; }

        public abstract Task ProcessItemAsync(object item);

        public abstract Task OnTemplatesAvailableAsync();

    }
}
