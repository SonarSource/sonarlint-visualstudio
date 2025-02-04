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
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Telemetry;

[TestClass]
public class FixSuggestionResolvedParamsTests
{
    [DataRow("sgstid", FixSuggestionStatus.ACCEPTED, null, """{"suggestionId":"sgstid","status":"ACCEPTED","snippetIndex":null}""")]
    [DataRow("sgstid2", FixSuggestionStatus.DECLINED, 12, """{"suggestionId":"sgstid2","status":"DECLINED","snippetIndex":12}""")]
    [DataTestMethod]
    public void SerializedAsExpected(string suggestionId, FixSuggestionStatus status, int? index, string expectedSerialized)
    {
        JsonConvert.SerializeObject(new FixSuggestionResolvedParams(suggestionId, status, index)).Should().Be(expectedSerialized);
    }
}
