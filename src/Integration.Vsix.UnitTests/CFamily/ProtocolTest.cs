/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class ProtocolTest
    {

        #region Protocol-level reading/writing tests

        [TestMethod]
        public void Write_Empty_Request()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.Write(writer, new Request());

                byte[] result = stream.ToArray();
                result.Length.Should().Be(75);
            }
        }

        [TestMethod]
        public void Write_Request_With_One_Empty_Option()
        {
            Request request = new Request
            {
                Options = new string[] { "" }
            };

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.Write(writer, request);

                byte[] result = stream.ToArray();
                result.Length.Should().Be(77);
            }
        }

        [TestMethod]
        public void Read_Empty_Response()
        {
            var response = CallProtocolRead(MockEmptyResponse());

            response.Messages.Length.Should().Be(0);
        }

        [TestMethod]
        public void Read_Response()
        {
            var response = CallProtocolRead(MockResponse());

            response.Messages.Length.Should().Be(1);
            response.Messages[0].RuleKey.Should().Be("ruleKey");
            response.Messages[0].Line.Should().Be(10);
            response.Messages[0].Column.Should().Be(11);
            response.Messages[0].EndLine.Should().Be(12);
            response.Messages[0].EndColumn.Should().Be(13);
            response.Messages[0].Text.Should().Be("Issue message");
            response.Messages[0].PartsMakeFlow.Should().Be(true);
            response.Messages[0].Parts.Length.Should().Be(1);
        }

        [TestMethod]
        public void Response_Filtering_NoFiltering()
        {
            var response = CallProtocolRead(MockResponseWithIssuesFromMultipleFiles(), null);

            response.Messages.Length.Should().Be(3);
            response.Messages[0].Filename.Should().Be("c:\\data\\file1.cpp");
            response.Messages[1].Filename.Should().Be("e:\\data\\file2.cpp");
            response.Messages[2].Filename.Should().Be("E:\\data\\file2.cpp");

            response.Messages[0].RuleKey.Should().Be("ruleKey1");
            response.Messages[1].RuleKey.Should().Be("ruleKey2");
            response.Messages[2].RuleKey.Should().Be("ruleKey3");
        }

        [TestMethod]
        public void Response_Filtering_WithFiltering()
        {
            // 1. Match file1
            var response = CallProtocolRead(MockResponseWithIssuesFromMultipleFiles(), "C:\\DATA/File1.cpp");

            response.Messages.Length.Should().Be(1);
            response.Messages[0].Filename.Should().Be("c:\\data\\file1.cpp");
            response.Messages[0].RuleKey.Should().Be("ruleKey1");

            // 2. Match file2
            response = CallProtocolRead(MockResponseWithIssuesFromMultipleFiles(), "e:/DATA/FILE2.cpp");

            response.Messages.Length.Should().Be(2);
            response.Messages[0].Filename.Should().Be("e:\\data\\file2.cpp");
            response.Messages[1].Filename.Should().Be("E:\\data\\file2.cpp");

            response.Messages[0].RuleKey.Should().Be("ruleKey2");
            response.Messages[1].RuleKey.Should().Be("ruleKey3");
        }

        [TestMethod]
        public void Response_Filtering_NoMatches()
        {
            var response = CallProtocolRead(MockResponseWithIssuesFromMultipleFiles(), "file4.cpp");

            response.Messages.Length.Should().Be(0);
        }

        [TestMethod]
        public void Read_Bad_Response_Throw()
        {
            Action act = () => CallProtocolRead(MockBadStartResponse());
            act.Should().ThrowExactly<InvalidDataException>();

            act = () => CallProtocolRead(MockBadEndResponse());
            act.Should().ThrowExactly<InvalidDataException>();
        }

        private static Response CallProtocolRead(byte[] data, string issueFileName = null)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                return Protocol.Read(reader, issueFileName);
            }
        }

        #endregion // Protocol-level reading/writing tests

        #region Low-level reading/writing tests

        [TestMethod]
        public void Write_UTF8()
        {
            WriteUtf("").Should().BeEquivalentTo(new byte[] { 0, 0 });
            WriteUtf("a").Should().BeEquivalentTo(new byte[] { 0, 1, 97 });
            WriteUtf("A").Should().BeEquivalentTo(new byte[] { 0, 1, 65 });
            WriteUtf("0").Should().BeEquivalentTo(new byte[] { 0, 1, 48 });
            WriteUtf("\n").Should().BeEquivalentTo(new byte[] { 0, 1, 10 });
            // 3 bytes
            WriteUtf("\u0800").Should().BeEquivalentTo(new byte[] { 0, 3, 224, 160, 128 });
            // Special case of NUL
            Action actNul = () => WriteUtf("\u0000");
            actNul.Should().ThrowExactly<InvalidOperationException>();
            // Supplementary characters as surrogate pair  (see CESU-8) are not supported for now
            Action actSupp = () => WriteUtf("\U00010400");
            actSupp.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void Read_UTF8()
        {
            ReadUtf(WriteUtf("")).Should().Be("");
            ReadUtf(WriteUtf("a")).Should().Be("a");
            ReadUtf(WriteUtf("A")).Should().Be("A");
            ReadUtf(WriteUtf("0")).Should().Be("0");
            ReadUtf(WriteUtf("\n")).Should().Be("\n");
            // 3 bytes
            ReadUtf(WriteUtf("\u0800")).Should().Be("\u0800");
        }

        [TestMethod]
        public void Write_Short()
        {
            WriteShort(0).Should().BeEquivalentTo(new byte[] { 0, 0 });
            WriteShort(1).Should().BeEquivalentTo(new byte[] { 0, 1 });
        }

        [TestMethod]
        public void Write_Int()
        {
            WriteInt(0).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0 });
            WriteInt(1).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1 });
            WriteInt(int.MaxValue).Should().BeEquivalentTo(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF });
            WriteInt(int.MinValue).Should().BeEquivalentTo(new byte[] { 0x80, 0x00, 0x00, 0x00 });
        }

        [TestMethod]
        public void Write_Long()
        {
            WriteLong(0).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            WriteLong(long.MaxValue).Should().BeEquivalentTo(new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 });
        }

        [TestMethod]
        public void Read_Int()
        {
            ReadInt(WriteInt(0)).Should().Be(0);
            ReadInt(WriteInt(int.MaxValue)).Should().Be(int.MaxValue);
            ReadInt(WriteInt(int.MinValue)).Should().Be(int.MinValue);
        }

        private byte[] WriteUtf(string s)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, s);

                return stream.ToArray();
            }
        }

        private string ReadUtf(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                BinaryReader reader = new BinaryReader(stream);
                return Protocol.ReadUTF(reader);
            }
        }

        private byte[] WriteShort(ushort s)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUShort(writer, s);

                return stream.ToArray();
            }
        }

        private byte[] WriteInt(int i)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteInt(writer, i);

                return stream.ToArray();
            }
        }

        private int ReadInt(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                BinaryReader reader = new BinaryReader(stream);
                return Protocol.ReadInt(reader);
            }
        }

        private byte[] WriteLong(long l)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteLong(writer, l);

                return stream.ToArray();
            }
        }

        #endregion // Low-level reading/writing tests

        private byte[] MockEmptyResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 0 issues

                // 0 measures
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 0);

                // 0 symbols
                Protocol.WriteUTF(writer, "symbols");
                Protocol.WriteInt(writer, 0);

                Protocol.WriteUTF(writer, "END");
                return stream.ToArray();
            }
        }

        private byte[] MockBadStartResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "FOO");
                return stream.ToArray();
            }
        }

        private byte[] MockBadEndResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");
                Protocol.WriteUTF(writer, "FOO");
                return stream.ToArray();
            }
        }

        private byte[] MockResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 1 issue
                Protocol.WriteUTF(writer, "message");

                Protocol.WriteUTF(writer, "ruleKey");
                Protocol.WriteUTF(writer, "file.cpp");
                Protocol.WriteInt(writer, 10);
                Protocol.WriteInt(writer, 11);
                Protocol.WriteInt(writer, 12);
                Protocol.WriteInt(writer, 13);
                Protocol.WriteInt(writer, 100);
                Protocol.WriteUTF(writer, "Issue message");
                writer.Write(true);

                // 1 flow
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "another.cpp");
                Protocol.WriteInt(writer, 14);
                Protocol.WriteInt(writer, 15);
                Protocol.WriteInt(writer, 16);
                Protocol.WriteInt(writer, 17);
                Protocol.WriteUTF(writer, "Flow message");

                // 1 measure
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "file.cpp");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                byte[] execLines = new byte[] { 1, 2, 3, 4 };
                Protocol.WriteInt(writer, execLines.Length);
                writer.Write(execLines);
                

                // 1 symbol
                Protocol.WriteUTF(writer, "symbols");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                Protocol.WriteUTF(writer, "END");
                return stream.ToArray();
            }
        }

        private byte[] MockResponseWithIssuesFromMultipleFiles()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // Issue 1 
                Protocol.WriteUTF(writer, "message");
                Protocol.WriteUTF(writer, "ruleKey1");
                Protocol.WriteUTF(writer, "c:\\data\\file1.cpp");
                Protocol.WriteInt(writer, 10);
                Protocol.WriteInt(writer, 11);
                Protocol.WriteInt(writer, 12);
                Protocol.WriteInt(writer, 13);
                Protocol.WriteInt(writer, 100);
                Protocol.WriteUTF(writer, "Issue message");

                writer.Write(false); // no flow
                Protocol.WriteInt(writer, 0);

                // Issue 2
                Protocol.WriteUTF(writer, "message");
                Protocol.WriteUTF(writer, "ruleKey2");
                Protocol.WriteUTF(writer, "e:\\data\\file2.cpp");
                Protocol.WriteInt(writer, 10);
                Protocol.WriteInt(writer, 11);
                Protocol.WriteInt(writer, 12);
                Protocol.WriteInt(writer, 13);
                Protocol.WriteInt(writer, 100);
                Protocol.WriteUTF(writer, "Issue message");

                writer.Write(false); // no flow
                Protocol.WriteInt(writer, 0);

                // Issue 3 
                Protocol.WriteUTF(writer, "message");
                Protocol.WriteUTF(writer, "ruleKey3");
                Protocol.WriteUTF(writer, "E:\\data\\file2.cpp");
                Protocol.WriteInt(writer, 10);
                Protocol.WriteInt(writer, 11);
                Protocol.WriteInt(writer, 12);
                Protocol.WriteInt(writer, 13);
                Protocol.WriteInt(writer, 100);
                Protocol.WriteUTF(writer, "Issue message");

                writer.Write(false); // no flow
                Protocol.WriteInt(writer, 0);


                // 1 measure
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "file.cpp");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                byte[] execLines = new byte[] { 1, 2, 3, 4 };
                Protocol.WriteInt(writer, execLines.Length);
                writer.Write(execLines);


                // 1 symbol
                Protocol.WriteUTF(writer, "symbols");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                Protocol.WriteUTF(writer, "END");
                return stream.ToArray();
            }
        }

    }
}
