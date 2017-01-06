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
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Base class for <see cref="EventArgs"/>
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        // The base class has debug only verification code that can be used to verify serialization
        // of event raising and handling
        private int handled = 0;

        internal void Handled()
        {
            Interlocked.Increment(ref this.handled);
        }

        internal void CheckHandled()
        {
            Debug.WriteLine("The event arguments {0} were handled by {1} handlers", this.GetType().FullName, this.handled);
            Debug.Assert(this.handled > 0, "Caught unhandled event which was supposed to be handled", "Arguments type: {0}", this.GetType().FullName);
        }
    }
}
