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
using System.Linq;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Some of the tests cover exceptions in which we have asserts as well, we want to ignore those asserts during tests
    /// </summary>
    public class AssertIgnoreScope : IDisposable
    {
        public AssertIgnoreScope()
        {
            DefaultTraceListener listener = Debug.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
            if (listener != null)
            {
                listener.AssertUiEnabled = false;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultTraceListener listener = Debug.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
                if (listener != null)
                {
                    listener.AssertUiEnabled = true;
                }
            }
        }
    }
}