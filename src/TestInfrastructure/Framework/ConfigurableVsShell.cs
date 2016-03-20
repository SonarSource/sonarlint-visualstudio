//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsShell.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsShell : IVsShell
    {
        private readonly Dictionary<int, Func<object>> propertyGetters = new Dictionary<int, Func<object>>();

        #region IVsShell
        int IVsShell.AdviseBroadcastMessages(IVsBroadcastMessageEvents pSink, out uint pdwCookie)
        {
            throw new NotImplementedException();
        }

        int IVsShell.AdviseShellPropertyChanges(IVsShellPropertyEvents pSink, out uint pdwCookie)
        {
            throw new NotImplementedException();
        }

        int IVsShell.GetPackageEnum(out IEnumPackages ppenum)
        {
            throw new NotImplementedException();
        }

        int IVsShell.GetProperty(int propid, out object pvar)
        {
            pvar = null;
            Func<object> getter;
            if (this.propertyGetters.TryGetValue(propid, out getter))
            {
                pvar = getter();
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsShell.IsPackageInstalled(ref Guid guidPackage, out int pfInstalled)
        {
            throw new NotImplementedException();
        }

        int IVsShell.IsPackageLoaded(ref Guid guidPackage, out IVsPackage ppPackage)
        {
            throw new NotImplementedException();
        }

        int IVsShell.LoadPackage(ref Guid guidPackage, out IVsPackage ppPackage)
        {
            throw new NotImplementedException();
        }

        int IVsShell.LoadPackageString(ref Guid guidPackage, uint resid, out string pbstrOut)
        {
            throw new NotImplementedException();
        }

        int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out uint phinstOut)
        {
            throw new NotImplementedException();
        }

        int IVsShell.SetProperty(int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsShell.UnadviseBroadcastMessages(uint dwCookie)
        {
            throw new NotImplementedException();
        }

        int IVsShell.UnadviseShellPropertyChanges(uint dwCookie)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Test helpers
        public void RegisterPropertyGetter(int propertyId, Func<object> getter)
        {
            this.propertyGetters[propertyId] = getter;
        }
        #endregion
    }
}
