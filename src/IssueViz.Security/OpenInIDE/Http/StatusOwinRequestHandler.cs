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

using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http
{
    /// <summary>
    /// Handles low-level HTTP request for the IDE status
    /// </summary>
    [Export(typeof(IOwinPathRequestHandler))]
    internal class StatusOwinRequestHandler : IOwinPathRequestHandler
    {
        private readonly IOpenInIDERequestHandler openInIDERequestHandler;

        [ImportingConstructor]
        internal StatusOwinRequestHandler(IOpenInIDERequestHandler openInIDERequestHandler)
        {
            this.openInIDERequestHandler = openInIDERequestHandler;
        }

        public string ApiPath => "/status";

        public async Task ProcessRequest(IOwinContext context)
        {
            var status = await openInIDERequestHandler.GetStatusAsync();
            var data = JsonConvert.SerializeObject(status, Formatting.Indented);
            await context.Response.WriteAsync(data);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
