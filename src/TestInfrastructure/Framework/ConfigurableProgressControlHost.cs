//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressControlHost.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableProgressControlHost : IProgressControlHost
    {
        private ProgressControl progressControl = null;

        #region IProgressControlHost
        void IProgressControlHost.Host(ProgressControl progressControl)
        {
            Assert.IsNotNull(progressControl);
            this.progressControl = progressControl;
        }
        #endregion

        #region Test helpers
        public void AssertHasProgressControl()
        {
            Assert.IsNotNull(this.progressControl, "ProgressControl was not set");
        }

        public void AssertHasNoProgressControl()
        {
            Assert.IsNull(this.progressControl, "ProgressControl was set");
        }
        #endregion
    }
}
