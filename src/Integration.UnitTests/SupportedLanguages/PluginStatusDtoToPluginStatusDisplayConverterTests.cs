/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.SupportedLanguages;

[TestClass]
public class PluginStatusDtoToPluginStatusDisplayConverterTests
{
    private PluginStatusDtoToPluginStatusDisplayConverter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new PluginStatusDtoToPluginStatusDisplayConverter();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<PluginStatusDtoToPluginStatusDisplayConverter, IPluginStatusDtoToPluginStatusDisplayConverter>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<PluginStatusDtoToPluginStatusDisplayConverter>();
    }

    [TestMethod]
    public void Convert_EmbeddedSource_ReturnsExtensionVersionText()
    {
        var dto = new PluginStatusDto(Language.CS, "C#", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null);

        var display = testSubject.Convert(dto);

        display.PluginName.Should().Be("C#");
        display.State.Should().Be(PluginStateDto.ACTIVE);
        display.Source.Should().Be(ArtifactSourceDto.EMBEDDED);
        display.SourceText.Should().Be(string.Format(Strings.PluginStatuses_SourceIde, VersionHelper.SonarLintVersion));
    }

    [TestMethod]
    public void Convert_OnDemandSource_ReturnsExtensionVersionText()
    {
        var dto = new PluginStatusDto(Language.CS, "C#", PluginStateDto.DOWNLOADING, ArtifactSourceDto.ON_DEMAND, "1.0", null, null);

        var display = testSubject.Convert(dto);

        display.PluginName.Should().Be("C#");
        display.State.Should().Be(PluginStateDto.DOWNLOADING);
        display.Source.Should().Be(ArtifactSourceDto.ON_DEMAND);
        display.SourceText.Should().Be(string.Format(Strings.PluginStatuses_SourceIde, VersionHelper.SonarLintVersion));
    }

    [TestMethod]
    public void Convert_SonarQubeServerSource_ReturnsServerVersionText()
    {
        var dto = new PluginStatusDto(Language.PYTHON, "Python", PluginStateDto.SYNCED, ArtifactSourceDto.SONARQUBE_SERVER, "2.0", "1.5", "10.8.1");

        var display = testSubject.Convert(dto);

        display.PluginName.Should().Be("Python");
        display.State.Should().Be(PluginStateDto.SYNCED);
        display.Source.Should().Be(ArtifactSourceDto.SONARQUBE_SERVER);
        display.SourceText.Should().Be(string.Format(Strings.PluginStatuses_SourceServer, "10.8.1"));
    }

    [TestMethod]
    public void Convert_SonarQubeCloudSource_ReturnsCloudText()
    {
        var dto = new PluginStatusDto(Language.JS, "JS", PluginStateDto.ACTIVE, ArtifactSourceDto.SONARQUBE_CLOUD, "1.0", null, null);

        var display = testSubject.Convert(dto);

        display.PluginName.Should().Be("JS");
        display.State.Should().Be(PluginStateDto.ACTIVE);
        display.Source.Should().Be(ArtifactSourceDto.SONARQUBE_CLOUD);
        display.SourceText.Should().Be(Strings.PluginStatuses_SourceCloud);
    }

    [TestMethod]
    public void Convert_NullSource_ReturnsEmptyString()
    {
        var dto = new PluginStatusDto(Language.GO, "Go", PluginStateDto.FAILED, null, null, null, null);

        var display = testSubject.Convert(dto);

        display.PluginName.Should().Be("Go");
        display.State.Should().Be(PluginStateDto.FAILED);
        display.Source.Should().BeNull();
        display.SourceText.Should().BeEmpty();
    }
}
