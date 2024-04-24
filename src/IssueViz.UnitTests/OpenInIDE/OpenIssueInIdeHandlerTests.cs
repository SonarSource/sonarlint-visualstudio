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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenIssueInIdeHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenIssueInIdeHandler, IOpenIssueInIdeHandler>(
            MefTestHelpers.CreateExport<IOpenInIdeHandler>(),
            MefTestHelpers.CreateExport<IIssueOpenInIdeConverter>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenIssueInIdeHandler>();
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Show_CallsBaseHandler(bool isTaint)
    {
        const string configScope = "configscope";
        var issue = new IssueDetailDto(default, default, default, default, default, default,
            default, default, isTaint, default, default);
        var testSubject = CreateTestSubject(out var handler, out var converter);
        
        testSubject.Show(issue, configScope);
        
        handler.Received().ShowIssue(issue, configScope, converter, isTaint ? IssueListIds.TaintId : IssueListIds.ErrorListId, null);
    }

    private OpenIssueInIdeHandler CreateTestSubject(out IOpenInIdeHandler openInIdeHandler,
        out IIssueOpenInIdeConverter issueOpenInIdeConverter)
    {
        openInIdeHandler = Substitute.For<IOpenInIdeHandler>();
        issueOpenInIdeConverter = Substitute.For<IIssueOpenInIdeConverter>();
        return new OpenIssueInIdeHandler(openInIdeHandler,
            issueOpenInIdeConverter);
    } 
}
