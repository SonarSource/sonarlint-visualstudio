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
using System.IO;
using System.IO.Abstractions;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.CFamily.Analysis
{
    internal interface ICFamilyAnalyzer : IAnalyzer
    {
        void ExecuteAnalysis(string path,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            IAnalysisStatusNotifier statusNotifier,
            CancellationToken cancellationToken);
    }

    [Export(typeof(IAnalyzer))]
    [Export(typeof(ICFamilyAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CLangAnalyzer : ICFamilyAnalyzer
    {
        private readonly ITelemetryManager telemetryManager;
        private readonly ISonarLintSettings settings;
        private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
        private readonly ILogger logger;
        private readonly ICFamilyIssueConverterFactory issueConverterFactory;
        private readonly IRequestFactoryAggregate requestFactory;
        private readonly IFileSystem fileSystem;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public CLangAnalyzer(ITelemetryManager telemetryManager,
            ISonarLintSettings settings,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            ICFamilyIssueConverterFactory issueConverterFactory,
            IRequestFactoryAggregate requestFactory,
            ILogger logger)
            : this(telemetryManager, settings, analysisStatusNotifierFactory, issueConverterFactory, requestFactory, logger, new FileSystem(), ThreadHandling.Instance)
        {
        }

        internal /* for testing */ CLangAnalyzer(ITelemetryManager telemetryManager,
            ISonarLintSettings settings,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            ICFamilyIssueConverterFactory issueConverterFactory,
            IRequestFactoryAggregate requestFactory,
            ILogger logger,
            IFileSystem fileSystem,
            IThreadHandling threadHandling)

        {
            this.telemetryManager = telemetryManager;
            this.settings = settings;
            this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
            this.logger = logger;
            this.issueConverterFactory = issueConverterFactory;
            this.requestFactory = requestFactory;
            this.fileSystem = fileSystem;
            this.threadHandling = threadHandling;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.CFamily);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer, IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken, Guid analysisId = default)
        {
            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(CLangAnalyzer), path);

            ExecuteAnalysis(path, detectedLanguages, consumer, analyzerOptions, analysisStatusNotifier, cancellationToken);
        }

        public void ExecuteAnalysis(string path,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            IAnalysisStatusNotifier statusNotifier,
            CancellationToken cancellationToken) =>
            TriggerAnalysisAsync(path, detectedLanguages, consumer, analyzerOptions, statusNotifier, cancellationToken)
                .Forget(); // fire and forget

        internal /* for testing */ async Task TriggerAnalysisAsync(string path,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            IAnalysisStatusNotifier statusNotifier,
            CancellationToken cancellationToken)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            // Switch to a background thread
            await threadHandling.SwitchToBackgroundThread();

            var request = await TryCreateRequestAsync(path, analyzerOptions);

            if (request != null)
            {
                RunAnalysis(request, consumer, statusNotifier, cancellationToken);
            }
        }

        private async Task<IRequest> TryCreateRequestAsync(string path, IAnalyzerOptions analyzerOptions)
        {
            var cFamilyAnalyzerOptions = analyzerOptions as CFamilyAnalyzerOptions;
            var request = await requestFactory.TryCreateAsync(path, cFamilyAnalyzerOptions);

            if (request == null)
            {
                // Logging for PCH is too noisy: #2553
                if (cFamilyAnalyzerOptions == null || !cFamilyAnalyzerOptions.CreatePreCompiledHeaders)
                {
                    logger.WriteLine(CFamilyStrings.MSG_UnableToCreateConfig, path);
                }
                return null;
            }

            return request;
        }

        protected /* for testing */ virtual void CallSubProcess(Action<Message> handleMessage, IRequest request, ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken)
        {
            ExecuteSubProcess(handleMessage, request, new ProcessRunner(settings, logger), logger, cancellationToken, fileSystem);
        }

        private void RunAnalysis(IRequest request, IIssueConsumer consumer, IAnalysisStatusNotifier statusNotifier, CancellationToken cancellationToken)
        {
            var analysisStartTime = DateTime.Now;
            statusNotifier?.AnalysisStarted();

            var messageHandler = consumer == null
                ? NoOpMessageHandler.Instance
                : new MessageHandler(request, consumer, issueConverterFactory.Create(), logger);

            try
            {
                // We're tying up a background thread waiting for out-of-process analysis. We could
                // change the process runner so it works asynchronously. Alternatively, we could change the
                // RequestAnalysis method to be asynchronous, rather than fire-and-forget.
                CallSubProcess(messageHandler.HandleMessage, request, settings, logger, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    statusNotifier?.AnalysisCancelled();
                }
                else
                {
                    if (messageHandler.AnalysisSucceeded)
                    {
                        var analysisTime = DateTime.Now - analysisStartTime;
                        statusNotifier?.AnalysisFinished(messageHandler.IssueCount, analysisTime);
                    }
                    else
                    {
                        statusNotifier?.AnalysisFailed(CFamilyStrings.MSG_GenericAnalysisFailed);
                    }
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                statusNotifier?.AnalysisFailed(ex);
            }

            telemetryManager.LanguageAnalyzed(request.Context.CFamilyLanguage); // different keys for C and C++
        }

        internal /* for testing */ static void ExecuteSubProcess(Action<Message> handleMessage, IRequest request, IProcessRunner runner, ILogger logger, CancellationToken cancellationToken, IFileSystem fileSystem)
        {
            if (SubProcessFilePaths.AnalyzerExeFilePath == null)
            {
                logger.WriteLine(CFamilyStrings.MSG_UnableToLocateSubProcessExe);
                return;
            }

            var createReproducer = request.Context.AnalyzerOptions?.CreateReproducer ?? false;
            if (createReproducer)
            {
                SaveRequestDiagnostics(request, logger, fileSystem);
            }

            const string communicateViaStreaming = "-"; // signal the subprocess we want to communicate via standard IO streams.

            var args = new ProcessRunnerArguments(SubProcessFilePaths.AnalyzerExeFilePath, false)
            {
                CmdLineArgs = new[] { communicateViaStreaming },
                CancellationToken = cancellationToken,
                WorkingDirectory = SubProcessFilePaths.WorkingDirectory,
                EnvironmentVariables = request.EnvironmentVariables,
                HandleInputStream = writer =>
                {
                    using (var binaryWriter = new BinaryWriter(writer.BaseStream))
                    {
                        request.WriteRequest(binaryWriter);
                    }
                },
                HandleOutputStream = reader =>
                {
                    if (createReproducer)
                    {
                        reader.ReadToEnd();
                        logger.WriteLine(CFamilyStrings.MSG_ReproducerSaved, SubProcessFilePaths.ReproducerFilePath);
                    }
                    else if (request.Context.AnalyzerOptions?.CreatePreCompiledHeaders ?? false)
                    {
                        reader.ReadToEnd();
                        logger.WriteLine(CFamilyStrings.MSG_PchSaved, request.Context.File, request.Context.PchFile);
                    }
                    else
                    {
                        using (var binaryReader = new BinaryReader(reader.BaseStream))
                        {
                            Protocol.Read(binaryReader, handleMessage);
                        }
                    }
                }
            };

            runner.Execute(args);
        }

        private static void SaveRequestDiagnostics(IRequest request, ILogger logger, IFileSystem fileSystem)
        {
            using (var stream = fileSystem.FileStream.Create(SubProcessFilePaths.RequestConfigFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                request.WriteRequestDiagnostics(writer);
            }

            logger.WriteLine(CFamilyStrings.MSG_RequestConfigSaved, SubProcessFilePaths.RequestConfigFilePath);
        }
    }
}
