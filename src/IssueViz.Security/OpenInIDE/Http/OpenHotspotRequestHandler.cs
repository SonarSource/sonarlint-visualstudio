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

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Owin;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http
{
    /// <summary>
    /// Handles low-level HTTP request to open a hotspot
    /// </summary>
    [Export(typeof(IOwinPathRequestHandler))]
    internal class OpenHotspotRequestHandler : IOwinPathRequestHandler
    {
        private readonly IOpenInIDERequestHandler openInIDERequestHandler;
        private readonly ILogger logger;

        [ImportingConstructor]
        internal OpenHotspotRequestHandler(IOpenInIDERequestHandler openInIDERequestHandler, ILogger logger)
        {
            this.openInIDERequestHandler = openInIDERequestHandler ?? throw new ArgumentNullException(nameof(openInIDERequestHandler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ApiPath => "hotspots/show";

        public Task ProcessRequest(IOwinContext context)
        {
            // TODO:
            // * extract and validate the request parameters
            // * pass the request to the API request handler
            throw new NotImplementedException();
        }
    }
}
