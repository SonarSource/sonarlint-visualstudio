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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http
{
    /// <summary>
    /// Component that can process requests in an Owin pipeline
    /// </summary>
    internal interface IOwinPipelineProcessor
    {
        Task ProcessRequest(IDictionary<string, object> environment);
    }

    [Export(typeof(IOwinPipelineProcessor))]
    internal class OwinPipelineProcessor : IOwinPipelineProcessor
    {
        private readonly IDictionary<string, IOwinPathRequestHandler> pathToHandlerMap;
        private readonly ILogger logger;

        internal /* for testing */ IReadOnlyDictionary<string, IOwinPathRequestHandler> PathToHandlerMap => (IReadOnlyDictionary<string, IOwinPathRequestHandler>)pathToHandlerMap;

        [ImportingConstructor]
        public OwinPipelineProcessor([ImportMany]IEnumerable<IOwinPathRequestHandler> pathRequestHandlers, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            pathToHandlerMap = new Dictionary<string, IOwinPathRequestHandler>();

            foreach(var handler in pathRequestHandlers)
            {
                Debug.Assert(handler.ApiPath.StartsWith("/"), "Expecting the handler path to start with /");
                pathToHandlerMap.Add(handler.ApiPath, handler);
            }
        }

        /// <summary>
        /// Called for each low-level HTTP request received by the underlying HTTPListener
        /// </summary>
        public async Task ProcessRequest(IDictionary<string, object> environment)
        {
            try
            {
                var context = new OwinContext(environment);
                context.Response.Headers["Access-Control-Allow-Origin"] = context.Request.Headers["Origin"];
                if (pathToHandlerMap.TryGetValue(context.Request.Path.Value, out var handler))
                {
                    logger.WriteLine(OpenInIDEResources.Pipeline_HandlingRequest, context.Request.Path.Value);
                    await handler.ProcessRequest(context);
                }
                else
                {
                    logger.WriteLine(OpenInIDEResources.Pipeline_UnrecognizedRequest, context.Request.Path.Value);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch(Exception ex)
            {
                // Log then rethrow. We expect the handler pipeline to convert the error into an HTML 500 response
                logger.WriteLine(OpenInIDEResources.Pipeline_UnhandledError, ex.Message);
                logger.LogDebug(string.Format(OpenInIDEResources.Pipeline_UnhandledError_Detailed, ex));
                throw;
            }
        }
    }
}
