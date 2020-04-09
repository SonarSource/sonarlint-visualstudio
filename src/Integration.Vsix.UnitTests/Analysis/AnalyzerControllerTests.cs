/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalyzerControllerTests
    {
        [TestMethod]
        public void IsAnalysisSupported()
        {
            // Arrange
            var analyzers = new IAnalyzer[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers, null);

            // Act and Assert
            controller.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily }).Should().BeTrue();
            controller.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript }).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_NotSupported_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new DummyAnalyzer[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers, null);

            // Act
            controller.ExecuteAnalysis("c:\\file.cpp", "charset1", new[] { AnalysisLanguage.Javascript }, null, null, CancellationToken.None);

            analyzers.Any(x => x.RequestAnalysisCalled).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_Supported_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new DummyAnalyzer[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers, null);

            // Act
            controller.ExecuteAnalysis("c:\\file.cpp", "charset1",
                new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript }, null, null, CancellationToken.None);

            analyzers[0].RequestAnalysisCalled.Should().BeFalse();
            analyzers[2].RequestAnalysisCalled.Should().BeFalse();

            // Both analyzers that support analysis should be given the chance to handle the request.
            analyzers[1].RequestAnalysisCalled.Should().BeTrue();
            analyzers[3].RequestAnalysisCalled.Should().BeTrue();
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3966:Objects should not be disposed more than once",
            Justification = "Deliberately disposing multiple times to test correct handling by the test subject")]
        public void CleanUp_MonitorDisposed()
        {
            // Arrange
            var analyzers = new DummyAnalyzer[]
            {
                new DummyAnalyzer()
            };

            var monitorMock = new Mock<SonarLint.VisualStudio.Integration.Vsix.Analysis.IAnalysisConfigMonitor>();
            var disposableMock = monitorMock.As<IDisposable>();

            var controller = new AnalyzerController(new TestLogger(), analyzers, monitorMock.Object);

            // Act - Dispose multiple times
            controller.Dispose();
            controller.Dispose();
            controller.Dispose();

            // Assert
            disposableMock.Verify(x => x.Dispose(), Times.Once);
        }

        private class DummyAnalyzer : IAnalyzer
        {
            private readonly AnalysisLanguage[] supportedLanguages;

            public bool RequestAnalysisCalled { get; private set; }

            public DummyAnalyzer(params AnalysisLanguage[] supportedLanguages)
            {
                this.supportedLanguages = supportedLanguages;
            }

            public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
            {
                return supportedLanguages?.Intersect(languages).Count() > 0;
            }

            public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer,
                ProjectItem projectItem, CancellationToken cancellationToken)
            {
                detectedLanguages.Should().NotBeNull();
                detectedLanguages.Any().Should().BeTrue();
                IsAnalysisSupported(detectedLanguages).Should().BeTrue();

                RequestAnalysisCalled = true;
            }
        }
    }
}

