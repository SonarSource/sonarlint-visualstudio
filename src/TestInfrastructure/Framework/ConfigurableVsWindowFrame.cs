//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsWindowFrame.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsWindowFrame : IVsWindowFrame
    {
        private readonly Dictionary<int, object> properties = new Dictionary<int, object>();
        private int showNoActivateCalled;

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
            this.showNoActivateCalled++;
            return VSConstants.S_OK;
        }
        #endregion

        #region Test helpers
        public void RegisterProperty(int propertyId, object value)
        {
            this.properties[propertyId] = value;
        }

        public void AssertShowNoActivateCalled(int expectedNumberOfTimes)
        {
            Assert.AreEqual(expectedNumberOfTimes, this.showNoActivateCalled, "ShowNoActivate called unexpected number of times");
        }
        #endregion
    }
}
