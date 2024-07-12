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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(ISlCoreTelemetryHelper))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SlCoreTelemetryHelper : ISlCoreTelemetryHelper
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;

    [ImportingConstructor]
    public SlCoreTelemetryHelper(ISLCoreServiceProvider serviceProvider, IThreadHandling threadHandling, ILogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public SlCoreTelemetryStatus GetStatus()
    {
        return threadHandling.Run(async () =>
        {
            var result = SlCoreTelemetryStatus.Unavailable;
            try
            {
                await threadHandling.SwitchToBackgroundThread();
                if (serviceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetryService))
                {
                    result = (await telemetryService.GetStatusAsync()).enabled ? SlCoreTelemetryStatus.Enabled : SlCoreTelemetryStatus.Disabled;
                }

            }
            catch (Exception e) when(!ErrorHandler.IsCriticalException(e))
            {
                logger.WriteLine(e.ToString());
            }
                
            return result;
        });
    }
    
    public void Notify(Action<ITelemetrySLCoreService> telemetryProducer)
    {
        threadHandling
            .RunOnBackgroundThread(() =>
            {
                if (!serviceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetryService))
                {
                    logger.LogVerbose("SLCore service is not available, telemetry is discarded");
                    return;
                }

                telemetryProducer(telemetryService);
            })
            .Forget();
    }
}
