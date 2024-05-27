/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

internal interface IOpenInIdeMessageBox
{
    void UnableToLocateIssue(string filePath);
    void UnableToOpenFile(string filePath);
    void InvalidRequest(string reason);
}

[Export(typeof(IOpenInIdeMessageBox))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeMessageBox : IOpenInIdeMessageBox
{
    private readonly IMessageBox messageBox;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public OpenInIdeMessageBox(IMessageBox messageBox, IThreadHandling threadHandling)
    {
        this.messageBox = messageBox;
        this.threadHandling = threadHandling;
    }

    public void UnableToLocateIssue(string filePath) => 
        Show(string.Format(OpenInIdeResources.MessageBox_UnableToLocateIssue, filePath));

    public void UnableToOpenFile(string filePath) => 
        Show(string.Format(OpenInIdeResources.MessageBox_UnableToOpenFile, filePath));
    
    public void InvalidRequest(string reason) => 
        Show(string.Format(OpenInIdeResources.MessageBox_InvalidConfiguration, reason));

    private void Show(string message) => threadHandling.RunOnUIThread(() =>
        messageBox.Show(message, OpenInIdeResources.MessageBox_Caption, MessageBoxButton.OK, MessageBoxImage.Warning));
}
