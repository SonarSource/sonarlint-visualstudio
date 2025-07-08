using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using Document = Microsoft.CodeAnalysis.Document;
using Language = SonarLint.VisualStudio.Core.Language;
using Path = System.IO.Path;

namespace SonarLint.VisualStudio.ConnectedMode
{
    public interface IRoslynAnalyzerRunner
    {
        Task<IEnumerable<DiagnosticDto>> AnalyzeFileAsync(string sourceFilePath);
    }

    [Export(typeof(IRoslynAnalyzerRunner))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RoslynAnalyzerRunner : IRoslynAnalyzerRunner
    {
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ISonarLintConfigGenerator roslynConfigGenerator;
        private readonly VisualStudioWorkspace visualStudioWorkspace;
        private readonly IEnumerable<CodeFixProvider> codeFixProviders;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RoslynAnalyzerRunner(
            IUserSettingsProvider userSettingsProvider,
            ISonarLintConfigGenerator roslynConfigGenerator,
            VisualStudioWorkspace vsWorkspace,
            [ImportMany] IEnumerable<CodeFixProvider> codeFixProviders,
            ILogger logger)
        {
            this.userSettingsProvider = userSettingsProvider;
            this.roslynConfigGenerator = roslynConfigGenerator;
            visualStudioWorkspace = vsWorkspace;
            this.codeFixProviders = codeFixProviders;
            this.logger = logger.ForVerboseContext(nameof(RoslynAnalyzerRunner));
        }

        public async Task<IEnumerable<DiagnosticDto>> AnalyzeFileAsync(string sourceFilePath)
        {
            var analyzerAssemblyPath
                = @"C:\USERS\GABRIELA.TRUTAN\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\17.0_108024F8EXP\EXTENSIONS\SONARSOURCE\SONARQUBE FOR VISUAL STUDIO 2022\8.22.0.0\EmbeddedDotnetAnalyzerDLLs\SonarAnalyzer.CSharp.dll";
            var analyzerAssembly = Assembly.LoadFrom(analyzerAssemblyPath);

            var analyzerTypes = analyzerAssembly.GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract);

            var analyzers = analyzerTypes.Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)).ToImmutableArray();

            var normalziedPath = sourceFilePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            // Get the project for references
            var documentIds = visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(normalziedPath);
            Document? document = null;
            Project project = null;
            if (documentIds.Any())
            {
                document = visualStudioWorkspace.CurrentSolution.GetDocument(documentIds.First());
                project = document?.Project;
            }
            if (project == null)
            {
                return [];
            }

            // Parse the file into a SyntaxTree
            var code = File.ReadAllText(sourceFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Use the project's references
            var references = project.MetadataReferences;

            // Create a compilation with only this file
            var compilation = CSharpCompilation.Create(
                project.AssemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var exclusions = ConvertExclusions(userSettingsProvider.UserSettings);
            var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(userSettingsProvider.UserSettings);

            var sonarLintConfig = roslynConfigGenerator.Generate(ruleParametersByLanguage[Language.CSharp], userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties, exclusions,
                Language.CSharp);
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(
                GetWithSonarLintAdditionalFiles(project.AnalyzerOptions, sonarLintConfig),
                onAnalyzerException: OnAnalyzerException,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: false));
            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            return diagnostics.Select(x => GetDiagnosticDto(document, x));
        }

        private static DiagnosticDto GetDiagnosticDto(Document document, Diagnostic diagnostic)
        {
            var startLine = diagnostic.Location.GetLineSpan().Span.Start.Line;
            var endLine = diagnostic.Location.GetLineSpan().Span.End.Line;
            var lineSpan = diagnostic.Location.GetLineSpan();
            int startColumn = lineSpan.Span.Start.Character;
            int endColumn = lineSpan.Span.End.Character;

            // TODO quickfixes and additional locations are not provided in the PoC
            return new DiagnosticDto(document.FilePath, startLine, startColumn, endLine, endColumn, diagnostic.GetMessage(), [], diagnostic.Id, []);
        }

