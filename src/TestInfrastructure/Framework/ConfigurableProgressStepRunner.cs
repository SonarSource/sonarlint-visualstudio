//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressStepRunner.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Progress;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableProgressStepRunner : IProgressStepRunnerWrapper
    {
        private int abortAllNumberOfCalls;
        private IProgressControlHost currentHost;

        #region IProgressStepRunnerWrapper
        void IProgressStepRunnerWrapper.AbortAll()
        {
            this.abortAllNumberOfCalls++;
        }

        void IProgressStepRunnerWrapper.ChangeHost(IProgressControlHost host)
        {
            Assert.IsNotNull(host);

            this.currentHost = host;
        }
        #endregion

        #region Test helper
        public void AssertAbortAllCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, this.abortAllNumberOfCalls, "AbortAll was not called expected number of times");
        }

        public void AssertCurrentHost(IProgressControlHost expectedHost)
        {
            Assert.AreSame(expectedHost, this.currentHost);
        }

        public void AssertNoCurrentHost()
        {
            Assert.IsNull(this.currentHost);
        }
        #endregion
    }
}