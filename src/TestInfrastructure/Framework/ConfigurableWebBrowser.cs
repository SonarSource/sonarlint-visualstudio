//-----------------------------------------------------------------------
// <copyright file="ConfigurableWebBrowser.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableWebBrowser : IWebBrowser
    {
        private readonly IList<string> navigatedUrls = new List<string>();

        #region Test Helpers
        public void AssertNavigateToCalls(int numCalls)
        {
            Assert.AreEqual(numCalls, this.navigatedUrls.Count, "Unexpected number of calls to NavigateTo");
        }

        public void AssertRequestToNavigateTo(string url)
        {
            Assert.IsTrue(navigatedUrls.Contains(url), $"URL '{url}' was not navigated to");
        }

        #endregion

        #region IWebBrowser

        void IWebBrowser.NavigateTo(string url)
        {
            this.navigatedUrls.Add(url);
        }

        #endregion
    }
}
