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
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IWebBrowser)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class WebBrowser : IWebBrowser
    {
        public void NavigateTo(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && IsSafeScheme(uri.Scheme))
            {
                var startInfo = new ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            else
            {
                Debug.Fail("Provided URL was not in a correct format");
            }
        }

        private static bool IsSafeScheme(string scheme)
        {
            return scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        }
    }
}
