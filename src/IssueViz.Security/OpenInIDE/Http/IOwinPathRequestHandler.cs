/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Threading.Tasks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http
{
    /// <summary>
    /// Component that handles a low-level OWIN HTTP request to a specific relative path
    /// </summary>
    internal interface IOwinPathRequestHandler
    {
        /// <summary>
        /// Relative path under the base URL address of http://{host}:{port}/sonarlint/api/
        /// e.g. "status"
        /// </summary>
        string ApiPath { get; }

        Task ProcessRequest(Microsoft.Owin.IOwinContext context);
    }
}
