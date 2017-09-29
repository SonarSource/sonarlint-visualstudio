/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    ///     This class only purposes is to avoid bringing MEF composition to the SonarQube.Client assembly which
    ///     can be used in contexts where it is not required.
    /// </summary>
    [Export(typeof(ISonarQubeService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MefSonarQubeService : SonarQubeService
    {
        public MefSonarQubeService()
            : base(new SonarQubeClient(new HttpClientHandler(), TimeSpan.FromSeconds(100)))
        {
            // TODO: Remove the following code which only purpose is to enable a quick testing while deciding of the direction
            // to take regarding ensuring the connection is restored on load.
            var sqConnection = Environment.GetEnvironmentVariable("SQ_CONNECTION");
            Debug.Assert(sqConnection != null,  "Please set up the 'SQ_CONNECTION' environment variable.");

            var split = sqConnection.Split(';');
            Debug.Assert(split.Length == 2 || split.Length == 3, $"Invalid connection information: {sqConnection}.");

            var connection = new ConnectionInformation(new Uri(split[0]), split[1], split.Length == 3 ? split[2].ToSecureString() : null);
            ConnectAsync(connection, CancellationToken.None).Wait();
        }
    }
}
