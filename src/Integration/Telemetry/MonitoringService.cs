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
using Sentry.Protocol;
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
    internal const string ExplicitCaptureTag = "slvs_explicit_capture";

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

            sentrySdk.Close();
        }
    }

    public void Reinit()
    {
        lock (stateLock)
        {
            if (active)
            {
                return;
            }

            InitializeSentry();
        }
    }

    public void ReportException(Exception exception, string context)
    {
        lock (stateLock)
        {
            if (!active)
            {
                return;
            }

            try
            {
                using (sentrySdk.PushScope())
                {
                    sentrySdk.ConfigureScope(scope =>
                    {
                        scope.SetTag("slvs_context", context);
                        scope.SetTag(ExplicitCaptureTag, "true");
                    });
                    sentrySdk.CaptureException(exception);
                }
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"Failed to report exception to Sentry: {ex.Message}");
            }
        }
    }

    private void InitializeSentry()
    {
        lock (stateLock)
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
                    options.SetBeforeSend((@event, _) => FilterSentryEvent(@event));
                });

                active = true;
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"Failed to initialize Sentry: {ex.Message}");

                active = false;
            }
        }
    }

    internal static SentryEvent FilterSentryEvent(SentryEvent @event)
    {
        if (@event == null)
        {
            return null;
        }

        if (@event.Tags.TryGetValue(ExplicitCaptureTag, out var explicitCapture) &&
            string.Equals(explicitCapture, "true", StringComparison.OrdinalIgnoreCase))
        {
            return @event;
        }

        var sentryExceptions = (@event.SentryExceptions ?? Enumerable.Empty<SentryException>()).ToList();

        var isUnhandled = sentryExceptions.Any(x => x.Mechanism?.Handled == false);
        if (!isUnhandled)
        {
            return null;
        }

        var containsSonarFrame = sentryExceptions
            .SelectMany(x => x.Stacktrace?.Frames ?? Array.Empty<SentryStackFrame>())
            .Any(frame =>
                (!string.IsNullOrEmpty(frame.Module) &&
                 (frame.Module.StartsWith("SonarLint", StringComparison.OrdinalIgnoreCase) ||
                  frame.Module.StartsWith("SonarQube", StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(frame.Package) &&
                 (frame.Package.StartsWith("SonarLint", StringComparison.OrdinalIgnoreCase) ||
                  frame.Package.StartsWith("SonarQube", StringComparison.OrdinalIgnoreCase))));

        return containsSonarFrame ? @event : null;
    }

}
