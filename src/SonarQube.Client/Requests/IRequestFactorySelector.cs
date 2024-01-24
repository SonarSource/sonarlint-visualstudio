/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarQube.Client.Api;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Requests
{
    internal interface IRequestFactorySelector
    {
        /// <summary>
        /// Returns the request factory for the specified type of server
        /// </summary>
        IRequestFactory Select(bool isSonarCloud, ILogger logger);
    }

    internal class RequestFactorySelector : IRequestFactorySelector
    {
        public IRequestFactory Select(bool isSonarCloud, ILogger logger)
        {
            if (isSonarCloud)
            {
                logger.Debug("Selected SonarCloud request factory");
                return DefaultConfiguration.ConfigureSonarCloud(new UnversionedRequestFactory(logger));
            }

            logger.Debug("Selected SonarQube request factory");
            return DefaultConfiguration.ConfigureSonarQube(new RequestFactory(logger));
        }
    }
}
