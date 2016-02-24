//-----------------------------------------------------------------------
// <copyright file="VsServiceProviderHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class VsServiceProviderHelper
    {
        static VsServiceProviderHelper()
        {
            GlobalServiceProvider = new ConfigurableServiceProvider();
            GlobalServiceProvider.RegisterService(typeof(SVsActivityLog), new ConfigurableVsActivityLog(), true);
            ServiceProvider.CreateFromSetSite(GlobalServiceProvider);
        }

        public static ConfigurableServiceProvider GlobalServiceProvider
        {
            get;
        }
    }
}
