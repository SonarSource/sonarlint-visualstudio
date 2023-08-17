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

using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core;
using EducationResx = SonarLint.VisualStudio.Education.Resources;

namespace SonarLint.VisualStudio.Education.Commands
{
    public sealed partial class RuleHelpUserControl : UserControl
    {
        private readonly IBrowserService browserService;
        private readonly IEducation education;
        private readonly ILogger logger;

        internal RuleHelpUserControl(IBrowserService browserService, IEducation education, ILogger logger)
        {
            this.browserService = browserService;
            this.education = education;
            this.logger = logger;

            InitializeComponent();
        }

        public void HandleRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                if (!e.Uri.IsAbsoluteUri)
                {
                    logger.LogVerbose(EducationResx.RuleHelpUserControl_Verbose_RelativeURI, e.Uri);
                    logger.WriteLine(EducationResx.RuleHelpUserControl_RelativeURI, e.Uri);

                    return;
                }

                // If the incoming URI can be decoded it means that the incoming URI is a cross reference rule
                // in which case it needs to be handed over to the education service.
                if (SonarRuleIdUriEncoderDecoder.TryDecodeToCompositeRuleId(e.Uri, out SonarCompositeRuleId compositeRuleId))
                {
                    education.ShowRuleHelp(compositeRuleId, null);

                    return;
                }

                browserService.Navigate(e.Uri.AbsoluteUri);
            }
            catch (Exception ex) when (!Core.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(EducationResx.ERR_RuleHelpUserControl_Exception, ex));
            }
        }
    }
}
