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
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    public class SonarQubeIssuesProviderFactory : ISonarQubeIssuesProviderFactory
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;
        private readonly ITimerFactory timerFactory;

        public SonarQubeIssuesProviderFactory(ISonarQubeService sonarQubeService, ILogger logger)
            : this(sonarQubeService, logger, new TimerFactory())
        {
        }

        internal SonarQubeIssuesProviderFactory(ISonarQubeService sonarQubeService, ILogger logger, ITimerFactory timerFactory)
        {
            this.sonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
        }

        public ISonarQubeIssuesProvider Create(BindingConfiguration configuration)
        {
            return new SonarQubeIssuesProvider(sonarQubeService, configuration.Project.ProjectKey, timerFactory, logger);
        }
    }
}
