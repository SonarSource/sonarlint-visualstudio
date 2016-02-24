//-----------------------------------------------------------------------
// <copyright file="OptionsPage.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OptionsPage : DialogPage
    {
        public const string CategoryName = "SonarLint for VisualStudio";
        public const string PageName = "Security";

        private IIntegrationSettings settings;

        private IIntegrationSettings Settings
        {
            get
            {
                if (this.settings == null)
                {
                    this.settings = ServiceProvider.GlobalProvider.GetMefService<IIntegrationSettings>();
                    Debug.Assert(this.settings != null, "Failed to get IIntegrationSettings from MEF, no settings will be available!");
                }

                return this.settings;
            }
        }

        public override object AutomationObject
        {
            get
            {
                return this.Settings;
            }
        }
    }
}
