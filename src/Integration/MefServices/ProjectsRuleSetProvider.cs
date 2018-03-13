/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CodeAnalysis.Extensibility;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    public interface IProjectsRuleSetProvider
    {
        bool HasRuleSetWithSonarAnalyzerRules(string projectFilePath);
    }

    [Export(typeof(IProjectsRuleSetProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ProjectsRuleSetProvider : IProjectsRuleSetProvider, IDisposable
    {
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IVsFileChangeEx fileChangeService;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ILogger logger;

        internal /* for testing purposes */ readonly Dictionary<string, ProjectData> projectPathToCachedData =
            new Dictionary<string, ProjectData>();
        private readonly Dictionary<string, FileChangeTracker> filesToTrack = new Dictionary<string, FileChangeTracker>();

        private bool isDisposed;

        [ImportingConstructor]
        public ProjectsRuleSetProvider(IHost host)
            : this(host.GetService<IProjectSystemHelper>(), host.GetService<SVsFileChangeEx, IVsFileChangeEx>(),
                  host.GetMefService<IActiveSolutionTracker>(), InMemoryConfigurationProvider.Instance, host.Logger)
        {
        }

        internal ProjectsRuleSetProvider(IProjectSystemHelper projectSystemHelper, IVsFileChangeEx fileChangeService,
            IActiveSolutionTracker activeSolutionTracker, IConfigurationProvider configurationProvider, ILogger logger)
        {
            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (fileChangeService == null)
            {
                throw new ArgumentNullException(nameof(fileChangeService));
            }

            if (activeSolutionTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionTracker));
            }

            if (configurationProvider == null)
            {
                throw new ArgumentNullException(nameof(configurationProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.projectSystemHelper = projectSystemHelper;
            this.fileChangeService = fileChangeService;
            this.activeSolutionTracker = activeSolutionTracker;
            this.configurationProvider = configurationProvider;
            this.logger = logger;

            this.activeSolutionTracker.ActiveSolutionChanged += OnActiveSolutionChanged;
            this.activeSolutionTracker.AfterProjectOpened += OnAfterProjectOpened;

            BuildCacheAsync();
        }

        public bool HasRuleSetWithSonarAnalyzerRules(string projectFilePath) =>
            !string.IsNullOrWhiteSpace(projectFilePath) &&
            projectPathToCachedData.ContainsKey(projectFilePath) &&
            projectPathToCachedData[projectFilePath].HasAnySonarRule;

        private async void OnAfterProjectOpened(object sender, ProjectOpenedEventArgs e)
        {
            try
            {
                if (this.projectSystemHelper.IsSolutionFullyOpened())
                {
                    // Haven't been able to find an event for a project being added to the solution but this one does get called after
                    // the added project is opened. This if is trying to detect if that's a project being added or not.
                    var project = this.projectSystemHelper.GetProject(e.ProjectHierarchy);

                    if (project != null &&
                        !string.IsNullOrEmpty(project.FullName) &&
                        !projectPathToCachedData.ContainsKey(project.FullName))
                    {
                        // Improve me: there is no need to rebuild the full cache here
                        ClearCache();
                        await BuildCacheAsync();
                    }
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Swallow the exception because we are on UI thread
                this.logger.WriteLine($"RuleSetProvider: error in {nameof(OnAfterProjectOpened)}, message: {ex.Message}");
            }
        }

        private async void OnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            try
            {
                ClearCache();

                if (e.IsSolutionOpen)
                {
                    await BuildCacheAsync();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Swallow the exception because we are on UI thread
                this.logger.WriteLine($"RuleSetProvider: error in {nameof(OnActiveSolutionChanged)}, message: {ex.Message}");
            }
        }

        private void ClearCache()
        {
            filesToTrack.Values.ToList().ForEach(x => x.UpdatedOnDisk -= OnUpdatedOnDisk);
            filesToTrack.Clear();

            projectPathToCachedData.Clear();
        }

        private async Task BuildCacheAsync()
        {
            while (!projectSystemHelper.IsSolutionFullyOpened())
            {
                await Task.Delay(2000); // REVIEW: Is 2s a good enough time?
            }

            foreach (var project in projectSystemHelper.GetSolutionProjects())
            {
                // This call has side effects (i.e. modify the field with the list of tracked files)
                var projectData = ProcessProjectAndCollectFilesToTrack(project);
                Debug.Assert(!projectPathToCachedData.ContainsKey(project.FullName),
                    "Not expecting the cache to already contain this project");
                projectPathToCachedData[project.FullName] = projectData;
            }

            if (this.configurationProvider.GetConfiguration().Mode == SonarLintMode.Connected)
            {
                var projectsWithSonarRuleSet = projectPathToCachedData.Where(x => x.Value.HasAnySonarRule)
                    .Select(x => Path.GetFileNameWithoutExtension(x.Key));
                if (projectsWithSonarRuleSet.Any())
                {
                    this.logger.WriteLine(Strings.WarningProjectsWithSonarRulesWhileNewConnected,
                        string.Join(", ", projectsWithSonarRuleSet));
                }
            }
        }

        private async void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            var fileChangeTracker = sender as FileChangeTracker;
            if (fileChangeTracker == null)
            {
                return;
            }

            if (!fileChangeTracker.FilePath.EndsWith(".ruleset"))
            {
                var cachedProjectRuleSetPath = projectPathToCachedData[fileChangeTracker.FilePath].RuleSetPath;

                var project = projectSystemHelper.GetSolutionProjects().FirstOrDefault(p =>
                    p.FullName.Equals(fileChangeTracker.FilePath, StringComparison.OrdinalIgnoreCase));
                if (project != null)
                {
                    // TODO: We only try to access the global CodeAnalysisRuleSet not a configuration specific. When we will handle
                    // this we should be careful to subscribe to configuration change in the IDE to re-build the cache.
                    // Note that handling the configuration specific will still not handle all cases where we could see/find a
                    // ruleset (for example we could have a condition on some file existing...)
                    var projectRuleSetPath = projectSystemHelper.GetProjectProperty(project, Constants.CodeAnalysisRuleSetPropertyKey);
                    var projectDirectoryFullPath = new FileInfo(project.FullName).Directory.FullName;
                    var projectRuleSetFullPath = GetFullPath(projectRuleSetPath, projectDirectoryFullPath);

                    if (cachedProjectRuleSetPath.Equals(projectRuleSetFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Project changed but the ruleset property is the same so there is no need to recreate the cache
                        return;
                    }
                }
                else
                {
                    // Cannot find the project (most likely project was changed while being unloaded) so let's rebuild the cache
                    // in case the CodeAnalysisRuleSet property was changed
                }
            }

            ClearCache();
            await BuildCacheAsync();
        }

        /// <remarks>
        /// [SIDE EFFECT] The set of ruleset is update at each call.
        /// </summaryremarks>
        private ProjectData ProcessProjectAndCollectFilesToTrack(EnvDTE.Project project)
        {
            TrackIfNotTracked(project.FullName);

            var projectRuleSetPath = projectSystemHelper.GetProjectProperty(project, Constants.CodeAnalysisRuleSetPropertyKey);
            if (projectRuleSetPath == null)
            {
                return new ProjectData(null, false);
            }

            var hasAnySonarRule = false;
            var projectDirectoryFullPath = new FileInfo(project.FullName).Directory.FullName;
            var projectRuleSetFullPath = GetFullPath(projectRuleSetPath, projectDirectoryFullPath);

            if (File.Exists(projectRuleSetFullPath))
            {
                var projectRuleSet = RuleSet.LoadFromFile(projectRuleSetFullPath);

                // 1. Collect all paths (current ruleset + includes)
                var ruleSetIncludeFullPaths = projectRuleSet.RuleSetIncludes
                    .Select(include => GetFullPath(include.FilePath, projectDirectoryFullPath))
                    .ToList();

                TrackIfNotTracked(projectRuleSetFullPath);
                ruleSetIncludeFullPaths.ForEach(path => TrackIfNotTracked(path));

                // 2. Look if any of the effective rules is from SonarAnalyzer and initialize dictionary with this result.
                hasAnySonarRule = projectRuleSet
                    .GetEffectiveRules(ruleSetIncludeFullPaths, new RuleInfoProvider[0])
                    .Any(rule => rule.AnalyzerId.StartsWith("SonarAnalyzer.", StringComparison.OrdinalIgnoreCase));
            }

            return new ProjectData(projectRuleSetPath, hasAnySonarRule);
        }

        private void TrackIfNotTracked(string filePath)
        {
            if (filesToTrack.ContainsKey(filePath))
            {
                return;
            }

            var fileChangeTracker = new FileChangeTracker(fileChangeService, filePath);
            fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;
            filesToTrack.Add(filePath, fileChangeTracker);
        }

        private static string GetFullPath(string maybeRelativePath, string relativeTo) =>
            Path.IsPathRooted(maybeRelativePath)
                ? maybeRelativePath
                : Path.GetFullPath(Path.Combine(relativeTo, maybeRelativePath));

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            this.activeSolutionTracker.ActiveSolutionChanged -= OnActiveSolutionChanged;
            this.activeSolutionTracker.AfterProjectOpened -= OnAfterProjectOpened;
            ClearCache();
            isDisposed = true;
        }

        private sealed class FileChangeTracker : IVsFileChangeEvents, IDisposable
        {
            private const uint FileChangeFlags = (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size
                | _VSFILECHANGEFLAGS.VSFILECHG_Del);

            private readonly IVsFileChangeEx fileChangeService;
            private readonly object lockThis = new object();

            private int subscriptionCount;
            private bool isDisposed;
            private EventHandler updatedOnDisk;
            private uint cookie;

            public event EventHandler UpdatedOnDisk
            {
                add
                {
                    lock (lockThis)
                    {
                        updatedOnDisk += value;
                    }
                    subscriptionCount++;

                    if (subscriptionCount == 1)
                    {
                        fileChangeService.AdviseFileChange(FilePath, FileChangeFlags, this, out cookie);
                    }
                }
                remove
                {
                    lock (lockThis)
                    {
                        updatedOnDisk -= value;
                    }
                    subscriptionCount--;

                    if (subscriptionCount == 0)
                    {
                        fileChangeService.UnadviseFileChange(cookie);
                    }
                }
            }

            public FileChangeTracker(IVsFileChangeEx fileChangeService, string filePath)
            {
                this.fileChangeService = fileChangeService;
                FilePath = filePath;
            }

            public string FilePath { get; }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                if (cookie != 0 && subscriptionCount != 0)
                {
                    fileChangeService.UnadviseFileChange(cookie);
                }

                isDisposed = true;
            }

            int IVsFileChangeEvents.DirectoryChanged(string pszDirectory)
            {
                Debug.Fail("We only watch files; we should never be seeing directory changes!");
                return VSConstants.S_OK;
            }

            int IVsFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                updatedOnDisk?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }

        internal /* for testing purposes */ class ProjectData
        {
            public ProjectData(string ruleSetPath, bool hasAnySonarRule)
            {
                RuleSetPath = ruleSetPath;
                HasAnySonarRule = hasAnySonarRule;
            }

            /// <summary>
            ///  The ruleset path as defined in the project (csproj, vbproj...)
            /// </summary>
            public string RuleSetPath { get; }
            public bool HasAnySonarRule { get; }
        }
    }
}
