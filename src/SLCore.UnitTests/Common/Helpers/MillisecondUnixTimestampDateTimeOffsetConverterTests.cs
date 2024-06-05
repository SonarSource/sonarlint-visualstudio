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
using SonarLint.VisualStudio.SLCore.Common.Helpers;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers
{
    [TestClass]
    public class MillisecondUnixTimestampDateTimeOffsetConverterTests
    {
        [TestMethod]
        public void SerializeObject_Throws()
        {
            var testData = new TestData("hello", DateTimeOffset.UtcNow, 12345);

            Action act = () => JsonConvert.SerializeObject(testData);

            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void DeserializeObject_NoValueInJson_SetsDefaultValue()
        {
            const string json = """
                                {
                                    "someString": "hello",
                                    "someLong" : 1234567890
                                }
                                """;

            var testData = JsonConvert.DeserializeObject<TestData>(json);

            testData.SomeDateTimeOffset.Should().Be(default);
        }

        [TestMethod]
        public void DeserializeObject_IncorrectValueInJson_SetsDefaultValue()
        {
            const string json = """
                                {
                                    "someString": "hello",
                                    "someDateTimeOffset": "lol",
                                    "someLong" : 1234567890
                                }
                                """;

            var testData = JsonConvert.DeserializeObject<TestData>(json);

            testData.SomeDateTimeOffset.Should().Be(default);
        }

        [TestMethod]
        public void DeserializeObject_CorrectlyDeserializesUnixTimestampToDateTimeOffset()
        {
            var date = DateTimeOffset.UtcNow;
            var timestamp = date.ToUnixTimeMilliseconds();

            var json = $$"""
                         {
                             "someString": "hello",
                             "someDateTimeOffset": {{timestamp}},
                             "someLong" : 1234567890
                         }
                         """;

            var testData = JsonConvert.DeserializeObject<TestData>(json);

            testData.SomeDateTimeOffset.Should().BeCloseTo(date, precision:1); // unix timestamp loses some precision
            testData.SomeDateTimeOffset.Offset.Should().Be(TimeSpan.Zero);
        }        
        
        [DataTestMethod]
        [DataRow(253402300799999L + 1)]
        [DataRow(-62135596800000L - 1)]
        public void DeserializeObject_TimestampOutOfRange_Throws(long timestamp)
        {
            var json = $$"""
                         {
                             "someString": "hello",
                             "someDateTimeOffset": {{timestamp}},
                             "someLong" : 1234567890
                         }
                         """;

            Action act = () => JsonConvert.DeserializeObject<TestData>(json);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        private class TestData
        {
            public TestData(string someString, [JsonConverter(typeof(MillisecondUnixTimestampDateTimeOffsetConverter))]DateTimeOffset someDateTimeOffset, long someLong)
            {
                SomeString = someString;
                SomeDateTimeOffset = someDateTimeOffset;
                SomeLong = someLong;
            }

            public string SomeString { get; }
            [JsonConverter(typeof(MillisecondUnixTimestampDateTimeOffsetConverter))]
            public DateTimeOffset SomeDateTimeOffset { get; set; }
            public long SomeLong { get; }
        }
    }
}
