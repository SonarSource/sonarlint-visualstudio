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

using SonarQube.Client.Models.ServerSentEvents.ServerContract;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class SqServerSentEventParserTests
    {
        [TestMethod]
        public void Parse_NullEventLines_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_EmptyEventLines_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(Array.Empty<string>());

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventType_MissingEventTypeField_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] {"data: some data"});

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventType_EventTypeIsEmpty_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] { "event: ", "data: some data"});

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventType_EventTypeIsNotInCorrectFormat_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] { "event : extra space", "data: some data" });

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventData_MissingEventDataField_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[]{ "event: some type" });

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventData_EventDataIsEmpty_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] { "event: some type", "data: " });

            result.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidEventData_EventDataIsNotInCorrectFormat_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] { "event: some type", "data : extra space" });

            result.Should().BeNull();
        }


        [TestMethod]
        public void Parse_CorrectEventString_ParsedEvent()
        {
            var eventLines = new[]
            {
                "event: some event type",
                "data: some event data"
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(eventLines);

            result.Should().NotBeNull();
            result.Type.Should().Be("some event type");
            result.Data.Should().Be("some event data");
        }

        [TestMethod]
        public void Parse_CorrectEventString_MultilineData_ParsedEvent()
        {
            var eventLines = new[]
            {
                "event: some event type",
                "data: some event data1",
                "data: ",
                "data: some event data2",
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(eventLines);

            result.Should().NotBeNull();
            result.Type.Should().Be("some event type");
            result.Data.Should().Be("some event data1some event data2");
        }

        [TestMethod]
        public void Parse_HasJunkFields_JunkFieldsIgnored()
        {
            var eventLines = new[]
            {
                "junk1: junk field1",
                "EVENT: junk event type",
                "event:",
                "event: some event type",
                "data: ",
                "data: some event data1",
                "junk2: junk field2",
                "DATA: junk data2",
                "data: some event data2",
                "junk3: junk field3"
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(eventLines);

            result.Should().NotBeNull();
            result.Type.Should().Be("some event type");
            result.Data.Should().Be("some event data1some event data2");
        }

        [TestMethod]
        public void Parse_EventTypeIsNotTheFirstField_EventTypeIsStillParsedCorrectly()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Parse(new[] { "data: some data", "event: some type" });

            result.Should().NotBeNull();
            result.Type.Should().Be("some type");
            result.Data.Should().Be("some data");
        }


        private static SqServerSentEventParser CreateTestSubject() => new();
    }
}
