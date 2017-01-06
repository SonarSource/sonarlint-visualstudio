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

using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace SonarLint.VisualStudio.Integration.InfoBar
{
    /// <summary>
    /// Info bar manager
    /// </summary>
    public interface IInfoBarManager
    {
        /// <summary>
        /// Attached to an existing tool window
        /// </summary>
        /// <param name="toolwindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="buttonText">The button text</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBar(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker);

        /// <summary>
        /// Detaches an <see cref="IInfoBar"/> from its tool window
        /// </summary>
        /// <param name="currentInfoBar">Instance of <see cref="IInfoBar"/> created by <see cref="AttachInfoBar(Guid, string, string)"/></param>
        void DetachInfoBar(IInfoBar currentInfoBar);
    }
}
