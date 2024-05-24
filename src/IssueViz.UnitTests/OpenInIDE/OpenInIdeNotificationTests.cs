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

using System;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenInIdeNotificationTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeNotification, IOpenInIdeNotification>(
            MefTestHelpers.CreateExport<IOpenInIdeFailureInfoBar>(),
            MefTestHelpers.CreateExport<IToolWindowService>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenInIdeNotification>();
    }

    [TestMethod]
    public void UnableToLocateIssue_ShowsMessageBox()
    {
        var filePath = "file/path/123";
        var toolWindowId = Guid.NewGuid();
        var infoBar = Substitute.For<IOpenInIdeFailureInfoBar>();
        var toolWindowService = Substitute.For<IToolWindowService>();
        
        new OpenInIdeNotification(infoBar, toolWindowService).UnableToLocateIssue(filePath, toolWindowId);
        
        VerifyToolWindowOpened(toolWindowService, toolWindowId);
        VerifyInfoBarShown(infoBar, "Open in IDE. Could not locate issue. Ensure the file (file/path/123) has not been modified", toolWindowId);
    }
    
    [TestMethod]
    public void UnableToOpenFile_ShowsMessageBox()
    {
        var filePath = "file/path/123";
        var toolWindowId = Guid.NewGuid();
        var infoBar = Substitute.For<IOpenInIdeFailureInfoBar>();
        var toolWindowService = Substitute.For<IToolWindowService>();
        
        new OpenInIdeNotification(infoBar, toolWindowService).UnableToOpenFile(filePath, toolWindowId);
        
        VerifyToolWindowOpened(toolWindowService, toolWindowId);
        VerifyInfoBarShown(infoBar, "Open in IDE. Could not open File: file/path/123", toolWindowId);
    }
    
    [TestMethod]
    public void InvalidConfiguration_ShowsMessageBox()
    {
        var reason = "reason 123";
        var infoBar = Substitute.For<IOpenInIdeFailureInfoBar>();
        var toolWindowId = Guid.NewGuid();
        var toolWindowService = Substitute.For<IToolWindowService>();
        
        new OpenInIdeNotification(infoBar, toolWindowService).InvalidRequest(reason, toolWindowId);
        
        VerifyToolWindowOpened(toolWindowService, toolWindowId);
        VerifyInfoBarShown(infoBar, $"Unable to process Open in IDE request. Reason: reason 123", toolWindowId);
    }

    private void VerifyToolWindowOpened(IToolWindowService toolWindowService, Guid toolWindowId) =>
        toolWindowService.Show(toolWindowId);
    
    private void VerifyInfoBarShown(IOpenInIdeFailureInfoBar infoBar, string message, Guid toolWindowId) =>
        infoBar.Received(1).ShowAsync(toolWindowId, message);
}
