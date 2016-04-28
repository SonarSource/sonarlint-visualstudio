//-----------------------------------------------------------------------
// <copyright file="SolutionAnalysisRequesterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.ComponentModel.Composition.Primitives;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    [TestClass]
    public class SolutionAnalysisRequesterTests
    {
        private ConfigurableServiceProvider serviceProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), new ConfigurableVsOutputWindow());
        }

        [TestMethod]
        public void SolutionAnalysisRequester_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionAnalysisRequester(null, null));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionAnalysisRequester(null, new AdhocWorkspace()));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionAnalysisRequester(this.serviceProvider, null));
            try
            {
#pragma warning disable S1848 // Only testing ctor does not throw exceptions; no need to keep resulting instance.
                new SolutionAnalysisRequester(this.serviceProvider, new AdhocWorkspace());
#pragma warning restore S1848
            }
            catch (Exception)
            {
                Assert.Fail("SolutionAnalysisRequester constructor should not throw exception on adequate input");
            }
        }

        [TestMethod]
        public void SolutionAnalysisRequester_FlipFullSolutionAnalysisFlag()
        {
            Option<bool> option = new Option<bool>(SolutionAnalysisRequester.OptionFeatureRuntime,
                SolutionAnalysisRequester.OptionNameFullSolutionAnalysis, true);

            SolutionAnalysisRequester testSubject = new SolutionAnalysisRequester(this.serviceProvider, new AdhocWorkspace(), option);
            bool optionInitialValue = testSubject.GetOptionValue();

            // Act
            testSubject.FlipFullSolutionAnalysisFlag();

            // Verify
            Assert.AreEqual(!optionInitialValue,
                testSubject.GetOptionValue(),
                "Option should be inverted");
        }

        [TestMethod]
        public void SolutionAnalysisRequester_Reanalyze_DoNotChangeOriginalFlagValue()
        {
            Option<bool> option = new Option<bool>(SolutionAnalysisRequester.OptionFeatureRuntime,
                SolutionAnalysisRequester.OptionNameFullSolutionAnalysis, true);

            SolutionAnalysisRequester testSubject = new SolutionAnalysisRequester(this.serviceProvider, new AdhocWorkspace(), option);
            bool optionInitialValue = testSubject.GetOptionValue();

            // Act
            testSubject.ReanalyzeSolution();

            // Verify
            Assert.AreEqual(optionInitialValue,
                testSubject.GetOptionValue(),
                "Option should not be inverted");
        }

        [TestMethod]
        public void SonarAnalyzerManager_Triggers_SolutionBindingChanged_ReanalyzeSolution()
        {
            ConfigurableHost host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            Export mefExport1 = MefTestHelpers.CreateExport<IHost>(host);

            ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            Export mefExport2 = MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(activeSolutionBoundTracker);

            IComponentModel mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);

            ConfigurableSolutionAnalysisRequester solutionAnalysisRequester = new ConfigurableSolutionAnalysisRequester();

            using (new SonarAnalyzerManager(this.serviceProvider, new AdhocWorkspace(), solutionAnalysisRequester))
            {
                // Sanity
                Assert.AreEqual(0, solutionAnalysisRequester.ReanalyzeSolutionCallCount);

                // Act
                activeSolutionBoundTracker.SimulateSolutionBindingChanged(true);

                // Verify
                Assert.AreEqual(1, solutionAnalysisRequester.ReanalyzeSolutionCallCount);
            }
        }
    }
}
