/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.SLCore.Monitoring;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(IMonitoringService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class MonitoringService(
    ISlCoreTelemetryHelper telemetryHelper,
    IVsInfoProvider vsInfoProvider,
    IThreadHandling threadHandling,
    ILogger logger,
    ISentrySdk sentrySdk,
    IDogfoodingService dogfoodingService) : IMonitoringService
{
    private const string ClientDsn = "https://654099e8f0967df586fda7753c66211e@o1316750.ingest.us.sentry.io/4510622904877056";

    private readonly ILogger logger = logger.ForVerboseContext(nameof(MonitoringService));
    private readonly object stateLock = new();

    private bool active;

    public void Init()
    {
        lock (stateLock)
        {
            if (active)
            {
                return;
            }
        }

        threadHandling.RunOnBackgroundThread(() =>
        {
            var status = telemetryHelper.GetStatus();
            if (status == SlCoreTelemetryStatus.Enabled)
            {
                InitializeSentry();
            }
        }).Forget();
    }

    public void Close()
    {
        lock (stateLock)
        {
            if (!active)
            {
                return;
            }

            active = false;
        }

        sentrySdk.Close();
    }

    public void Reinit()
    {
        lock (stateLock)
        {
            if (active)
            {
                return;
            }
        }

        InitializeSentry();
    }

    public void ReportException(Exception exception, string context)
    {
        lock (stateLock)
        {
            if (!active)
            {
                return;
            }
        }

        try
        {
            using (sentrySdk.PushScope())
            {
                sentrySdk.ConfigureScope(scope => scope.SetTag("slvs_context", context));
                sentrySdk.CaptureException(exception);
            }
        }
        catch (Exception ex)
        {
            logger.LogVerbose($"Failed to report exception to Sentry: {ex.Message}");
        }
    }

    private void InitializeSentry()
    {
        try
        {
            sentrySdk.Init(options =>
            {
                options.Dsn = ClientDsn;
                options.Release = VersionHelper.SonarLintVersion;
                options.Environment = dogfoodingService.IsDogfoodingEnvironment ? "dogfood" : "production";
                options.DefaultTags["ideVersion"] = vsInfoProvider.Version?.DisplayVersion ?? "unknown";
                options.DefaultTags["platform"] = Environment.OSVersion.Platform.ToString();
                options.DefaultTags["architecture"] = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                options.AddInAppInclude("SonarLint.VisualStudio");
            });

            lock (stateLock)
            {
                active = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogVerbose($"Failed to initialize Sentry: {ex.Message}");

            lock (stateLock)
            {
                active = false;
            }
        }
    }

}
