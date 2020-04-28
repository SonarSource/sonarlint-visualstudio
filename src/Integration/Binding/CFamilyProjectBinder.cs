using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class CFamilyProjectBinder : IProjectBinder
    {
        private readonly ISolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private readonly IFileSystem fileSystem;

        public CFamilyProjectBinder(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            ruleSetInfoProvider = serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            ruleSetInfoProvider.AssertLocalServiceIsNotNull();
        }

        public bool IsBound(BindingConfiguration binding, Project project)
        {
            Debug.Assert(binding != null);
            Debug.Assert(project != null);

            var languages = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project);

            return languages.All(language =>
            {
                var slnLevelBindingConfigFilepath = ruleSetInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(binding.Project.ProjectKey, language, binding.Mode);

                return fileSystem.File.Exists(slnLevelBindingConfigFilepath);
            });
        }
    }
}