        public static async Task<IList<CodeAction>> GetQuickFixesAsync(Document document, Diagnostic diagnostic, IEnumerable<CodeFixProvider> codeFixProviders)
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, _) => actions.Add(action),
                CancellationToken.None);

            foreach (var provider in codeFixProviders)
            {
                if (provider.FixableDiagnosticIds.Contains(diagnostic.Id))
                {
                    await provider.RegisterCodeFixesAsync(context);
                }
            }

            return actions;
        }

        private static void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3)
        {
            // to log
        }

        public static AdditionalText Convert(SonarLintConfiguration sonarLintConfiguration)
        {
            var sonarLintXmlFileContent = SonarLintConfigurationSerializer.Serialize(sonarLintConfiguration);
            var sonarLintXmlAdditionalText = new AdditionalTextImpl(DummySonarLintXmlFilePath, sonarLintXmlFileContent);

            return sonarLintXmlAdditionalText;
        }

        /// <summary>
        /// Add sonar-dotnet analyzer additional files.
        /// Override any existing sonar-dotnet analyzer additional files that were already in the project.
        /// </summary>
        private static AnalyzerOptions GetWithSonarLintAdditionalFiles(AnalyzerOptions workspaceAnalyzerOptions, SonarLintConfiguration sonarLintConfiguration)
        {
            var sonarLintAdditionalFile = Convert(sonarLintConfiguration);
            var sonarLintAdditionalFileName = Path.GetFileName(sonarLintAdditionalFile.Path);

            var additionalFiles = workspaceAnalyzerOptions.AdditionalFiles;
            var builder = ImmutableArray.CreateBuilder<AdditionalText>();
            builder.AddRange(additionalFiles.Where(x => !IsSonarLintAdditionalFile(x)));
            builder.Add(sonarLintAdditionalFile);

            var modifiedAdditionalFiles = builder.ToImmutable();
            var finalAnalyzerOptions = new AnalyzerOptions(modifiedAdditionalFiles, workspaceAnalyzerOptions.AnalyzerConfigOptionsProvider);

            return finalAnalyzerOptions;

            bool IsSonarLintAdditionalFile(AdditionalText existingAdditionalFile)
            {
                return Path.GetFileName(existingAdditionalFile.Path).Equals(
                    sonarLintAdditionalFileName,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The file will never be written to disk so the path is irrelevant.
        /// It only needs to be named 'SonarLint.Xml' so the sonar-dotnet analyzers could load it.
        /// </summary>
        private static readonly string DummySonarLintXmlFilePath = Path.Combine(Path.GetTempPath(), "SonarLint.xml");

        // There isn't a public implementation of source text so we need to create one
        internal class AdditionalTextImpl : AdditionalText
        {
            private readonly SourceText sourceText;

            public AdditionalTextImpl(string path, string content)
            {
                Path = path;
                sourceText = SourceText.From(content);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => sourceText;
        }

        internal static class SonarLintConfigurationSerializer
        {
            public static string Serialize(SonarLintConfiguration sonarLintConfiguration)
            {
                var settings = new XmlWriterSettings
                {
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Document,
                    Indent = true,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates,
                    OmitXmlDeclaration = false,
                    Encoding = new UTF8Encoding(false) // to avoid generating unicode BOM 
                };

                using (var stream = new MemoryStream { Position = 0 })
                using (var xmlWriter = XmlWriter.Create(stream, settings))
                {
                    var serializer = new XmlSerializer(typeof(SonarLintConfiguration));
                    serializer.Serialize(xmlWriter, sonarLintConfiguration);
                    xmlWriter.Flush();

                    var data = stream.ToArray();
                    var sonarLintXmlFileContent = Encoding.UTF8.GetString(data);

                    return sonarLintXmlFileContent;
                }
            }
        }

        private static StandaloneRoslynFileExclusions ConvertExclusions(UserSettings settings)
        {
            var exclusions = new StandaloneRoslynFileExclusions(settings.AnalysisSettings);
            return exclusions;
        }

        private static (Dictionary<Language, List<IRoslynRuleStatus>>, Dictionary<Language, List<IRuleParameters>>) ConvertRules(UserSettings settings)
        {
            var ruleStatusesByLanguage = InitializeForAllRoslynLanguages<IRoslynRuleStatus>();
            var ruleParametersByLanguage = InitializeForAllRoslynLanguages<IRuleParameters>();
            foreach (var analysisSettingsRule in settings.AnalysisSettings.Rules)
            {
                if (!SonarCompositeRuleId.TryParse(analysisSettingsRule.Key, out var ruleId)
                    || !new List<Language> { Language.CSharp, Language.VBNET }.Contains(ruleId.Language))
                {
                    continue;
                }

                ruleStatusesByLanguage[ruleId.Language]
                    .Add(new StandaloneRoslynRuleStatus(ruleId, analysisSettingsRule.Value.Level is RuleLevel.On));
                ruleParametersByLanguage[ruleId.Language]
                    .Add(new StandaloneRoslynRuleParameters(ruleId, analysisSettingsRule.Value.Parameters));
            }
            return (ruleStatusesByLanguage, ruleParametersByLanguage);
        }

        private static Dictionary<Language, List<T>> InitializeForAllRoslynLanguages<T>()
        {
            var dictionary = new Dictionary<Language, List<T>>();

            foreach (var language in new List<Language> { Language.CSharp, Language.VBNET })
            {
                dictionary[language] = [];
            }

            return dictionary;
        }

        internal class StandaloneRoslynRuleStatus(SonarCompositeRuleId ruleId, bool isEnabled) : IRoslynRuleStatus
        {
            public string Key => ruleId.RuleKey;

            public RuleAction GetSeverity() => isEnabled ? RuleAction.Warning : RuleAction.None;
        }

        internal class StandaloneRoslynRuleParameters(SonarCompositeRuleId ruleId, IReadOnlyDictionary<string, string> parameters) : IRuleParameters
        {
            public string Key { get; } = ruleId.RuleKey;
            public string RepositoryKey { get; } = ruleId.RepoKey;
            public IReadOnlyDictionary<string, string> Parameters { get; } = parameters;
        }

        internal class StandaloneRoslynFileExclusions(AnalysisSettings exclusions) : IFileExclusions
        {
            private readonly string[] exclusions = exclusions.NormalizedFileExclusions.ToArray();

            public Dictionary<string, string> ToDictionary() => new() { { "sonar.exclusions", string.Join(",", exclusions) } };
        }
    }
}
