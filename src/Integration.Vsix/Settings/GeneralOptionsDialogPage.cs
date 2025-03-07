/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [ExcludeFromCodeCoverage]
    internal class GeneralOptionsDialogPage : UIElementDialogPage
    {
        public const string PageName = "General";

        private GeneralOptionsDialogControl dialogControl;
        private ISonarLintSettings settings;
        private GeneralOptionsDialogControlViewModel viewModel;

        protected override UIElement Child => dialogControl ??= new GeneralOptionsDialogControl(ViewModel);

        private GeneralOptionsDialogControlViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                {
                    Debug.Assert(Site != null, "Expecting the page to be sited");
                    var browserService = Site.GetMefService<IBrowserService>();
                    var dogfoodService = Site.GetMefService<IDogfoodingService>();
                    viewModel = new GeneralOptionsDialogControlViewModel(Settings, browserService, dogfoodService, GetOpenSettingsFileWpfCommand());
                }
                return viewModel;
            }
        }

        private ISonarLintSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    Debug.Assert(Site != null, "Expecting the page to be sited");
                    settings = Site.GetMefService<ISonarLintSettings>();
                }

                return settings;
            }
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                ViewModel.SaveSettings();
            }

            base.OnApply(e);
        }

        private ICommand GetOpenSettingsFileWpfCommand()
        {
            var userSettingsProvider = Site.GetMefService<IUserSettingsProvider>();
            var logger = Site.GetMefService<ILogger>();
            return new OpenSettingsFileWpfCommand(Site, userSettingsProvider, this, logger);
        }
    }
}
