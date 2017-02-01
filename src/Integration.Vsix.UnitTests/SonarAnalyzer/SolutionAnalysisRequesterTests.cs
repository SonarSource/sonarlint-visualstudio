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

using System;
using System.ComponentModel.Composition.Primitives;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    [TestClass]
    public class SolutionAnalysisRequesterTests
    {
        [TestMethod]
        public void Ctor_WhenUsingNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var workspaceConfigurator = new WorkspaceConfigurator(new AdhocWorkspace());

            // Act & Assert
            Exceptions.Expect<ArgumentNullException>(() =>
                new SolutionAnalysisRequester(null, workspaceConfigurator));
        }

        [TestMethod]
        public void Ctor_WhenUsingNullWorkspace_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Exceptions.Expect<ArgumentNullException>(() =>
                new SolutionAnalysisRequester(new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false), null));
        }

        [TestMethod]
        public void FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndInvalidVisualStudioVersion_ReturnsNullOptionKeyAndWritesToTheOutputWindow()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            var visualStudioVersion = "42";
            serviceProvider.RegisterService(typeof(EnvDTE.DTE), new DTEMock { Version = visualStudioVersion });
            var workspaceConfigurator = new WorkspaceConfigurator(new AdhocWorkspace());

            // Act
            var optionKey = SolutionAnalysisRequester.FindFullSolutionAnalysisOptionKey(serviceProvider, workspaceConfigurator);

            // Assert
            optionKey.Should().BeNull();
            outputWindowPane.AssertOutputStrings(string.Format(Strings.InvalidVisualStudioVersion, visualStudioVersion));
        }

        [TestMethod]
        public void FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndVisualStudio2015Version_ReturnsNonNullOptionKeyAndDoesNotWriteToTheOutputWindow()
        {
            // Arrange, Act, Assert
            FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndVisualStudioVersion_ReturnsNonNullOptionKeyAndDoesNotWriteToTheOutputWindow(VisualStudioConstants.VS2015VersionNumber);
        }

        [TestMethod]
        public void FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndVisualStudio2017Version_ReturnsNonNullOptionKeyAndDoesNotWriteToTheOutputWindow()
        {
            // Arrange, Act, Assert
            FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndVisualStudioVersion_ReturnsNonNullOptionKeyAndDoesNotWriteToTheOutputWindow(VisualStudioConstants.VS2017VersionNumber);
        }

        private void FindFullSolutionAnalysisOptionKey_WithProperArgumentsAndVisualStudioVersion_ReturnsNonNullOptionKeyAndDoesNotWriteToTheOutputWindow(string visualStudioVersion)
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            var dteMock = new DTEMock { Version = visualStudioVersion };
            serviceProvider.RegisterService(typeof(EnvDTE.DTE), dteMock);
            var roslynRuntimeOptions = RoslynRuntimeOptions.Resolve(serviceProvider);
            var option = new Option<bool>(roslynRuntimeOptions.RuntimeOptionsFeatureName, roslynRuntimeOptions.FullSolutionAnalysisOptionName);
            var workspaceConfigurator = new WorkspaceConfiguratorMock();
            workspaceConfigurator.FindOptionByNameFunc =
                (featureName, fsaName) =>
                {
                    if (featureName == roslynRuntimeOptions.RuntimeOptionsFeatureName &&
                        fsaName == roslynRuntimeOptions.FullSolutionAnalysisOptionName)
                    {
                        return option;
                    }
                    else
                    {
                        FluentAssertions.Execution.Execute.Assertion.FailWith("Method was called with unexpected parameters. Expecting '"
                            + roslynRuntimeOptions.RuntimeOptionsFeatureName + "' and '"
                            + roslynRuntimeOptions.FullSolutionAnalysisOptionName + "', got '"
                            + featureName + "' and '" + fsaName + "'");
                        return null;
                    }
                };

            // Act
            var optionKey = SolutionAnalysisRequester.FindFullSolutionAnalysisOptionKey(serviceProvider, workspaceConfigurator);

            // Assert
            optionKey.Should().NotBeNull();
            outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ReanalyzeSolution_WhenOptionKeyIsNull_WritesToTheOutputWindow()
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            var visualStudioVersion = "42";
            serviceProvider.RegisterService(typeof(EnvDTE.DTE), new DTEMock { Version = visualStudioVersion });
            var workspaceConfigurator = new WorkspaceConfigurator(new AdhocWorkspace());
            var solutionAnalyzerRequester = new SolutionAnalysisRequester(serviceProvider, workspaceConfigurator);

            // Act
            solutionAnalyzerRequester.ReanalyzeSolution();

            // Assert
            outputWindowPane.AssertOutputStrings(
                string.Format(Strings.InvalidVisualStudioVersion, visualStudioVersion),
                Strings.MissingRuntimeOptionsInWorkspace);
        }

        [TestMethod]
        public void ReanalyzeSolution_WhenOptionKeyIsNotNullWithVs2015_ReanalyzeSolutionAndDoesNotWriteToTheOutputWindow()
        {
            // Arrange, Act, Assert
            ReanalyzeSolution_WhenOptionKeyIsNotNull_ReanalyzeSolutionAndDoesNotWriteToTheOutputWindow(VisualStudioConstants.VS2015VersionNumber);
        }

        [TestMethod]
        public void ReanalyzeSolution_WhenOptionKeyIsNotNullWithVs2017_ReanalyzeSolutionAndDoesNotWriteToTheOutputWindow()
        {
            // Arrange, Act, Assert
            ReanalyzeSolution_WhenOptionKeyIsNotNull_ReanalyzeSolutionAndDoesNotWriteToTheOutputWindow(VisualStudioConstants.VS2017VersionNumber);
        }

        private void ReanalyzeSolution_WhenOptionKeyIsNotNull_ReanalyzeSolutionAndDoesNotWriteToTheOutputWindow(string visualStudioVersion)
        {
            // Arrange
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            var dteMock = new DTEMock { Version = visualStudioVersion };
            serviceProvider.RegisterService(typeof(EnvDTE.DTE), dteMock);
            var roslynRuntimeOptions = RoslynRuntimeOptions.Resolve(serviceProvider);
            var option = new Option<bool>(roslynRuntimeOptions.RuntimeOptionsFeatureName, roslynRuntimeOptions.FullSolutionAnalysisOptionName);
            var workspaceConfigurator = new WorkspaceConfiguratorMock();
            workspaceConfigurator.FindOptionByNameFunc =
                (featureName, fsaName) =>
                {
                    if (featureName == roslynRuntimeOptions.RuntimeOptionsFeatureName &&
                        fsaName == roslynRuntimeOptions.FullSolutionAnalysisOptionName)
                    {
                        return option;
                    }
                    else
                    {
                        FluentAssertions.Execution.Execute.Assertion.FailWith("Method was called with unexpected parameters. Expecting '"
                            + roslynRuntimeOptions.RuntimeOptionsFeatureName + "' and '"
                            + roslynRuntimeOptions.FullSolutionAnalysisOptionName + "', got '"
                            + featureName + "' and '" + fsaName + "'");
                        return null;
                    }
                };
            int callCount = 0;
            workspaceConfigurator.ToggleBooleanOptionKeyAction =
                optionKey =>
                {
                    if (optionKey.Option.Feature == roslynRuntimeOptions.RuntimeOptionsFeatureName &&
                        optionKey.Option.Name == roslynRuntimeOptions.FullSolutionAnalysisOptionName)
                    {
                        callCount++;
                    }
                    else
                    {
                        FluentAssertions.Execution.Execute.Assertion.FailWith("Method was called with unexpected parameters. Expecting '"
                            + roslynRuntimeOptions.RuntimeOptionsFeatureName + "' and '"
                            + roslynRuntimeOptions.FullSolutionAnalysisOptionName + "', got '"
                            + optionKey.Option.Feature + "' and '" + optionKey.Option.Name + "'");
                    }
                };
            var solutionAnalyzerRequester = new SolutionAnalysisRequester(serviceProvider, workspaceConfigurator);

            // Act
            solutionAnalyzerRequester.ReanalyzeSolution();

            // Assert
            outputWindowPane.AssertOutputStrings(0);
            callCount.Should().Be(2);
        }

        [TestMethod]
        public void SonarAnalyzerManager_Triggers_SolutionBindingChanged_ReanalyzeSolution()
        {
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            var outputWindow = new ConfigurableVsOutputWindow();
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            ConfigurableHost host = new ConfigurableHost(serviceProvider, Dispatcher.CurrentDispatcher);
            Export mefExport1 = MefTestHelpers.CreateExport<IHost>(host);

            ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            Export mefExport2 = MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(activeSolutionBoundTracker);

            IComponentModel mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);
            serviceProvider.RegisterService(typeof(SComponentModel), mefModel);

            ConfigurableSolutionAnalysisRequester solutionAnalysisRequester = new ConfigurableSolutionAnalysisRequester();

            using (new SonarAnalyzerManager(serviceProvider, new AdhocWorkspace(), solutionAnalysisRequester))
            {
                // Sanity
                solutionAnalysisRequester.ReanalyzeSolutionCallCount.Should().Be(0);

                // Act
                activeSolutionBoundTracker.SimulateSolutionBindingChanged(true);

                // Assert
                solutionAnalysisRequester.ReanalyzeSolutionCallCount.Should().Be(1);
            }
        }
    }
}