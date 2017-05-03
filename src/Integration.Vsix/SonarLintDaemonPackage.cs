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

using Grpc.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sonarlint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SonarLintDaemonPackage : Package
    {
        private static readonly string DAEMON_HOST = "localhost";
        private static readonly int DAEMON_PORT = 8050;

        public const string PackageGuidString = "6f63ab5a-5ab8-4a0d-9914-151911885966";

        internal static SonarLintDaemonPackage Instance { get; private set; }

        private string WorkDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarLintDaemonPackage"/> class.
        /// </summary>
        public SonarLintDaemonPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            InitializeSonarLintDaemonFacade();

            Instance = this;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Directory.Delete(WorkDir, true);
            }
        }

        #endregion

        private void InitializeSonarLintDaemonFacade()
        {
            WorkDir = CreateTempDir();
        }

        private string CreateTempDir()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "SonarLintDaemon", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void RequestAnalysis(string path, string charset)
        {
            Analyze(path, charset);
        }

        private async void Analyze(string path, string charset)
        {
            var channel = new Channel(string.Join(":", DAEMON_HOST, DAEMON_PORT), ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);

            var inputFile = new InputFile
            {
                Path = path,
                Charset = charset,
            };

            var request = new AnalysisReq
            {
                BaseDir = path,
                WorkDir = WorkDir,
            };
            request.File.Add(inputFile);

            try
            {
                using (var call = client.Analyze(request))
                {
                    await ProcessIssues(call, path);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Call to .Analyze failed: {0}", e);
            }

            await channel.ShutdownAsync();
        }

        private async System.Threading.Tasks.Task ProcessIssues(AsyncServerStreamingCall<Issue> call, string path)
        {
            var issues = new List<Issue>();

            while (await call.ResponseStream.MoveNext())
            {
                var issue = call.ResponseStream.Current;
                issues.Add(issue);
            }

            TaggerProvider.Instance.UpdateIssues(path, issues);
        }
    }
}
