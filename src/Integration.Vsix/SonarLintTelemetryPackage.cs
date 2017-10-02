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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    public sealed class SonarLintTelemetryPackage : Package
    {
        public const string PackageGuidString = "4E057B4B-E2B8-490D-95D8-2A1A4E7ACAED";

        private TelemetryManager telemetryManager;

        protected override void Initialize()
        {
            base.Initialize();

            var activeSolutionTracker = this.GetMefService<IActiveSolutionBoundTracker>();
            Debug.Assert(activeSolutionTracker != null, "Failed to resolve 'IActiveSolutionBoundTracker'.");

            this.telemetryManager = new TelemetryManager(activeSolutionTracker, new TelemetryDataRepository(),
                new TelemetryClient(), new TimerFactory(), new KnownUIContextsWrapper());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            this.telemetryManager?.Dispose();
            this.telemetryManager = null;
        }
    }
}
