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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalyzerControllerTests
    {
        [TestMethod]
        public void IsAnalysisSupported()
        {
            // Arrange
            var analyzers = new[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var controller = CreateTestSubject(analyzers);

            // Act and Assert
            controller.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily }).Should().BeTrue();
            controller.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript }).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_FileIsNotAnalyzable_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new[]
            {
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
            };

            var analyzableFileIndicator = new Mock<IAnalyzableFileIndicator>();
            analyzableFileIndicator.Setup(x => x.ShouldAnalyze("c:\\file.cpp")).Returns(false);

            var controller = CreateTestSubject(analyzers, analyzableFileIndicator: analyzableFileIndicator.Object);

            // Act
            controller.ExecuteAnalysis("c:\\file.cpp", "charset1",
                new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript }, null, null, CancellationToken.None);

            analyzers.Any(x => x.RequestAnalysisCalled).Should().BeFalse();

            // Verify that the file was checked only once, regardless of number of analyzers
            analyzableFileIndicator.Verify(x => x.ShouldAnalyze(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void RequestAnalysis_FileIsAnalyzable_LanguageIsNotSupported_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var analyzableFileIndicator = new Mock<IAnalyzableFileIndicator>();
            analyzableFileIndicator.Setup(x => x.ShouldAnalyze("c:\\file.cpp")).Returns(true);

            var controller = CreateTestSubject(analyzers, analyzableFileIndicator: analyzableFileIndicator.Object);

            // Act
            controller.ExecuteAnalysis("c:\\file.cpp", "charset1", new[] { AnalysisLanguage.Javascript }, null, null, CancellationToken.None);

            analyzers.Any(x => x.RequestAnalysisCalled).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_FileIsAnalyzable_LanguageIsSupported_RequestAnalysisIsCalled()
        {
            // Arrange
            var analyzers = new[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
                new DummyAnalyzer(),
                new DummyAnalyzer(AnalysisLanguage.CFamily),
            };

            var analyzableFileIndicator = new Mock<IAnalyzableFileIndicator>();
            analyzableFileIndicator.Setup(x => x.ShouldAnalyze("c:\\file.cpp")).Returns(true);

            var controller = CreateTestSubject(analyzers, analyzableFileIndicator: analyzableFileIndicator.Object);

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
            var analyzers = new[]
            {
                new DummyAnalyzer()
            };

            var monitorMock = new Mock<IAnalysisConfigMonitor>();
            var disposableMock = monitorMock.As<IDisposable>();

            var controller = CreateTestSubject(analyzers, monitorMock.Object);

            // Act - Dispose multiple times
            controller.Dispose();
            controller.Dispose();
            controller.Dispose();

            // Assert
            disposableMock.Verify(x => x.Dispose(), Times.Once);
        }

        private static AnalyzerController CreateTestSubject(IEnumerable<IAnalyzer> analyzers,
            IAnalysisConfigMonitor analysisConfigMonitor = null,
            IAnalyzableFileIndicator analyzableFileIndicator = null) =>
            new(Mock.Of<ILogger>(), analyzers, analysisConfigMonitor, analyzableFileIndicator);

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

            public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
                IIssueConsumer consumer, IAnalyzerOptions analyzerOptions, CancellationToken cancellationToken, Guid analysisId = default)
            {
                detectedLanguages.Should().NotBeNull();
                detectedLanguages.Any().Should().BeTrue();
                IsAnalysisSupported(detectedLanguages).Should().BeTrue();

                RequestAnalysisCalled = true;
            }
        }
    }
}
