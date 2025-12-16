/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.IssueVisualization.Helpers;

public static class IssueUrlHelper
{
    private const string ViewIssueRelativeUrl = "project/issues?id={0}&issues={1}&open={1}";

    public static Uri GetViewIssueUrl(BoundServerProject boundServerProject, string issueKey)
    {
        if (boundServerProject == null)
        {
            throw new ArgumentNullException(nameof(boundServerProject));
        }
        if (issueKey == null)
        {
            throw new ArgumentNullException(nameof(issueKey));
        }

        return new Uri(boundServerProject.ServerConnection.ServerUri, string.Format(ViewIssueRelativeUrl, boundServerProject.ServerProjectKey, issueKey));
    }
}
