/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

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
