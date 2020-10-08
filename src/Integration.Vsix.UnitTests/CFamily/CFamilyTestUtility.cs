using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    internal class CFamilyTestUtility
    {
        internal class ProjectItemConfig
        {
            public string PlatformName { get; set; } = "Win32";

            public IDictionary<string, string> ProjectConfigProperties { get; set; } = new Dictionary<string, string>
            {
                ["PlatformToolset"] = "v140_xp"
            };
            public IDictionary<string, string> FileConfigProperties { get; set; } = new Dictionary<string, string>
            {
                ["PrecompiledHeader"] = "NotUsing",
                ["CompileAs"] = "CompileAsCpp",
                ["CompileAsManaged"] = "false",
                ["EnableEnhancedInstructionSet"] = "",
                ["RuntimeLibrary"] = "",
                ["LanguageStandard"] = "",
                ["ExceptionHandling"] = "Sync",
                ["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck",
            };
            public bool IsVCCLCompilerTool { get; set; } = true;
            public string ItemType { get; set; } = "ClCompile";
            public ConfigurationTypes ConfigurationType { get; set; } = ConfigurationTypes.typeApplication;
        }

        private static readonly ProjectItemConfig DefaultSetting = new ProjectItemConfig();

        internal static Mock<ProjectItem> CreateMockProjectItem(string projectName, ProjectItemConfig projectItemConfig = null)
        {
            projectItemConfig ??= DefaultSetting;

            var vcProjectMock = new Mock<VCProject>();
            var vcConfig = CreateVCConfigurationWithProperties(projectItemConfig);
            vcProjectMock.SetupGet(x => x.ActiveConfiguration).Returns(vcConfig);
            vcProjectMock.Setup(x => x.ProjectFile).Returns(projectName);

            var toolPropertiesMock = GetToolPropertiesMock(projectItemConfig);
            var vcFileConfigMock = new Mock<VCFileConfiguration>();
            vcFileConfigMock.SetupGet(x => x.Tool).Returns(toolPropertiesMock.Object);

            var vcFileMock = new Mock<VCFile>();
            vcFileMock.SetupGet(x => x.ItemType).Returns(projectItemConfig.ItemType);
            vcFileMock.Setup(x => x.GetFileConfigurationForProjectConfiguration(vcConfig)).Returns(vcFileConfigMock.Object);

            var projectMock = new ProjectMock(projectName) { Project = vcProjectMock.Object };
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(projectMock);
            projectItemMock.Setup(i => i.Object).Returns(vcFileMock.Object);

            // Set the project item to have a valid DTE configuration
            // - used to check whether the project item is in a solution or not
            var dteConfigManagerMock = new Mock<ConfigurationManager>();
            var dteConfigMock = new Mock<Configuration>();
            dteConfigManagerMock.Setup(x => x.ActiveConfiguration).Returns(dteConfigMock.Object);
            projectItemMock.Setup(i => i.ConfigurationManager).Returns(dteConfigManagerMock.Object);

            return projectItemMock;
        }

        private static VCConfiguration CreateVCConfigurationWithProperties(ProjectItemConfig projectItemConfig)
        {
            var vcPlatformMock = new Mock<VCPlatform>();
            vcPlatformMock.SetupGet(x => x.Name).Returns(projectItemConfig.PlatformName);

            var vcConfigMock = new Mock<VCConfiguration>();
            vcConfigMock.SetupGet(x => x.Platform).Returns(vcPlatformMock.Object);
            vcConfigMock.SetupGet(x => x.ConfigurationType).Returns(projectItemConfig.ConfigurationType);

            vcConfigMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    projectItemConfig.ProjectConfigProperties?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            // Project VCCLCompilerTool needed for header files analysis
            var ivcCollection = new Mock<IVCCollection>();
            vcConfigMock.SetupGet(x => x.Tools).Returns(ivcCollection.Object);

            var toolPropertiesMock = GetToolPropertiesMock(projectItemConfig);
            ivcCollection.Setup(x => x.Item("VCCLCompilerTool")).Returns(projectItemConfig.IsVCCLCompilerTool ? toolPropertiesMock.Object : null);

            return vcConfigMock.Object;
        }

        private static Mock<IVCRulePropertyStorage> GetToolPropertiesMock(ProjectItemConfig projectItemConfig)
        {
            var toolPropertiesMock = new Mock<IVCRulePropertyStorage>();

            if (projectItemConfig.IsVCCLCompilerTool)
            {
                toolPropertiesMock.As<VCCLCompilerTool>();
            }

            toolPropertiesMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    projectItemConfig.FileConfigProperties?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            return toolPropertiesMock;
        }
    }
}
