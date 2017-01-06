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

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Event arguments for cancellation support changes
    /// </summary>
    public class CancellationSupportChangedEventArgs : ProgressEventArgs
    {
        /// <summary>
        /// Constructs event arguments used to update cancellable state of the controller
        /// </summary>
        /// <param name="cancellable">Latest cancellability state</param>
        public CancellationSupportChangedEventArgs(bool cancellable)
        {
            this.Cancellable = cancellable;
        }

        /// <summary>
        /// The current cancellability state
        /// </summary>
        public bool Cancellable
        {
            get;
            private set;
        }
    }
}
