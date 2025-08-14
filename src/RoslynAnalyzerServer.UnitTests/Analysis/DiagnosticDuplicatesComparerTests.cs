/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class DiagnosticDuplicatesComparerTests
{
    private readonly DiagnosticDuplicatesComparer testSubject = DiagnosticDuplicatesComparer.Instance;
    private readonly RoslynIssue diagnostic1 = CreateDiagnostic("rule1", "file1.cs", 1, 1, 1, 10);

    [TestMethod]
    public void Equals_SameReference_ReturnsTrue()
    {
        var result = testSubject.Equals(diagnostic1, diagnostic1);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void Equals_FirstArgumentNull_ReturnsFalse()
    {
        var result = testSubject.Equals(null, diagnostic1);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_SecondArgumentNull_ReturnsFalse()
    {
        var result = testSubject.Equals(diagnostic1, null);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_SameRuleKeyAndLocation_ReturnsTrue()
    {
        var diagnostic2 = CreateDiagnostic("rule1", "file1.cs", 1, 1, 1, 10);

        var result = testSubject.Equals(diagnostic1, diagnostic2);

        result.Should().BeTrue();
    }


    [TestMethod]
    public void Equals_SameRuleKeyAndLocation_MessageIsIgnored()
    {
        var diagnostic2 = CreateDiagnostic("rule1", "file1.cs", 1, 1, 1, 10, "some different message");

        var result = testSubject.Equals(diagnostic1, diagnostic2);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("rule2", "file1.cs", 1, 1, 1, 10, DisplayName = "Different RuleKey")]
    [DataRow("rule1", "file2.cs", 1, 1, 1, 10, DisplayName = "Different FilePath")]
    [DataRow("rule1", "file1.cs", 2, 1, 1, 10, DisplayName = "Different StartLine")]
    [DataRow("rule1", "file1.cs", 1, 1, 2, 10, DisplayName = "Different EndLine")]
    [DataRow("rule1", "file1.cs", 1, 2, 1, 10, DisplayName = "Different StartLineOffset")]
    [DataRow("rule1", "file1.cs", 1, 1, 1, 11, DisplayName = "Different EndLineOffset")]
    public void Equals_DifferentValues_ReturnsFalse(string ruleKey, string filePath, int startLine, int startLineOffset, int endLine, int endLineOffset)
    {
        var diagnostic2 = CreateDiagnostic(ruleKey, filePath, startLine, startLineOffset, endLine, endLineOffset);

        var result = testSubject.Equals(diagnostic1, diagnostic2);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void GetHashCode_SameObjects_ReturnsSameHashCode()
    {
        var diagnostic2 = CreateDiagnostic("rule1", "file1.cs", 1, 1, 1, 10);

        var hash1 = testSubject.GetHashCode(diagnostic1);
        var hash2 = testSubject.GetHashCode(diagnostic2);

        hash1.Should().Be(hash2);
    }

    [TestMethod]
    public void GetHashCode_DifferentObjects_ReturnsDifferentHashCodes()
    {
        var diagnostic2 = CreateDiagnostic("rule2", "file2.cs", 2, 2, 2, 20);

        var hash1 = testSubject.GetHashCode(diagnostic1);
        var hash2 = testSubject.GetHashCode(diagnostic2);

        hash1.Should().NotBe(hash2);
    }

    [TestMethod]
    public void Instance_ReturnsSingletonInstance()
    {
        var instance1 = DiagnosticDuplicatesComparer.Instance;
        var instance2 = DiagnosticDuplicatesComparer.Instance;

        instance1.Should().BeSameAs(instance2);
    }

    private static RoslynIssue CreateDiagnostic(string ruleKey, string filePath, int startLine, int startLineOffset, int endLine, int endLineOffset, string? message = null)
    {
        var textRange = new SonarTextRange(startLine, endLine, startLineOffset, endLineOffset);
        var location = new SonarDiagnosticLocation(message ?? "message", filePath, textRange);
        return new RoslynIssue(ruleKey, location);
    }
}
