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
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Helpers;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // Register this class as a VS package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(CommonGuids.Package)]
    [ProvideBindingPath]
    // Specify when to load the extension (GUID can be found in Microsoft.VisualStudio.VSConstants.UICONTEXT)
    [ProvideAutoLoad(CommonGuids.PackageActivation)]
    // Register the information needed to show the package in the Help/About dialog of VS.
    // NB: The version is automatically updated by the ChangeVersion.proj
    [InstalledProductRegistration("#110", "#112", "4.0.0.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), "SonarLint", GeneralOptionsDialogPage.PageName, 901, 902, false, 903)]
    [ProvideOptionPage(typeof(OtherOptionsDialogPage), "SonarLint", OtherOptionsDialogPage.PageName, 901, 904, true)]
    [ProvideUIContextRule(CommonGuids.PackageActivation, "SonarLintIntegrationPackageActivation",
         "(HasCSProj | HasVBProj)",
        new string[] { "HasCSProj",
                       "HasVBProj" },
        new string[] { "SolutionHasProjectCapability:CSharp",
                       "SolutionHasProjectCapability:VB" }
    )]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
        "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"",
        Justification = "By-Design. The base class exposes a Dispose override in which the disposable instances will be disposed",
        Scope = "type",
        Target = "~T:SonarLint.VisualStudio.Integration.Vsix.SonarLintIntegrationPackage")]
    public partial class SonarLintIntegrationPackage : Package
    {
        private BoundSolutionAnalyzer usageAnalyzer;
        private PackageCommandManager commandManager;
        private SonarAnalyzerManager sonarAnalyzerManager;
        private DeprecationManager deprecationManager;

        private InMemoryConfigurationProvider inMemoryConfigProvider;
        private const string SonarLintDataKey = "SonarLintBindingData";
        private readonly IFormatter formatter = new BinaryFormatter();
        private ILogger logger;

        public SonarLintIntegrationPackage()
        {
            AddOptionKey(SonarLintDataKey);
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.InitializeSqm();

            IServiceProvider serviceProvider = this;

            var activeSolutionBoundTracker = this.GetMefService<IActiveSolutionBoundTracker>();
            var sonarQubeService = this.GetMefService<ISonarQubeService>();
            var workspace = this.GetMefService<VisualStudioWorkspace>();
            logger = this.GetMefService<ILogger>();
            var vsSolution = serviceProvider.GetService<SVsSolution, IVsSolution>();
            this.sonarAnalyzerManager = new SonarAnalyzerManager(activeSolutionBoundTracker, sonarQubeService, workspace,
                vsSolution, logger);

            this.usageAnalyzer = new BoundSolutionAnalyzer(serviceProvider);

            this.commandManager = new PackageCommandManager(serviceProvider.GetService<IMenuCommandService>());
            this.commandManager.Initialize(serviceProvider.GetMefService<ITeamExplorerController>(),
                serviceProvider.GetMefService<IProjectPropertyManager>());

            this.deprecationManager = new DeprecationManager(this.GetMefService<IInfoBarManager>(), logger);

            this.inMemoryConfigProvider = new InMemoryConfigurationProvider();
        }

        #region .suo serialization

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key == SonarLintDataKey)
            {
                try
                {
                    if (stream.Length > 0)
                    {
                        logger.WriteLine("Binding: reading binding information from the .suo file");

                        var data = formatter.Deserialize(stream);

                        var boundProject = JsonHelper.Deserialize<BoundSonarQubeProject>(data as string);
                        var configuration = BindingConfiguration.CreateBoundConfiguration(boundProject, isLegacy: false);
                        this.inMemoryConfigProvider.WriteConfiguration(configuration);
                    }
                    else
                    {
                        logger.WriteLine("Binding: no binding information found in the .suo file");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLine($"Failed to read binding data from the .suo file: {ex.Message}");
                }
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key == SonarLintDataKey)
            {
                try
                {
                    var currentConfig = inMemoryConfigProvider.GetConfiguration();

                    // We only save the configuration in the .suo file when 
                    // in the new connected mode.
                    // The data is serialized to json first for two reasons:
                    // 1. it means the data is a string so it can be binary-serialized
                    // 2. it gives us more flexibility in the future if the format changes
                    if (currentConfig.Mode == SonarLintMode.Connected)
                    {
                        logger.WriteLine("Binding: writing binding information to the .suo file");
                        var serializable = JsonHelper.Serialize(currentConfig.Project);
                        formatter.Serialize(stream, serializable);
                    }
                    else
                    {
                        logger.WriteLine($"Binding: mode= {currentConfig.Mode.ToString() }. No data will be written to the .suo file");
                    }
                }
                catch(Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine($"Failed to write binding data to the .suo file: {ex.Message}");
                }
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.sonarAnalyzerManager?.Dispose();
                this.sonarAnalyzerManager = null;

                this.usageAnalyzer?.Dispose();
                this.usageAnalyzer = null;

                this.deprecationManager?.Dispose();
                this.deprecationManager = null;
            }
        }
    }
}
