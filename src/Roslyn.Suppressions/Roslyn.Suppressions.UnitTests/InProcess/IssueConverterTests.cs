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

using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess
{
    [TestClass]
    public class IssueConverterTests
    {
        [TestMethod]
        public void Convert_SimplePropertiesAreHandledCorrectly()
        {
            var sonarIssue = CreateSonarQubeIssue(hash: "aaaa", filePath: "\\bbb\\ccc\\file.txt");

            var actual = RoslynSettingsFileSynchronizer.IssueConverter.Convert(sonarIssue);

            actual.Hash.Should().Be("aaaa");
            actual.FilePath.Should().Be("\\bbb\\ccc\\file.txt");
        }

        [TestMethod]
        [DataRow(1, 0)]
        [DataRow(2, 1)]
        [DataRow(null, null)]
        public void Convert_LineNumbersAreHandledCorrectly(int? sonarLineNumber, int? expected)
        {
            // 1-based Sonar line numbers should be converted to 0-based Roslyn line numbers
            var sonarIssue = CreateSonarQubeIssue(line: sonarLineNumber);

            var actual = RoslynSettingsFileSynchronizer.IssueConverter.Convert(sonarIssue);

            if(expected.HasValue)
            {
                actual.RoslynIssueLine.Value.Should().Be(expected);
            }
            else
            {
                actual.RoslynRuleId.Should().BeNull();
            }
        }

        [TestMethod]
        // Recognised repos
        [DataRow("csharpsquid:S123", RoslynLanguage.CSharp, "S123")]
        [DataRow("vbnet:S999", RoslynLanguage.VB, "S999")]

        // Valid but unrecognised repos
        [DataRow("cpp:S111", RoslynLanguage.Unknown, "S111")]
        [DataRow("javascript:S222", RoslynLanguage.Unknown, "S222")]
        [DataRow("CSHARPSQUID:S333", RoslynLanguage.Unknown, "S333")]
        [DataRow("VBNET:S444", RoslynLanguage.Unknown, "S444")]
        
        // Invalid keys - should not error
        [DataRow("invalidcompositekey", RoslynLanguage.Unknown, null)]
        [DataRow("invalid::compositekey", RoslynLanguage.Unknown, ":compositekey")]
        [DataRow(":invalid", RoslynLanguage.Unknown, "invalid")]
        [DataRow("csharpsquid:", RoslynLanguage.CSharp, "")]
        public void Convert_RepoKeysAreHandledCorrectly(string compositeKey, RoslynLanguage expectedLanguage, string expectedRuleKey)
        {
            var sonarIssue = CreateSonarQubeIssue(ruleId: compositeKey);

            var actual = RoslynSettingsFileSynchronizer.IssueConverter.Convert(sonarIssue);

            actual.RoslynLanguage.Should().Be(expectedLanguage);
            actual.RoslynRuleId.Should().Be(expectedRuleKey);
        }
    }
}
