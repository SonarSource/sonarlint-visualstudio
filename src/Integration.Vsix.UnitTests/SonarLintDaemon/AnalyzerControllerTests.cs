/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
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
                new DummyAnalyzer(SonarLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers);

            // Act and Assert
            controller.IsAnalysisSupported(new[] { SonarLanguage.CFamily }).Should().BeTrue();
            controller.IsAnalysisSupported(new[] { SonarLanguage.Javascript }).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_NotSupported_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new DummyAnalyzer[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(SonarLanguage.CFamily),
                new DummyAnalyzer(),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers);

            // Act
            controller.RequestAnalysis("c:\\file.cpp", "charset1", new[] { SonarLanguage.Javascript }, null, null);

            analyzers.Any(x => x.RequestAnalysisCalled).Should().BeFalse();
        }

        [TestMethod]
        public void RequestAnalysis_Supported_RequestAnalysisNotCalled()
        {
            // Arrange
            var analyzers = new DummyAnalyzer[]
            {
                new DummyAnalyzer(),
                new DummyAnalyzer(SonarLanguage.CFamily),
                new DummyAnalyzer(),
                new DummyAnalyzer(SonarLanguage.CFamily),
            };

            var controller = new AnalyzerController(new TestLogger(), analyzers);

            // Act
            controller.RequestAnalysis("c:\\file.cpp", "charset1", 
                new[] { SonarLanguage.CFamily, SonarLanguage.Javascript }, null, null);

            analyzers[0].RequestAnalysisCalled.Should().BeFalse();
            analyzers[2].RequestAnalysisCalled.Should().BeFalse();

            // Both analyzers that support analysis should be given the chance to handle the request.
            analyzers[1].RequestAnalysisCalled.Should().BeTrue();
            analyzers[3].RequestAnalysisCalled.Should().BeTrue();
        }

        private class DummyAnalyzer : IAnalyzer
        {
            private readonly SonarLanguage[] supportedLanguages;

            public bool RequestAnalysisCalled { get; private set; }

            public DummyAnalyzer(params SonarLanguage[] supportedLanguages)
            {
                this.supportedLanguages = supportedLanguages;
            }

            public bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages)
            {
                return supportedLanguages?.Intersect(languages).Count() > 0;
            }

            public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
            {
                detectedLanguages.Should().NotBeNull();
                detectedLanguages.Any().Should().BeTrue();
                IsAnalysisSupported(detectedLanguages).Should().BeTrue();

                RequestAnalysisCalled = true;
            }
        }
    }
}

