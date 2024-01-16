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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class EslintBridgeAnalyzerFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<EslintBridgeAnalyzerFactory, IEslintBridgeAnalyzerFactory>(
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IAnalysisConfigMonitor>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Create_CreatesEslintBridgeAnalyzer()
        {
            var testSubject = new EslintBridgeAnalyzerFactory(
                Mock.Of<IActiveSolutionTracker>(),
                Mock.Of<IAnalysisConfigMonitor>(),
                Mock.Of<ILogger>());

            var result = testSubject.Create(Mock.Of<IRulesProvider>(), Mock.Of<IEslintBridgeClient>());

            result.Should().NotBeNull();
        }
    }
}
