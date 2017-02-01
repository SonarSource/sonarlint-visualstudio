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
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsInfoBarUIElement : IVsInfoBarUIElement
    {
        private int cookies;
        private readonly Dictionary<uint, IVsInfoBarUIEvents> sinks = new Dictionary<uint, IVsInfoBarUIEvents>();

        #region IVsInfoBarUIElement

        int IVsInfoBarUIElement.Advise(IVsInfoBarUIEvents eventSink, out uint cookie)
        {
            eventSink.Should().NotBeNull();

            cookie = (uint)Interlocked.Increment(ref this.cookies);
            this.sinks[cookie] = eventSink;

            return VSConstants.S_OK;
        }

        int IVsInfoBarUIElement.Close()
        {
            this.IsClosed.Should().BeFalse("Already closed");

            this.IsClosed = true;
            this.SimulateClosedEvent();

            return VSConstants.S_OK;
        }

        int IVsUIElement.GetUIObject(out object ppUnk)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.GetUIObject(out object ppUnk)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.get_DataSource(out IVsUISimpleDataSource ppDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.get_DataSource(out IVsUISimpleDataSource ppDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.put_DataSource(IVsUISimpleDataSource pDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.put_DataSource(IVsUISimpleDataSource pDataSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIElement.TranslateAccelerator(IVsUIAccelerator pAccel)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.TranslateAccelerator(IVsUIAccelerator pAccel)
        {
            throw new NotImplementedException();
        }

        int IVsInfoBarUIElement.Unadvise(uint cookie)
        {
            this.sinks.Should().ContainKey(cookie);

            this.sinks.Remove(cookie);

            return VSConstants.S_OK;
        }

        #endregion IVsInfoBarUIElement

        #region Test helpers

        public bool IsClosed
        {
            get;
            set;
        }

        public void SimulateClickEvent(IVsInfoBarActionItem item = null)
        {
            this.sinks.Values.ToList().ForEach(v => v.OnActionItemClicked(this, item));
        }

        public void SimulateClosedEvent()
        {
            this.sinks.Values.ToList().ForEach(v => v.OnClosed(this));
        }

        public IVsInfoBar Model
        {
            get;
            set;
        }

        #endregion Test helpers
    }
}