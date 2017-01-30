/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.Windows.Threading;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    public class SonarAnalyzerManagerTestsForBound
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;
        private SonarAnalyzerManager testSubject;

        public SonarAnalyzerManagerTestsForBound()
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
