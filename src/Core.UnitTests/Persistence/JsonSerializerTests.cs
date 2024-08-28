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

using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Persistence;
using SonarLint.VisualStudio.TestInfrastructure;
using JsonSerializer = SonarLint.VisualStudio.Core.Persistence.JsonSerializer;

namespace SonarLint.VisualStudio.Core.UnitTests.Persistence;

[TestClass]
public class JsonSerializerTests
{
    private JsonSerializer testSubject;
    private ILogger logger;
    private record TestType(string PropName);

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        testSubject = new JsonSerializer(logger);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<JsonSerializer, IJsonSerializer>(MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<JsonSerializer>();
    }

    [TestMethod]
    public void TrySerialize_Succeeds_ReturnsString()
    {
        var expectedString = "{\"PropName\":\"abc\"}";

        var result = testSubject.TrySerialize(new TestType("abc"), out string serializedObj);

        result.Should().BeTrue();
        serializedObj.Should().Be(expectedString);
    }

    [TestMethod]
    public void TrySerialize_Fails_LogsAndReturnsFalse()
    {
        var serializer = new JsonSerializer(logger, (_, _, _) => throw new Exception());

        var result = serializer.TrySerialize(new TestType("abc"), out string serializedObj);

        result.Should().BeFalse();
        serializedObj.Should().BeNull();
        logger.Received(1).WriteLine(string.Format(PersistenceStrings.FailedToSerializeObject, nameof(TestType)));
    }

    [TestMethod]
    public void TryDeserialize_Succeeds_ReturnsString()
    {
        var serializedString = "{\"PropName\":\"abc\"}";

        var result = testSubject.TryDeserialize(serializedString, out TestType deserializedObj);

        result.Should().BeTrue();
        deserializedObj.Should().BeEquivalentTo(new TestType("abc"));
    }

    [TestMethod]
    public void TryDeserialize_Fails_LogsAndReturnsFalse()
    {
        var serializedString = "invalid";

        var result = testSubject.TryDeserialize(serializedString, out TestType deserializedObj);

        result.Should().BeFalse();
        deserializedObj.Should().BeNull();
        logger.Received(1).WriteLine(string.Format(PersistenceStrings.FailedToDeserializeObject, nameof(TestType)));
    }
}
