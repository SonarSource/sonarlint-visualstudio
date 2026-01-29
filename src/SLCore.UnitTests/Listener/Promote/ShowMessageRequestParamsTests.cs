/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.SLCore.Listener.Promote;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Promote;

[TestClass]
public class ShowMessageRequestParamsTests
{
    [TestMethod]
    public void ShowMessageRequestParams_DeserializesCorrectly()
    {
        var expected = new ShowMessageRequestParams(MessageType.INFO, "Test message", new List<MessageActionItem>
        {
            new("action1", "Action 1", true),
            new("action2", "Action 2", false)
        });
        const string serialized = """
                                  {
                                      "type": "INFO",
                                      "message": "Test message",
                                      "actions": [
                                          {
                                              "key": "action1",
                                              "displayText": "Action 1",
                                              "isPrimaryAction": true
                                          },
                                          {
                                              "key": "action2",
                                              "displayText": "Action 2",
                                              "isPrimaryAction": false
                                          }
                                      ]
                                  }
                                  """;

        var actual = JsonConvert.DeserializeObject<ShowMessageRequestParams>(serialized);

        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<ShowMessageRequestParams>());
    }
}
