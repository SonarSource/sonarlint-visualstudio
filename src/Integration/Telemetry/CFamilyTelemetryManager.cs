using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    public interface ICFamilyTelemetryManager : IDisposable
    {
    }

    [Export(typeof(ICFamilyTelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class CFamilyTelemetryManager : ICFamilyTelemetryManager
    {
        private readonly ICFamilyProjectTypeIndicator projectTypeIndicator;
        private readonly ICompilationDatabaseLocator compilationDatabaseLocator;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly ITelemetryDataRepository telemetryDataRepository;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CFamilyTelemetryManager(ICFamilyProjectTypeIndicator projectTypeIndicator,
            ICompilationDatabaseLocator compilationDatabaseLocator,
            IActiveSolutionTracker activeSolutionTracker,
            ITelemetryDataRepository telemetryDataRepository,
            ILogger logger)
        {
            this.projectTypeIndicator = projectTypeIndicator;
            this.compilationDatabaseLocator = compilationDatabaseLocator;
            this.activeSolutionTracker = activeSolutionTracker;
            this.telemetryDataRepository = telemetryDataRepository;
            this.logger = logger;

            activeSolutionTracker.BeforeSolutionClosed += ActiveSolutionTracker_BeforeSolutionClosed;
            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
        }

        private void ActiveSolutionTracker_BeforeSolutionClosed(object sender, EventArgs e)
        {
            // todo: hacky bug fix: for open-as-folder projects, SolutionOpened event is not being raised.
            UpdateTelemetry();
        }

        private void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            if (!e.IsSolutionOpen)
            {
                return;
            }

            UpdateTelemetry();
        }

        private void UpdateTelemetry()
        {
            try
            {
                Debug.Assert(telemetryDataRepository.Data != null);

                var isCMake = projectTypeIndicator.IsCMake();

                if (isCMake)
                {
                    var compilationDatabaseLocation = compilationDatabaseLocator.Locate();
                    var isCMakeAnalyzable = !string.IsNullOrEmpty(compilationDatabaseLocation);

                    if (isCMakeAnalyzable)
                    {
                        telemetryDataRepository.Data.CFamilyProjectTypes.IsCMakeAnalyzable = true;
                    }
                    else
                    {
                        telemetryDataRepository.Data.CFamilyProjectTypes.IsCMakeNonAnalyzable = true;
                    }

                    telemetryDataRepository.Save();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogDebug("[CFamilyTelemetryManager] Failed to calculate cfamily project types: {0}", ex);
            }
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
            activeSolutionTracker.BeforeSolutionClosed -= ActiveSolutionTracker_BeforeSolutionClosed;
        }
    }
}
