//-----------------------------------------------------------------------
// <copyright file="ConfigurableRuleSetConflictsController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetConflictsController : IRuleSetConflictsController
    {
        private int clearCalled;

        #region IRuleSetConflictsController
        bool IRuleSetConflictsController.CheckForConflicts()
        {
            return this.HasConflicts;
        }

        void IRuleSetConflictsController.Clear()
        {
            this.clearCalled++;
        }
        #endregion

        #region Test helper
        public bool HasConflicts { get; set; }

        public void AssertClearCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, clearCalled, "Clear was called unexpected number of times");
        }
        #endregion
    }
}
