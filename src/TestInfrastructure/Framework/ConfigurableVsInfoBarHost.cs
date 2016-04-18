//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsInfoBarHost.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarHost : IVsInfoBarHost
    {
        private readonly List<IVsUIElement> elements = new List<IVsUIElement>();

        #region IVsInfoBarHost
        void IVsInfoBarHost.AddInfoBar(IVsUIElement uiElement)
        {
            Assert.IsFalse(this.elements.Contains(uiElement));
            this.elements.Add(uiElement);
        }

        void IVsInfoBarHost.RemoveInfoBar(IVsUIElement uiElement)
        {
            Assert.IsTrue(this.elements.Contains(uiElement));
            this.elements.Remove(uiElement);
        }
        #endregion

        #region Test helpers
        public void AssertInfoBars(int expectedNumberOfInfoBars)
        {
            Assert.AreEqual(expectedNumberOfInfoBars, this.elements.Count, "Unexpected number of info bars");
        }

        public IEnumerable<ConfigurableVsInfoBarUIElement> MockedElements
        {
            get
            {
                return this.elements.OfType<ConfigurableVsInfoBarUIElement>();
            }
        }
        #endregion
    }
}
