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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis;

[TestClass]
public class AdditionalAnalysisIssueStorageTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AdditionalAnalysisIssueStorage, IAdditionalAnalysisIssueStorage>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<AdditionalAnalysisIssueStorage>();

    [TestMethod]
    public void Get_KeyDoesNotExist_ReturnsEmptyList()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.Get("nonexistent");

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Store_NewKey_ThenGet_ReturnsStoredIssues()
    {
        var testSubject = CreateTestSubject();
        var issues = new List<IAnalysisIssue> { Substitute.For<IAnalysisIssue>(), Substitute.For<IAnalysisIssue>() };

        testSubject.Store("file.cs", issues);

        testSubject.Get("file.cs").Should().BeEquivalentTo(issues);
    }

    [TestMethod]
    public void Store_ExistingKey_OverwritesPreviousIssues()
    {
        var testSubject = CreateTestSubject();
        var originalIssues = new List<IAnalysisIssue> { Substitute.For<IAnalysisIssue>() };
        var newIssues = new List<IAnalysisIssue> { Substitute.For<IAnalysisIssue>(), Substitute.For<IAnalysisIssue>() };

        testSubject.Store("file.cs", originalIssues);
        testSubject.Store("file.cs", newIssues);

        testSubject.Get("file.cs").Should().BeEquivalentTo(newIssues);
    }

    [TestMethod]
    public void Remove_KeyExists_GetReturnsEmpty()
    {
        var testSubject = CreateTestSubject();
        testSubject.Store("file.cs", new List<IAnalysisIssue> { Substitute.For<IAnalysisIssue>() });

        testSubject.Remove("file.cs");

        testSubject.Get("file.cs").Should().BeEmpty();
    }

    [TestMethod]
    public void Remove_KeyDoesNotExist_DoesNotThrow()
    {
        var testSubject = CreateTestSubject();

        var act = () => testSubject.Remove("nonexistent");

        act.Should().NotThrow();
    }

    private AdditionalAnalysisIssueStorage CreateTestSubject() => new();
}
