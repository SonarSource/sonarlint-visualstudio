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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;

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
                pathToHandlerMap.Add(handler.ApiPath, handler);
            }
        }

        /// <summary>
        /// Called for each low-level HTTP request received by the underlying HTTPListener
        /// </summary>
        public Task ProcessRequest(IDictionary<string, object> environment)
        {
            //TODO
            return Task.CompletedTask;
        }
    }
}
