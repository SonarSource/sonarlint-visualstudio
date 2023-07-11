/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Collections.Immutable;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SuppressionExecutionContextTests
    {
        [TestMethod]
        [DataRow(@"C:\project\SonarLint for Visual Studio\Bindings\solutionName1\CSharp\SonarLint.xml", "solutionName1")]
        [DataRow(@"C:\project\SonarLint for Visual Studio\Bindings\solutionName2\VB\SonarLint.xml", "solutionName2")]
        [DataRow(@"C:\project\SonarLint for Visual Studio\Bindings\solutionName3\VB\sonarlint.xml", "solutionName3")]
        [DataRow(@"C:\project\SonarLint for Visual Studio\Bindings\solutionName4\VB\SONARLINT.xml", "solutionName4")]
        public void SonarProjectKey_PathIsValid_ReturnsExpectedSolutionName(string path, string solutionName)
        {
            var additionalText = new ConcreteAdditionalText(path);

            var testSubject = CreateTestSubject(additionalText);

            testSubject.SettingsKey.Should().Be(solutionName);
            testSubject.IsInConnectedMode.Should().BeTrue();
            testSubject.Mode.Should().Be("Connected");
        }

        [TestMethod]
        public void SonarProjectKey_PathIsNotValid_ReturnsNull()
        {
            var additionalText = new ConcreteAdditionalText(@"C:\project\projectKey1\CSharp\SonarLint.xml");

            var testSubject = CreateTestSubject(additionalText);

            testSubject.SettingsKey.Should().BeNull();
            testSubject.IsInConnectedMode.Should().BeFalse();
            testSubject.Mode.Should().Be("Standalone");
        }

        [TestMethod]
        public void SonarProjectKey_HasMixedPaths_ReturnsExpectedSolutionName()
        {
            var additionalText1 = new ConcreteAdditionalText(@"C:\project\projectKey1\CSharp\SonarLint.xml");
            var additionalText2 = new ConcreteAdditionalText(@"C:\project\SonarLint for Visual Studio\Bindings\expectedSlnName\CSharp\SonarLint.xml");            

            var testSubject = CreateTestSubject(additionalText1, additionalText2);

            testSubject.SettingsKey.Should().Be("expectedSlnName");
            testSubject.IsInConnectedMode.Should().BeTrue();
            testSubject.Mode.Should().Be("Connected");
        }

        [TestMethod]
        public void SonarProjectKey_HasNoPaths_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            testSubject.SettingsKey.Should().BeNull();
            testSubject.IsInConnectedMode.Should().BeFalse();
            testSubject.Mode.Should().Be("Standalone");
        }

        private SuppressionExecutionContext CreateTestSubject(params AdditionalText[] existingAdditionalFiles)
        {
            var analyzerOptions = CreateAnalyzerOptions(existingAdditionalFiles);
            return new SuppressionExecutionContext(analyzerOptions);
        }

        private AnalyzerOptions CreateAnalyzerOptions(params AdditionalText[] existingAdditionalFiles)
        {
            return new AnalyzerOptions(existingAdditionalFiles.ToImmutableArray());
        }

    }

    internal class ConcreteAdditionalText : AdditionalText
    {
        internal ConcreteAdditionalText(string path)
        {
            Path = path;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(string.Empty);
        }
    }
}
