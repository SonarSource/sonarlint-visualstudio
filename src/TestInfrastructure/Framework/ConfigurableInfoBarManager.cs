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
using FluentAssertions;
using Microsoft.VisualStudio.Imaging.Interop;
using SonarLint.VisualStudio.Integration.InfoBar;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableInfoBarManager : IInfoBarManager
    {
        private readonly Dictionary<Guid, ConfigurableInfoBar> attached = new Dictionary<Guid, ConfigurableInfoBar>();

        #region IInfoBarManager

        IInfoBar IInfoBarManager.AttachInfoBar(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker)
        {
            this.attached.ContainsKey(toolwindowGuid).Should().BeFalse("Info bar is already attached to tool window {0}", toolwindowGuid);

            var infoBar = new ConfigurableInfoBar(message, buttonText, imageMoniker);
            this.attached[toolwindowGuid] = infoBar;
            return infoBar;
        }

        void IInfoBarManager.DetachInfoBar(IInfoBar currentInfoBar)
        {
            this.attached.Values.Contains(currentInfoBar).Should().BeTrue("Info bar is not attached");
            this.attached.Remove(attached.Single(kv => kv.Value == currentInfoBar).Key);
        }

        #endregion IInfoBarManager

        #region Test Helpers

        public ConfigurableInfoBar AssertHasAttachedInfoBar(Guid toolwindowGuid)
        {
            ConfigurableInfoBar infoBar = null;
            this.attached.TryGetValue(toolwindowGuid, out infoBar).Should().BeTrue("The tool window {0} has no attached info bar", toolwindowGuid);
            return infoBar;
        }

        public void AssertHasNoAttachedInfoBar(Guid toolwindowGuid)
        {
            this.attached.ContainsKey(toolwindowGuid).Should().BeFalse("The tool window {0} has attached info bar", toolwindowGuid);
        }

        #endregion Test Helpers
    }
}