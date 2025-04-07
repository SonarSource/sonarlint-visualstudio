/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB;

[TestClass]
public class RoslynConfigGeneratorTests
{
    private RoslynConfigGenerator testSubject;
    private IFileSystemService fileSystemService;
    private IGlobalConfigGenerator globalConfigGenerator;
    private ISonarLintConfigGenerator sonarLintConfigGenerator;
    private ISonarLintConfigurationXmlSerializer sonarLintConfigurationXmlSerializer;

    private readonly Language language = Language.VBNET;
    private const string BaseDirectory = @"C:\base\dir";
    private const string SlconfigDirectory = BaseDirectory + @"\VB";
    private const string SlconfigFilePath = SlconfigDirectory + @"\SonarLint.xml";
    private const string GlobalconfigFilePath = BaseDirectory + @"\sonarlint_vb.globalconfig";
    private readonly Dictionary<string, string> properties = new() { { "a", "b" } };
    private readonly IFileExclusions fileExclusions = Substitute.For<IFileExclusions>();
    private readonly List<IRoslynRuleStatus> ruleStatuses = [Substitute.For<IRoslynRuleStatus>()];
    private readonly List<IRuleParameters> ruleParameters = [Substitute.For<IRuleParameters>()];

    [TestInitialize]
    public void TestInitialize()
    {
        fileSystemService = Substitute.For<IFileSystemService>();
        globalConfigGenerator = Substitute.For<IGlobalConfigGenerator>();
        sonarLintConfigGenerator = Substitute.For<ISonarLintConfigGenerator>();
        sonarLintConfigurationXmlSerializer = Substitute.For<ISonarLintConfigurationXmlSerializer>();
        testSubject = new RoslynConfigGenerator(fileSystemService, globalConfigGenerator, sonarLintConfigGenerator, sonarLintConfigurationXmlSerializer);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynConfigGenerator, IRoslynConfigGenerator>(
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IGlobalConfigGenerator>(),
            MefTestHelpers.CreateExport<ISonarLintConfigGenerator>(),
            MefTestHelpers.CreateExport<ISonarLintConfigurationXmlSerializer>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynConfigGenerator>();

    [TestMethod]
    public void GenerateAndSaveConfiguration_CallsGeneratorsWithCorrectParametersAndSaves()
    {
        var sonarLintConfiguration = new SonarLintConfiguration();
        var slconfigContent = "slconfig content";
        var globalconfigContent = "globalconfig content";
        sonarLintConfigGenerator.Generate(ruleParameters, properties, fileExclusions, language).Returns(sonarLintConfiguration);
        sonarLintConfigurationXmlSerializer.Serialize(sonarLintConfiguration).Returns(slconfigContent);
        globalConfigGenerator.Generate(ruleStatuses).Returns(globalconfigContent);

        testSubject.GenerateAndSaveConfiguration(language, BaseDirectory, properties, fileExclusions, ruleStatuses, ruleParameters);

        fileSystemService.Directory.Received().CreateDirectory(BaseDirectory);
        fileSystemService.Directory.Received().CreateDirectory(SlconfigDirectory);
        fileSystemService.File.Received().WriteAllText(SlconfigFilePath, slconfigContent);
        fileSystemService.File.Received().WriteAllText(GlobalconfigFilePath, globalconfigContent);
    }
}
