/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.SupportedLanguages;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands;

[ExcludeFromCodeCoverage]
internal class SupportedLanguagesCommand : VsCommandBase
{
    internal const int Id = 0x1029;

    private readonly IPluginStatusesStore pluginStatusesStore;
    private readonly IThreadHandling threadHandling;

    public SupportedLanguagesCommand(IPluginStatusesStore pluginStatusesStore, IThreadHandling threadHandling)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.threadHandling = threadHandling;
    }

    protected override void InvokeInternal()
    {
        var supportedLanguagesWindow = new SupportedLanguagesDialogWindow(pluginStatusesStore, threadHandling);
        supportedLanguagesWindow.ShowDialog(Application.Current.MainWindow);
    }
}
