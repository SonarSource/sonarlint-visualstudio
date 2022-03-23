/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
        [DataRow(@"C:\project\.sonarlint\projectKey1\CSharp\SonarLint.xml", "projectKey1")]
        [DataRow(@"C:\project\.sonarlint\projectKey2\VB\SonarLint.xml", "projectKey2")]
        [DataRow(@"C:\project\.sonarlint\projectKey3\VB\sonarlint.xml", "projectKey3")]
        [DataRow(@"C:\project\.sonarlint\projectKey4\VB\SONARLINT.xml", "projectKey4")]
        public void SonarProjectKey_PathIsValid_ReturnsKey(string path, string projectKey)
        {
            var additionalText = new ConcreteAdditionalText(path);

            var testSubject = CreateTestSubject(additionalText);

            testSubject.SettingsKey.Should().Be(projectKey);
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
        public void SonarProjectKey_HasMixedPaths_ReturnsKey()
        {
            var additionalText1 = new ConcreteAdditionalText(@"C:\project\projectKey1\CSharp\SonarLint.xml");
            var additionalText2 = new ConcreteAdditionalText(@"C:\project\.sonarlint\projectKey2\CSharp\SonarLint.xml");            

            var testSubject = CreateTestSubject(additionalText1, additionalText2);

            testSubject.SettingsKey.Should().Be("projectKey2");
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
