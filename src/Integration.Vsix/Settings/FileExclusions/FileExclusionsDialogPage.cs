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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

[ExcludeFromCodeCoverage]
internal class FileExclusionsDialogPage : UIElementDialogPage
{
    public const string PageName = "File Exclusions";
    private FileExclusionsDialogControl dialogControl;
    private FileExclusionsViewModel viewModel;

    protected override UIElement Child => dialogControl ??= new FileExclusionsDialogControl(ViewModel);

    private FileExclusionsViewModel ViewModel
    {
        get
        {
            if (viewModel == null)
            {
                var browserService = Site.GetMefService<IBrowserService>();
                var userSettingsProvider = Site.GetMefService<IUserSettingsProvider>();
                viewModel = new FileExclusionsViewModel(browserService, userSettingsProvider);
            }
            return viewModel;
        }
    }

    protected override void OnActivate(CancelEventArgs e)
    {
        ViewModel.InitializeExclusions();
        base.OnActivate(e);
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
        if (e.ApplyBehavior == ApplyKind.Apply)
        {
            ViewModel.SaveExclusions();
        }
        base.OnApply(e);
    }
}
