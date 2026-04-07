/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class AdditionalAnalysisIssueStorageTests
{
    private IDiagnosticToAnalysisIssueConverter diagnosticToAnalysisIssueConverter = null!;
    private AdditionalAnalysisIssueStorage testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        diagnosticToAnalysisIssueConverter = Substitute.For<IDiagnosticToAnalysisIssueConverter>();
        testSubject = new AdditionalAnalysisIssueStorage(diagnosticToAnalysisIssueConverter);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        CheckTypeCanBeImported<IAdditionalAnalysisIssueStorage>();

    [TestMethod]
    public void MefCtor_CheckIsExported_Writer() =>
        CheckTypeCanBeImported<IAdditionalAnalysisIssueStorageWriter>();

    private static void CheckTypeCanBeImported<T>() where T : class =>
        MefTestHelpers.CheckTypeCanBeImported<AdditionalAnalysisIssueStorage, T>(
            MefTestHelpers.CreateExport<IDiagnosticToAnalysisIssueConverter>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<AdditionalAnalysisIssueStorage>();

    [TestMethod]
    public void Get_KeyDoesNotExist_ReturnsEmptyList()
    {
        var result = testSubject.Get("nonexistent");

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Add_SingleIssue_ConvertsAndStores()
    {
        var (roslynIssue, analysisIssue) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");

        testSubject.Add([roslynIssue]);

        testSubject.Get(@"C:\file.cs").Should().ContainSingle().Which.Should().Be(analysisIssue);
    }

    [TestMethod]
    public void Add_MultipleIssuesSameFile_ReturnsAll()
    {
        var (roslynIssue1, analysisIssue1) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");
        var (roslynIssue2, analysisIssue2) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");

        testSubject.Add([roslynIssue1, roslynIssue2]);

        testSubject.Get(@"C:\file.cs").Should().BeEquivalentTo([analysisIssue1, analysisIssue2]);
    }

    [TestMethod]
    public void Add_IssuesForDifferentFiles_GroupedCorrectly()
    {
        var (roslynIssue1, analysisIssue1) = SetUpRoslynIssueAndConvertedModel(@"C:\file1.cs");
        var (roslynIssue2, analysisIssue2) = SetUpRoslynIssueAndConvertedModel(@"C:\file2.cs");
        var (roslynIssue3, analysisIssue3) = SetUpRoslynIssueAndConvertedModel(@"C:\file1.cs");

        testSubject.Add([roslynIssue1, roslynIssue2, roslynIssue3]);

        testSubject.Get(@"C:\file1.cs").Should().BeEquivalentTo([analysisIssue1, analysisIssue3]);
        testSubject.Get(@"C:\file2.cs").Should().ContainSingle().Which.Should().Be(analysisIssue2);
    }

    [TestMethod]
    public void Add_CalledTwice_Accumulates()
    {
        var (roslynIssue1, analysisIssue1) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");
        var (roslynIssue2, analysisIssue2) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");

        testSubject.Add([roslynIssue1]);
        testSubject.Add([roslynIssue2]);

        testSubject.Get(@"C:\file.cs").Should().BeEquivalentTo([analysisIssue1, analysisIssue2]);
    }

    [TestMethod]
    public void Add_ConvertsViaConverter()
    {
        var (roslynIssue, _) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");

        testSubject.Add([roslynIssue]);

        diagnosticToAnalysisIssueConverter.Received(1).Convert(roslynIssue);
    }

    [TestMethod]
    public void Remove_KeyExists_GetReturnsEmpty()
    {
        var (roslynIssue, _) = SetUpRoslynIssueAndConvertedModel(@"C:\file.cs");
        testSubject.Add([roslynIssue]);

        testSubject.Remove(@"C:\file.cs");

        testSubject.Get(@"C:\file.cs").Should().BeEmpty();
    }

    [TestMethod]
    public void Remove_KeyDoesNotExist_DoesNotThrow()
    {
        var act = () => testSubject.Remove("nonexistent");

        act.Should().NotThrow();
    }

    private (RoslynIssue roslynIssue, IAnalysisIssue analysisIssue) SetUpRoslynIssueAndConvertedModel(string filePath)
    {
        var roslynIssue = CreateRoslynIssue(filePath);
        var analysisIssue = Substitute.For<IAnalysisIssue>();
        var location = Substitute.For<IAnalysisIssueLocation>();
        location.FilePath.Returns(filePath);
        analysisIssue.PrimaryLocation.Returns(location);
        diagnosticToAnalysisIssueConverter.Convert(roslynIssue).Returns(analysisIssue);
        return (roslynIssue, analysisIssue);
    }

    private static RoslynIssue CreateRoslynIssue(string filePath)
    {
        var textRange = new RoslynIssueTextRange(1, 1, 0, 1);
        var location = new RoslynIssueLocation("message", new FileUri(filePath), textRange);
        return new RoslynIssue(Guid.NewGuid().ToString(), location);
    }
}
