//-----------------------------------------------------------------------
// <copyright file="WebBrowser.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IWebBrowser)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class WebBrowser : IWebBrowser
    {
        public void NavigateTo(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && IsSafeScheme(uri.Scheme))
            {
                var startInfo = new ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            else
            {
                Debug.Fail("Provided URL was not in a correct format");
            }
        }

        private static bool IsSafeScheme(string scheme)
        {
            return scheme.ToLowerInvariant().Contains("http");
        }
    }
}
