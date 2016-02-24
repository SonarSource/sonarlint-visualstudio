//-----------------------------------------------------------------------
// <copyright file="RelayCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class RelayCommandTests
    {
        [TestMethod]
        public void RelayCommand_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Setup
            var command = new RelayCommand(() => { });

            // Act + Verify
            Assert.IsTrue(command.CanExecute());
        }

        [TestMethod]
        public void RelayCommandOfT_Ctor_EmptyPredicate_CanAlwaysExecute()
        {
            // Setup
            var command = new RelayCommand<object>(x => { });

            // Act + Verify
            Assert.IsTrue(command.CanExecute(null));
        }
    }
}
