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

using Moq;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis
{
    [TestClass]
    public class AnalyzerControllerTests
    {
        [TestMethod]
        public void RequestAnalysis_FileIsAnalyzable_LanguageIsSupported_RequestAnalysisIsCalled()
        {
            // Arrange
            var analyzer = new DummyAnalyzer();

            var controller = CreateTestSubject(analyzer);

            // Act
            controller.ExecuteAnalysis("c:\\file.cpp", Guid.NewGuid(),
                new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript }, null, CancellationToken.None);


            analyzer.RequestAnalysisCalled.Should().BeTrue();
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3966:Objects should not be disposed more than once",
            Justification = "Deliberately disposing multiple times to test correct handling by the test subject")]
        public void CleanUp_MonitorDisposed()
        {
            // Arrange

            var monitorMock = new Mock<IAnalysisConfigMonitor>();
            var disposableMock = monitorMock.As<IDisposable>();

            var controller = CreateTestSubject(new DummyAnalyzer(), monitorMock.Object);

            // Act - Dispose multiple times
            controller.Dispose();
            controller.Dispose();
            controller.Dispose();

            // Assert
            disposableMock.Verify(x => x.Dispose(), Times.Once);
        }

        private static AnalyzerController CreateTestSubject(IAnalyzer analyzer,
            IAnalysisConfigMonitor analysisConfigMonitor = null) =>
            new(analysisConfigMonitor, analyzer, Mock.Of<ILogger>());

        private class DummyAnalyzer : IAnalyzer
        {
            public bool RequestAnalysisCalled { get; private set; }

            public void ExecuteAnalysis(string path, Guid analysisId, IEnumerable<AnalysisLanguage> detectedLanguages, IAnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
            {
                detectedLanguages.Should().NotBeNull();
                detectedLanguages.Any().Should().BeTrue();

                RequestAnalysisCalled = true;
            }
        }
    }
}
