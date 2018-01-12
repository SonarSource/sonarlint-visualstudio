/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Net.Http;
using System.Threading;
using SonarQube.Client.Api.Requests;
using SonarQube.Client.Services;

namespace SonarQube.Client.RequestGenerator
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            var sonarQubeService = new SonarQubeService(
                new HttpClientHandler(),
                DefaultConfiguration.Configure(new RequestFactory()));

            var parser = new ArgsParser(args);

            var runner = new ServiceRunner(sonarQubeService)
            {
                OutputPath = parser.NextArg(),
                SonarQubeUrl = new Uri(parser.NextArg()),
                Username = parser.NextArg(),
                Password = parser.NextArg(),
                Project = parser.NextArg(),
                Organization = parser.NextArg(),
                RoslynQualityProfile = parser.NextArg(),
            };

            runner.Run(args, cts.Token)
                .GetAwaiter()
                .GetResult();
        }
    }
}
