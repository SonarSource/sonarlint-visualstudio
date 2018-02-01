/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsShell : IVsShell
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

        #endregion IVsShell

        #region Test helpers

        public void RegisterPropertyGetter(int propertyId, Func<object> getter)
        {
            this.propertyGetters[propertyId] = getter;
        }

        #endregion Test helpers
    }
}