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

using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    [Export(typeof(ITeamExplorerController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TeamExplorerController : ITeamExplorerController
    {
        private readonly ITeamExplorer teamExplorer;

        internal /* testing purposes */ ITeamExplorer TeamExplorer => this.teamExplorer;

        [ImportingConstructor]
        public TeamExplorerController([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.teamExplorer = serviceProvider.GetService<ITeamExplorer>();
            if (this.TeamExplorer == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.MissingService, nameof(ITeamExplorer)), nameof(serviceProvider));
            }
        }

        public void ShowSonarQubePage()
        {
            Debug.Assert(this.TeamExplorer != null, "Shouldn't be created without the Team Explorer service");
            this.TeamExplorer.NavigateToPage(new Guid(SonarQubePage.PageId), null);
        }
    }
}
