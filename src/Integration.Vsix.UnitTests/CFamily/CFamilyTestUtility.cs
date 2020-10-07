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
            public string platformName { get; set; } = "Win32";
            public IDictionary<string, string> projectConfigProperties { get; set; } = new Dictionary<string, string>
            {
                ["PlatformToolset"] = "v140_xp"
            };
            public IDictionary<string, string> fileConfigProperties { get; set; } = new Dictionary<string, string>
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
            public bool isVCCLCompilerTool { get; set; } = true;
            public string itemType { get; set; } = "ClCompile";
            public ConfigurationTypes configurationType { get; set; } = ConfigurationTypes.typeApplication;
        }

        private static readonly ProjectItemConfig defaultSetting = new ProjectItemConfig();

        internal static Mock<ProjectItem> CreateProjectItemWithProject(string projectName, ProjectItemConfig projectItemConfig = null)
        {
            projectItemConfig = projectItemConfig ?? defaultSetting;

            var vcProjectMock = new Mock<VCProject>();
            var vcConfig = CreateVCConfigurationWithProperties(projectItemConfig);
            vcProjectMock.SetupGet(x => x.ActiveConfiguration).Returns(vcConfig);

            var projectMock = new ProjectMock(projectName) { Project = vcProjectMock.Object };

            var vcFileMock = new Mock<VCFile>();
            vcFileMock.SetupGet(x => x.ItemType).Returns(projectItemConfig.itemType);
            var vcFileConfig = CreateVCFileConfigurationWithToolProperties(projectItemConfig);
            vcFileMock.Setup(x => x.GetFileConfigurationForProjectConfiguration(vcConfig)).Returns(vcFileConfig);
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
            vcPlatformMock.SetupGet(x => x.Name).Returns(projectItemConfig.platformName);

            var vcConfigMock = new Mock<VCConfiguration>();
            vcConfigMock.SetupGet(x => x.Platform).Returns(vcPlatformMock.Object);
            vcConfigMock.SetupGet(x => x.ConfigurationType).Returns(projectItemConfig.configurationType);

            vcConfigMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    projectItemConfig.projectConfigProperties?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            return vcConfigMock.Object;
        }

        private static VCFileConfiguration CreateVCFileConfigurationWithToolProperties(ProjectItemConfig projectItemConfig)
        {
            var toolPropertiesMock = new Mock<IVCRulePropertyStorage>();
            if (projectItemConfig.isVCCLCompilerTool)
            {
                toolPropertiesMock.As<VCCLCompilerTool>();
            }

            toolPropertiesMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    projectItemConfig.fileConfigProperties?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            var vcFileConfigMock = new Mock<VCFileConfiguration>();
            vcFileConfigMock.SetupGet(x => x.Tool).Returns(toolPropertiesMock.Object);

            return vcFileConfigMock.Object;
        }

        internal static void AssertMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Equals(message))), Times.Once);
        }

        internal static void AssertPartialMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Contains(message))), Times.Once);
        }
    }
}
