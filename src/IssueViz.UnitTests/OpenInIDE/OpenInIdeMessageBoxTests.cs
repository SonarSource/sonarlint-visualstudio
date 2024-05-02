﻿/*
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
        var filePath = "file/path/123";
        new OpenInIdeMessageBox(messageBox).UnableToLocateIssue(filePath);
        
        VerifyMessageBox(messageBox, $"Could not locate issue. Ensure the file (file/path/123) has not been modified");
    }
    
    [TestMethod]
    public void UnableToOpenFile_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        var filePath = "file/path/123";
        new OpenInIdeMessageBox(messageBox).UnableToOpenFile(filePath);
        
        VerifyMessageBox(messageBox, $"Could not open File: file/path/123");
    }
    
    [TestMethod]
    public void InvalidConfiguration_ShowsMessageBox()
    {
        var messageBox = Substitute.For<IMessageBox>();
        var reason = "reason 123";
        new OpenInIdeMessageBox(messageBox).InvalidRequest(reason);
        
        VerifyMessageBox(messageBox, string.Format(OpenInIdeResources.MessageBox_InvalidConfiguration, reason));
        VerifyMessageBox(messageBox, $"Unable to process Open in IDE request. Reason: reason 123");
    }

    private void VerifyMessageBox(IMessageBox messageBox, string message) =>
        messageBox.Received(1).Show(message, OpenInIdeResources.MessageBox_Caption, MessageBoxButton.OK,
            MessageBoxImage.Warning);
}
