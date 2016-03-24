//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsInfoBarUIFactory.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarUIFactory : IVsInfoBarUIFactory
    {
        #region IVsInfoBarUIFactory
        IVsInfoBarUIElement IVsInfoBarUIFactory.CreateInfoBar(IVsInfoBar infoBar)
        {
            return new ConfigurableVsInfoBarUIElement { Model = infoBar };
        }
        #endregion
    }
}
