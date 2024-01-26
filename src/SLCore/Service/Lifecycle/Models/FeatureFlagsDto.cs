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

namespace SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models
{
    public class FeatureFlagsDto
    {
        public bool taintVulnerabilitiesEnabled { get; }
        public bool shouldSynchronizeProjects { get; }
        public bool shouldManageLocalServer { get; }
        public bool enableSecurityHotspots { get; }
        public bool shouldManageServerSentEvents { get; }
        public bool enableDataflowBugDetection { get; }
        public bool shouldManageFullSynchronization { get; }

        public FeatureFlagsDto(bool taintVulnerabilitiesEnabled,
            bool shouldSynchronizeProjects,
            bool shouldManageLocalServer,
            bool enableSecurityHotspots,
            bool shouldManageServerSentEvents,
            bool enableDataflowBugDetection,
            bool shouldManageFullSynchronization)
        {
            this.taintVulnerabilitiesEnabled = taintVulnerabilitiesEnabled;
            this.shouldSynchronizeProjects = shouldSynchronizeProjects;
            this.shouldManageLocalServer = shouldManageLocalServer;
            this.enableSecurityHotspots = enableSecurityHotspots;
            this.shouldManageServerSentEvents = shouldManageServerSentEvents;
            this.enableDataflowBugDetection = enableDataflowBugDetection;
            this.shouldManageFullSynchronization = shouldManageFullSynchronization;
        }
    }
}
