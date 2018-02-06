/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using static SonarLint.VisualStudio.Integration.Vsix.SonarAnalyzerWorkflowBase;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerWorkflowBaseTests
    {
        private class TestableSonarAnalyzerWorkflow : SonarAnalyzerWorkflowBase
        {
            public TestableSonarAnalyzerWorkflow(Workspace workspace)
                : base(workspace)
            {
            }
        }

        [TestMethod]
        public void Ctor_WhenWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new TestableSonarAnalyzerWorkflow(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenReferencesIsNull_ReturnsNoAnalyzer()
        {
            // Arrange & Act
            var result = ProcessAnalyzerReferences(null);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.NoAnalyzer);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNoReferencesMatchName_ReturnsNoAnalyzer()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(null, "foo1") };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.NoAnalyzer);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNameMatchesAndDifferentVersions_ReturnsDifferentVersion()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(new AssemblyIdentity(AnalyzerName, new Version("0.1.2.3")),
                AnalyzerName) };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.DifferentVersion);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNameMatchesAndSameVersions_ReturnsSameVersion()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(new AssemblyIdentity(AnalyzerName, AnalyzerVersion),
                AnalyzerName) };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.SameVersion);
        }
    }
}
