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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Progress;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Progress;

[TestClass]
public class StartProgressParamsTest
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Deserialized_AsExpected(bool boolValue)
    {
        var taskId = Guid.NewGuid().ToString();
        var configurationScopeId = "configScope";
        var title = "analyze";
        var message = "analyze 1 file";
        var expected = new StartProgressParams(taskId, configurationScopeId, title, message, boolValue, boolValue);
        string serialized = $@"
            {{
              ""taskId"": ""{taskId}"",
              ""configurationScopeId"": ""{configurationScopeId}"",
              ""title"": ""{title}"",
              ""message"": ""{message}"",
              ""indeterminate"": ""{boolValue}"",
              ""cancellable"": ""{boolValue}""
            }}";

        var actual = JsonConvert.DeserializeObject<StartProgressParams>(serialized);

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Serialized_AsExpected(bool boolValue)
    {
        var taskId = Guid.NewGuid().ToString();
        var configurationScopeId = "configScope";
        var title = "analyze";
        var message = "analyze 1 file";
        var testSubject = new StartProgressParams(taskId, configurationScopeId, title, message, boolValue, boolValue);
        string expected = $@"
            {{
              ""taskId"": ""{taskId}"",
              ""configurationScopeId"": ""{configurationScopeId}"",
              ""title"": ""{title}"",
              ""message"": ""{message}"",
              ""indeterminate"": {boolValue},
              ""cancellable"": {boolValue}
            }}";

        var actual = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        actual.Should().BeEquivalentTo(expected);
    }
}
