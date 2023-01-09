﻿/*
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Host.HttpListener;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http
{
    internal interface IHttpListenerFactory
    {
        /// <summary>
        /// Attempts to create and return a new HTTP listener listening
        /// on the first available port in the specified range.
        /// Returns null if there is not a free port in the range.
        /// </summary>
        IDisposable Create(int startPort, int endPort);
    }

    [Export(typeof(IHttpListenerFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class HttpListenerFactory : IHttpListenerFactory
    {
        private readonly IOwinPipelineProcessor requestProcessor;
        private readonly ILogger logger;

        [ImportingConstructor]
        public HttpListenerFactory(IOwinPipelineProcessor requestProcessor, ILogger logger)
        {
            this.requestProcessor = requestProcessor ?? throw new ArgumentNullException(nameof(requestProcessor));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        IDisposable IHttpListenerFactory.Create(int startPort, int endPort)
        {
            logger.WriteLine(OpenInIDEResources.Factory_CreatingListener);
            IDisposable listener = null;

            for (int port = startPort; port <= endPort; port++)
            {
                logger.WriteLine(OpenInIDEResources.Factory_CheckingPort, port);
                try
                {
                    listener = CreateListener(port);
                    logger.WriteLine(OpenInIDEResources.Factory_Succeeded, port);
                    break;
                }
                catch(Exception ex)  when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(OpenInIDEResources.Factory_PortIsUnavailable, port);
                    listener = null;
                }
            }

            if (listener == null)
            {
                logger.WriteLine(OpenInIDEResources.Factory_Failed_NoAvailablePorts);
            }
            return listener;
        }

        private IDisposable CreateListener(int port)
        {
            var appProperties = new AppProperties(new Dictionary<string, object>())
            {
                Addresses = AddressCollection.Create()
            };

            var address = Address.Create();
            address.Scheme = "http";
            address.Host = "localhost";
            address.Port = port.ToString();
            address.Path = "/sonarlint/api/";
            appProperties.Addresses.Add(address);

            // Create a new Owin HTTPListener that forwards all requests to our processor for handling.
            return OwinServerFactory.Create(requestProcessor.ProcessRequest, appProperties.Dictionary);
        }
    }
}
