/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Helpers
{
    public interface IShowInBrowserService
    {
        void ShowIssue(string issueKey);

        void ShowDocumentation();

        void ShowCommunityPage();
    }

    [Export(typeof(IShowInBrowserService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ShowInBrowserService : IShowInBrowserService
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IBrowserService vsBrowserService;

        [ImportingConstructor]
        public ShowInBrowserService(ISonarQubeService sonarQubeService,
            IConfigurationProvider configurationProvider,
            IBrowserService vsBrowserService)
        {
            this.sonarQubeService = sonarQubeService;
            this.configurationProvider = configurationProvider;
            this.vsBrowserService = vsBrowserService;
        }

        public void ShowIssue(string issueKey)
        {
            if (string.IsNullOrEmpty(issueKey))
            {
                throw new ArgumentNullException(nameof(issueKey));
            }

            var bindingConfiguration = configurationProvider.GetConfiguration();

            if (bindingConfiguration.Equals(BindingConfiguration.Standalone))
            {
                return;
            }

            var projectKey = bindingConfiguration.Project.ProjectKey;
            var viewIssueUrl = sonarQubeService.GetViewIssueUrl(projectKey, issueKey);

            vsBrowserService.Navigate(viewIssueUrl.ToString());
        }

        public void ShowDocumentation()
        {
            vsBrowserService.Navigate(DocumentationLinks.HomePage);
        }

        public void ShowCommunityPage()
        {
            vsBrowserService.Navigate("https://community.sonarsource.com/c/sl/visual-studio/35");
        }
    }
}
