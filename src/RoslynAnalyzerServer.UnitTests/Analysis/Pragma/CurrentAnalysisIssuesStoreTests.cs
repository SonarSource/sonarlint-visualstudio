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

using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Pragma;

[TestClass]
public class CurrentAnalysisIssuesStoreTests
{
    private CurrentAnalysisIssuesStore testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new CurrentAnalysisIssuesStore();
    }

    [TestMethod]
    public void GetAll_NoIssuesAdded_ReturnsEmptyArray()
    {
        var result = testSubject.GetAll();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Add_SuppressedDiagnostic_StoresDiagnostic()
    {
        var diagnostic = CreateSuppressedDiagnostic();

        testSubject.Add(diagnostic);

        testSubject.GetAll().Should().ContainSingle().Which.Should().Be(diagnostic);
    }

    [TestMethod]
    public void Add_NonSuppressedDiagnostic_DoesNotStore()
    {
        var diagnostic = CreateNonSuppressedDiagnostic();

        testSubject.Add(diagnostic);

        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Add_MultipleSuppressedDiagnostics_StoresAll()
    {
        var diagnostic1 = CreateSuppressedDiagnostic();
        var diagnostic2 = CreateSuppressedDiagnostic();
        var diagnostic3 = CreateSuppressedDiagnostic();

        testSubject.Add(diagnostic1);
        testSubject.Add(diagnostic2);
        testSubject.Add(diagnostic3);

        testSubject.GetAll().Should().BeEquivalentTo([diagnostic1, diagnostic2, diagnostic3]);
    }

    [TestMethod]
    public void Add_MixOfSuppressedAndNonSuppressed_StoresOnlySuppressed()
    {
        var suppressed1 = CreateSuppressedDiagnostic();
        var nonSuppressed1 = CreateNonSuppressedDiagnostic();
        var suppressed2 = CreateSuppressedDiagnostic();
        var nonSuppressed2 = CreateNonSuppressedDiagnostic();

        testSubject.Add(suppressed1);
        testSubject.Add(nonSuppressed1);
        testSubject.Add(suppressed2);
        testSubject.Add(nonSuppressed2);

        testSubject.GetAll().Should().BeEquivalentTo([suppressed1, suppressed2]);
    }

    private static Diagnostic CreateSuppressedDiagnostic() =>
        Diagnostic.Create("S001", "category", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, isSuppressed: true);

    private static Diagnostic CreateNonSuppressedDiagnostic() =>
        Diagnostic.Create("S001", "category", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, isSuppressed: false);
}
