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

using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenInIdeMessageBoxTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeMessageBox, IOpenInIdeMessageBox>(
            MefTestHelpers.CreateExport<IMessageBox>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenInIdeMessageBox>();
    }

    [TestMethod]
    public void UnableToLocateIssue_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        var filePath = "file/path";
        new OpenInIdeMessageBox(messageBox).UnableToLocateIssue(filePath);
        
        VerifyMessageBox(messageBox, string.Format(OpenInIdeResources.MessageBox_UnableToLocateIssue, filePath));
    }
    
    [TestMethod]
    public void UnableToOpenFile_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        var filePath = "file/path";
        new OpenInIdeMessageBox(messageBox).UnableToOpenFile(filePath);
        
        VerifyMessageBox(messageBox, string.Format(OpenInIdeResources.MessageBox_UnableToOpenFile, filePath));
    }
    
    [TestMethod]
    public void InvalidConfiguration_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        var reason = "reason";
        new OpenInIdeMessageBox(messageBox).InvalidConfiguration(reason);
        
        VerifyMessageBox(messageBox, string.Format(OpenInIdeResources.MessageBox_InvalidConfiguration, reason));
    }
    
    [TestMethod]
    public void UnableToConvertIssue_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        new OpenInIdeMessageBox(messageBox).UnableToConvertIssue();
        
        VerifyMessageBox(messageBox, string.Format(OpenInIdeResources.MessageBox_UnableToConvertIssue));
    }

    private void VerifyMessageBox(IMessageBox messageBox, string message) =>
        messageBox.Received(1).Show(message, OpenInIdeResources.MessageBox_Caption, MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
