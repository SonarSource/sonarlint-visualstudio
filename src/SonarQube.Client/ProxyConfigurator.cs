﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Net;
using System.Net.Http;
using SonarQube.Client.Logging;

namespace SonarQube.Client;

internal interface IProxyConfigurator
{
    void ConfigureProxy(HttpClientHandler httpClientHandler, Uri baseAddress);
}

internal class ProxyConfigurator(ISystemProxyProvider proxyProvider, ILogger logger) : IProxyConfigurator
{
    public void ConfigureProxy(HttpClientHandler httpClientHandler, Uri baseAddress)
    {
        var proxyUri = proxyProvider.GetProxyUri(baseAddress);
        if (baseAddress != proxyUri)
        {
            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = new WebProxy(proxyUri);
            logger.Debug($"System proxy detected and configured: {proxyUri}");
        }
        else
        {
            logger.Debug("No system proxy detected");
        }
    }
}
