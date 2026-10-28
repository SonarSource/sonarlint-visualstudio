/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Service.Log;

[Export(typeof(ISLCoreLogLevelUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class SLCoreLogLevelUpdater(ISLCoreServiceProvider serviceProvider, IThreadHandling threadHandling) : ISLCoreLogLevelUpdater
{
    public void UpdateLogLevel(DaemonLogLevel daemonLogLevel)
    {
        threadHandling.Run(async () =>
        {
            await threadHandling.SwitchToBackgroundThread();
            if (!serviceProvider.TryGetTransientService(out ILogSLCoreService? logService))
            {
                return 0;
            }

            logService.SetLogLevel(new SetLogLevelParams(SlCoreBackendLogLevel.FromDaemonLogLevel(daemonLogLevel)));
            return 0;
        });
    }
}
