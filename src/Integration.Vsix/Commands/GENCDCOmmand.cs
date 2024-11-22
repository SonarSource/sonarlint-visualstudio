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

using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using ThreadedWaitDialogProgressData = Microsoft.VisualStudio.Shell.ThreadedWaitDialogProgressData;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands
{
    internal class GENCDCOmmand : VsCommandBase
    {
        private readonly IVsSolution solution;
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly IFileConfigProvider fileConfigProvider;
        private readonly ILogger log;
        internal const int Id = 0x1025;

        public GENCDCOmmand(
            IVsSolution solution,
            IProjectSystemHelper projectSystemHelper,
            IFileConfigProvider fileConfigProvider,
            ILogger log)
        {
            this.solution = solution;
            this.projectSystemHelper = projectSystemHelper;
            this.fileConfigProvider = fileConfigProvider;
            this.log = log;
        }

        protected override async void InvokeInternal()
        {
            Dictionary<string, IFileConfig> configs = new Dictionary<string, IFileConfig>();

            List<VCProject> projects = new List<VCProject>();

            var sw = Stopwatch.StartNew();
            // await ThreadHandling.Instance.SwitchToBackgroundThread();
            // DEBUG_ONLY_VsCookbookThreadingChecker.CheckIfRequiresMainThread(() =>
            // {

            // ThreadHandling.Instance.RunOnBackgroundThread(() =>
            // {
            ThreadHelper.JoinableTaskFactory.Run(
                "Creating CD entries...",
                (progress, cancellation) =>
            {
                foreach (var loadedProject in GetLoadedProjects(solution))
                {
                    var project = projectSystemHelper.GetProject(loadedProject as IVsHierarchy);
                    if (project.Object is VCProject vcProject && vcProject.Files is IVCCollection vcFiles)
                    {
                        projects.Add(vcProject);
                    }
                }

                foreach (var vcProject in projects)
                {
                    progress.Report(new ThreadedWaitDialogProgressData($"Progress in seconds {sw.Elapsed.TotalSeconds}"));
                    var vcFiles = vcProject.Files as IVCCollection;
                    foreach (VCFile vcFile in vcFiles)
                    {
                        progress.Report(new ThreadedWaitDialogProgressData($"Progress in seconds {sw.Elapsed.TotalSeconds}"));
                        if (vcFile.ItemType == "ClCompile")
                        {
                            configs[vcFile.FullPath] = fileConfigProvider.Get(vcProject, vcFile, vcFile.FullPath);
                        }
                    }
                }
                sw.Stop();
                log.WriteLine($"[CDGEN] Total {sw.Elapsed.TotalMilliseconds}");
                log.WriteLine($"[CDGEN] Files count: {configs.Count(x => x.Value is not null)}");
                log.WriteLine($"[CDGEN] Failed to generate count: {configs.Count(x => x.Value is null)}");

                var path = @$".\cdgen{Guid.NewGuid()}.json";
                var fullPath = Path.GetFullPath(path);
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
                log.WriteLine($"[CDGEN] Generated file: {fullPath}");

                return Task.CompletedTask;
            });



            // });



        }

        private IEnumerable<IVsProject> GetLoadedProjects(IVsSolution solution)
        {
            var guid = Guid.Empty;
            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out var enumerator);
            var hierarchy = new IVsHierarchy[1] { null };
            for (enumerator.Reset();
                 enumerator.Next(1, hierarchy, out var fetched) == VSConstants.S_OK && fetched == 1; /*nothing*/)
            {
                yield return (IVsProject)hierarchy[0];
            }
        }
    }
}
