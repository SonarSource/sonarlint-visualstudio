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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using Sentry;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.SLCore.Monitoring;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(IMonitoringService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class MonitoringService : IMonitoringService
{
    internal interface ISentrySdk
    {
        IDisposable PushScope();
        void ConfigureScope(Action<Scope> configureScope);
        IDisposable Init(Action<SentryOptions> options);
        void CaptureException(Exception exception);
        void Close();
    }

    private sealed class SentrySdkAdapter : ISentrySdk
    {
        public IDisposable PushScope() => SentrySdk.PushScope();

        public void ConfigureScope(Action<Scope> configureScope) => SentrySdk.ConfigureScope(configureScope);

        public IDisposable Init(Action<SentryOptions> options) => SentrySdk.Init(options);

        public void CaptureException(Exception exception) => SentrySdk.CaptureException(exception);

        public void Close() => SentrySdk.Close();
    }

    private const string ClientDsn = "https://654099e8f0967df586fda7753c66211e@o1316750.ingest.us.sentry.io/4510622904877056";

    private readonly ISlCoreTelemetryHelper telemetryHelper;
    private readonly IVsInfoProvider vsInfoProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly ISentrySdk sentrySdk;

    private bool active;

    [ImportingConstructor]
    public MonitoringService(
        ISlCoreTelemetryHelper telemetryHelper,
        IVsInfoProvider vsInfoProvider,
        IThreadHandling threadHandling,
        ILogger logger)
        : this(telemetryHelper, vsInfoProvider, threadHandling, logger, new SentrySdkAdapter())
    {
    }

    internal MonitoringService(
        ISlCoreTelemetryHelper telemetryHelper,
        IVsInfoProvider vsInfoProvider,
        IThreadHandling threadHandling,
        ILogger logger,
        ISentrySdk sentrySdk)
    {
        this.telemetryHelper = telemetryHelper;
        this.vsInfoProvider = vsInfoProvider;
        this.threadHandling = threadHandling;
        this.logger = logger;
        this.sentrySdk = sentrySdk;
    }

    public void Init()
    {
        if (active)
        {
            return;
        }

        threadHandling.RunOnBackgroundThread(() =>
        {
            var status = telemetryHelper.GetStatus();
            if (status == SlCoreTelemetryStatus.Enabled)
            {
                InitializeSentry();
            }
            else
            {
                active = false;
            }
        }).Forget();
    }

    public void Close()
    {
        if (!active)
        {
            return;
        }

        active = false;
        sentrySdk.Close();
    }

    public void Reinit()
    {
        if (active)
        {
            return;
        }

        active = true;
        InitializeSentry();
    }

    public void ReportException(Exception exception, string context)
    {
        if (!active)
        {
            return;
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
            logger.LogVerbose($"[MonitoringService] Failed to report exception to Sentry: {ex.Message}");
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
                options.Environment = IsDogfoodingEnvironment() ? "dogfood" : "production";
                options.DefaultTags["ideVersion"] = vsInfoProvider.Version?.DisplayVersion ?? "unknown";
                options.DefaultTags["platform"] = Environment.OSVersion.Platform.ToString();
                options.DefaultTags["architecture"] = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                options.AddInAppInclude("SonarLint.VisualStudio");
            });

            active = true;
            sentrySdk.CaptureException(new Exception("TEST: Sentry capture exception5 (SLVS)"));
        }
        catch (Exception ex)
        {
            logger.LogVerbose($"[MonitoringService] Failed to initialize Sentry: {ex.Message}");
            active = false;
        }
    }

    private static bool IsDogfoodingEnvironment() =>
        "1" == Environment.GetEnvironmentVariable("SONARSOURCE_DOGFOODING");
}
