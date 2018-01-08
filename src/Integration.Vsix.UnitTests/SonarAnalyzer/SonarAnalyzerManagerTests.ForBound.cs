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
using System.Collections.Generic;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    [TestClass]
    public class SonarAnalyzerManagerTestsForBound
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;
        private SonarAnalyzerManager testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider(false);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            var outputWindow = new ConfigurableVsOutputWindow();

            var mefExport1 = MefTestHelpers.CreateExport<IHost>(this.host);
            var mefExport2 = MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(this.activeSolutionBoundTracker);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);

            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.testSubject = new SonarAnalyzerManager(this.serviceProvider, new AdhocWorkspace());
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_Empty()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = false;

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(null))
                .Should().BeFalse("Unbound solution should never return true");

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>()))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_Conflicting()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = false;

            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new ConfigurableAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                    SonarAnalyzerManager.AnalyzerName)
            };

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_NonConflicting()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = false;

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new ConfigurableAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                    SonarAnalyzerManager.AnalyzerName)
            };

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_Empty()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = true;

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(null))
                .Should().BeTrue("Bound solution with no reference should never return true");

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>()))
                .Should().BeTrue("Bound solution with no reference should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_Conflicting()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = true;

            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
               "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new ConfigurableAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                    SonarAnalyzerManager.AnalyzerName)
            };

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Bound solution with conflicting analyzer name should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_NonConflicting()
        {
            this.activeSolutionBoundTracker.IsActiveSolutionBound = true;

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new ConfigurableAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                    SonarAnalyzerManager.AnalyzerName)
            };

            this.testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Bound solution with conflicting analyzer name should never return true");
        }
    }
}