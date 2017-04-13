/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
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
