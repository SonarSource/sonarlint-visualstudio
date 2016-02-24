//-----------------------------------------------------------------------
// <copyright file="ConnectSectionViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class ConnectSectionViewModelTests
    {
        [TestMethod]
        public void ConnectSectionViewModel_Ctor_IsVisibleAndExpanded()
        {
            var vm = new ConnectSectionViewModel();

            Assert.IsTrue(vm.IsVisible);
            Assert.IsTrue(vm.IsExpanded);
        }
    }
}
