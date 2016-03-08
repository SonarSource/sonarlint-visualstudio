//-----------------------------------------------------------------------
// <copyright file="VsShellUtilsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class VsShellUtilsTests
    {
        [TestMethod]
        public void VsShellUtils_ActivateSolutionExplorer()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var dteMock = new DTEMock();
            serviceProvider.RegisterService(typeof(DTE), dteMock);

            // Sanity
            Assert.IsFalse(dteMock.ToolWindows.SolutionExplorer.Window.Active);

            // Act
            VsShellUtils.ActivateSolutionExplorer(serviceProvider);

            // Verify
            Assert.IsTrue(dteMock.ToolWindows.SolutionExplorer.Window.Active, "Expected to become Active");
        }

        [TestMethod]
        public void VsShellUtils_SaveSolution_Silent()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) =>
            {
                Assert.AreEqual(__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, (__VSSLNSAVEOPTIONS)options, "Unexpected save options");
                Assert.IsNull(hierarchy, "Expecting the scope to be the whole solution");
                Assert.AreEqual(0U, docCookie, "Expecting the scope to be the whole solution");

                return VSConstants.S_OK;
            };

            // Act + Verify
            Assert.IsTrue(VsShellUtils.SaveSolution(serviceProvider, silent: true));
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "S1854:Dead stores should be removed", 
            Justification = "False positive: hrResult is used in lambda", 
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.UnitTests.VsShellUtilsTests.VsShellUtils_SaveSolution_Prompt")]
        [TestMethod]
        public void VsShellUtils_SaveSolution_Prompt()
        {
            // Setup
            var serviceProvider = new ConfigurableServiceProvider();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            int hrResult = 0;
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) =>
            {
                Assert.AreEqual(__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty | __VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave, (__VSSLNSAVEOPTIONS)options, "Unexpected save options");
                Assert.IsNull(hierarchy, "Expecting the scope to be the whole solution");
                Assert.AreEqual(0U, docCookie, "Expecting the scope to be the whole solution");

                return hrResult;
            };

            // Case 1: user selected 'Yes'
            hrResult = VSConstants.S_OK; //0

            // Act + Verify
            Assert.IsTrue(VsShellUtils.SaveSolution(serviceProvider, silent: false));

            // Case 2: user selected 'No'
            hrResult = VSConstants.S_FALSE; //1

            // Act + Verify
            Assert.IsFalse(VsShellUtils.SaveSolution(serviceProvider, silent: false));

            // Case 3: user selected 'Cancel'
            hrResult = VSConstants.E_ABORT;

            // Act + Verify
            Assert.IsFalse(VsShellUtils.SaveSolution(serviceProvider, silent: false));
        }
    }
}
