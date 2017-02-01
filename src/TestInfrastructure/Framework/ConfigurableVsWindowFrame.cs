/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using FluentAssertions;
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