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
    public void Add_SingleIssue_ThenGet_ReturnsStoredIssue()
    {
        var testSubject = CreateTestSubject();
        var issue = CreateIssue("file.cs");

        testSubject.Add([issue]);

        testSubject.Get("file.cs").Should().ContainSingle().Which.Should().Be(issue);
    }

    [TestMethod]
    public void Add_MultipleIssuesSameFile_ThenGet_ReturnsAll()
    {
        var testSubject = CreateTestSubject();
        var issue1 = CreateIssue("file.cs");
        var issue2 = CreateIssue("file.cs");

        testSubject.Add([issue1, issue2]);

        testSubject.Get("file.cs").Should().BeEquivalentTo([issue1, issue2]);
    }

    [TestMethod]
    public void Add_IssuesForDifferentFiles_GroupedCorrectly()
    {
        var testSubject = CreateTestSubject();
        var issue1 = CreateIssue("file1.cs");
        var issue2 = CreateIssue("file2.cs");
        var issue3 = CreateIssue("file1.cs");

        testSubject.Add([issue1, issue2, issue3]);

        testSubject.Get("file1.cs").Should().BeEquivalentTo([issue1, issue3]);
        testSubject.Get("file2.cs").Should().ContainSingle().Which.Should().Be(issue2);
    }

    [TestMethod]
    public void Add_CalledTwice_Accumulates()
    {
        var testSubject = CreateTestSubject();
        var issue1 = CreateIssue("file.cs");
        var issue2 = CreateIssue("file.cs");

        testSubject.Add([issue1]);
        testSubject.Add([issue2]);

        testSubject.Get("file.cs").Should().BeEquivalentTo([issue1, issue2]);
    }

    [TestMethod]
    public void Remove_KeyExists_GetReturnsEmpty()
    {
        var testSubject = CreateTestSubject();
        var issue = CreateIssue("file.cs");
        testSubject.Add([issue]);

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

    private static AdditionalAnalysisIssueStorage CreateTestSubject() => new();

    private static IAnalysisIssue CreateIssue(string filePath)
    {
        var issue = Substitute.For<IAnalysisIssue>();
        var location = Substitute.For<IAnalysisIssueLocation>();
        location.FilePath.Returns(filePath);
        issue.PrimaryLocation.Returns(location);
        return issue;
    }
}
