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

using NSubstitute;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Visualization;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ShowInIdeListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ShowInIdeListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IOpenIssueInIdeHandler>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ShowInIdeListener>();
    }

    [TestMethod]
    public void ShowIssue_ForwardsToHandler()
    {
        var dummyIssue = new IssueDetailDto(default, default, default, default,
            default, default, default, default,
            default, default, default);
        const string configScopeId = "configscope";
        var openIssueInIdeHandler = Substitute.For<IOpenIssueInIdeHandler>();
        var testSubject = new ShowInIdeListener(openIssueInIdeHandler);
        
        testSubject.ShowIssue(new ShowIssueParams(configScopeId, dummyIssue));
        
        openIssueInIdeHandler.Received().ShowIssue(dummyIssue, configScopeId);
    }
}
