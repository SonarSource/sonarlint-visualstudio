//-----------------------------------------------------------------------
// <copyright file="ConfigurableErrorListInfoBarController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableErrorListInfoBarController : IErrorListInfoBarController
    {
        private int refreshCalled;
        private int resetCalled;

        #region IErrorListInfoBarController
        void IErrorListInfoBarController.Refresh()
        {
            this.refreshCalled++;
        }

        void IErrorListInfoBarController.Reset()
        {
            this.resetCalled++;
        }
        #endregion

        #region Test helpers
        public void AssertRefreshCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, this.refreshCalled, $"{nameof(IErrorListInfoBarController.Refresh)} called unexpected number of times");
        }

        public void AssertResetCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, this.resetCalled, $"{nameof(IErrorListInfoBarController.Reset)} called unexpected number of times");
        }
        #endregion
    }
}
