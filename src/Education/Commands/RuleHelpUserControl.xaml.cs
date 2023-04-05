/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Windows.Controls;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Education.Commands
{
    public sealed partial class RuleHelpUserControl : UserControl
    {
        private readonly IBrowserService browserService;
        private readonly IEducation education;

        internal RuleHelpUserControl(IBrowserService browserService, IEducation education)
        {
            this.browserService = browserService;
            this.education = education;

            InitializeComponent();
        }

        public void HandleRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // If the incoming URI can be decoded it means that the incoming URI is a cross reference rule
            // in which case it needs to be handed over to the education service.
            if (SonarRuleIdUriEncoderDecoder.TryDecodeToCompositeRuleId(e.Uri, out SonarCompositeRuleId compositeRuleId))
            {
                education.ShowRuleHelp(compositeRuleId);
                return;
            }

            browserService.Navigate(e.Uri.AbsoluteUri);
        }
    }
}
