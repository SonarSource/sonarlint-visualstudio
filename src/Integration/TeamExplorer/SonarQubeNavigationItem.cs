//-----------------------------------------------------------------------
// <copyright file="SonarQubeNavigationItem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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

        private ITeamExplorerController controller;

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
            this.controller.ShowConnectionsPage();
        }
    }
}
