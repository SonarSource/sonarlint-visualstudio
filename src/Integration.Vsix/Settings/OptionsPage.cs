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

using Microsoft.VisualStudio.Shell;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OptionsPage : DialogPage
    {
        public const string CategoryName = "SonarLint for VisualStudio";
        public const string PageName = "Security";

        private IIntegrationSettings settings;

        private IIntegrationSettings Settings
        {
            get
            {
                if (this.settings == null)
                {
                    this.settings = ServiceProvider.GlobalProvider.GetMefService<IIntegrationSettings>();
                    Debug.Assert(this.settings != null, "Failed to get IIntegrationSettings from MEF, no settings will be available!");
                }

                return this.settings;
            }
        }

        public override object AutomationObject
        {
            get
            {
                return this.Settings;
            }
        }
    }
}
