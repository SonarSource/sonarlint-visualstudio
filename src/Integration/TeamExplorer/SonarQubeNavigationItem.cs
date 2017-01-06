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
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    [TeamExplorerNavigationItem(SonarQubeNavigationItem.ItemId, SonarQubeNavigationItem.Priority, TargetPageId = SonarQubePage.PageId)]
    internal class SonarQubeNavigationItem : TeamExplorerNavigationItemBase
    {
        public const string ItemId = "172AF455-5F42-46FC-BFE6-23227A05806B";
        public const int Priority = TeamExplorerNavigationItemPriority.Settings - 1;

        private readonly ITeamExplorerController controller;

        [ImportingConstructor]
        internal SonarQubeNavigationItem([Import] ITeamExplorerController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            this.controller = controller;

            this.Text = Strings.TeamExplorerPageTitle;
            this.IsVisible = true;
            this.IsEnabled = true;

            var image = ResourceHelper.Get<DrawingImage>("SonarQubeServerIcon");
            this.m_icon = image != null ? new DrawingBrush(image.Drawing) : null;

            this.m_defaultArgbColorBrush = ResourceHelper.Get<SolidColorBrush>("SQForegroundBrush");
        }

        public override void Execute()
        {
            this.controller.ShowSonarQubePage();
        }
    }
}
