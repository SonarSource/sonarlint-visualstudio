//-----------------------------------------------------------------------
// <copyright file="VsShellUtilsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
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
    }
}
