/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
    public class ConfigurableVsWindowFrame : IVsWindowFrame
    {
        private readonly Dictionary<int, object> properties = new Dictionary<int, object>();
        internal int ShowNoActivateCalledCount { get; private set; }

        #region IVsWindowFrame

        int IVsWindowFrame.CloseFrame(uint grfSaveOptions)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.GetFramePos(VSSETFRAMEPOS[] pdwSFP, out Guid pguidRelativeTo, out int px, out int py, out int pcx, out int pcy)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.GetGuidProperty(int propid, out Guid pguid)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.GetProperty(int propid, out object pvar)
        {
            if (this.properties.TryGetValue(propid, out pvar))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsWindowFrame.Hide()
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.IsOnScreen(out int pfOnScreen)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.IsVisible()
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.QueryViewInterface(ref Guid riid, out IntPtr ppv)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.SetFramePos(VSSETFRAMEPOS dwSFP, ref Guid rguidRelativeTo, int x, int y, int cx, int cy)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.SetGuidProperty(int propid, ref Guid rguid)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.SetProperty(int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.Show()
        {
            throw new NotImplementedException();
        }

        int IVsWindowFrame.ShowNoActivate()
        {
            this.ShowNoActivateCalledCount++;
            return VSConstants.S_OK;
        }

        #endregion IVsWindowFrame

        #region Test helpers

        public void RegisterProperty(int propertyId, object value)
        {
            this.properties[propertyId] = value;
        }

        #endregion Test helpers
    }
}