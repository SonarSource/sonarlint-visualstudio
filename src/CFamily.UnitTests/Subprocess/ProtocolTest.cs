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

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.CFamily.SubProcess.UnitTests
{
    [TestClass]
    public class ProtocolTest
    {

        #region Protocol-level reading/writing tests

        [TestMethod]
        public void Read_Empty_Response()
        {
            var response = CallProtocolRead(MockEmptyResponse());

            response.Messages.Length.Should().Be(0);
        }

        [TestMethod]
        public void Read_RequestHasFileName_ReturnedMessageHasNoFileName_MessageIsNotIgnored()
        {
            const string returnedIssueFileName = "";

            var response = CallProtocolRead(MockResponse(returnedIssueFileName));

            response.Messages.Length.Should().Be(1);
            response.Messages[0].Filename.Should().Be(string.Empty);
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
        public void Response_WithDataFlow_DataFlowIgnored()
        {
            var response = CallProtocolRead(MockResponseWithDataFlow());

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
        public void Response_WithMultipleMessages_MultipleMessageAreReturned()
        {
            var response = CallProtocolRead(MockResponseWithIssuesFromMultipleFiles());

            response.Messages.Length.Should().Be(3);
            response.Messages[0].Filename.Should().Be("c:\\data\\file1.cpp");
            response.Messages[1].Filename.Should().Be("e:\\data\\file2.cpp");
            response.Messages[2].Filename.Should().Be("E:\\data\\file2.cpp");

            response.Messages[0].RuleKey.Should().Be("ruleKey1");
            response.Messages[1].RuleKey.Should().Be("ruleKey2");
            response.Messages[2].RuleKey.Should().Be("ruleKey3");
        }

        [TestMethod]
        public void Read_Bad_Response_Throw()
        {
            Action act = () => CallProtocolRead(MockBadStartResponse());
            act.Should().ThrowExactly<InvalidDataException>();

            act = () => CallProtocolRead(MockBadEndResponse());
            act.Should().ThrowExactly<InvalidDataException>();
        }

        [TestMethod]
        public void Read_ResponseWithQuickFixes_QuickFixesRead()
        {
            var response = CallProtocolRead(MockResponseWithQuickFixes());

            response.Messages.Length.Should().Be(1);
         
            response.Messages[0].Fixes.Length.Should().Be(1);
            response.Messages[0].Fixes[0].Message.Should().Be("Fix message");
            response.Messages[0].Fixes[0].Edits.Length.Should().Be(1);
            response.Messages[0].Fixes[0].Edits[0].Text.Should().Be("Edit message");
            response.Messages[0].Fixes[0].Edits[0].StartLine.Should().Be(1);
            response.Messages[0].Fixes[0].Edits[0].StartColumn.Should().Be(2);
            response.Messages[0].Fixes[0].Edits[0].EndLine.Should().Be(3);
            response.Messages[0].Fixes[0].Edits[0].EndColumn.Should().Be(4);
        }

        private static Response CallProtocolRead(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);

                var messages = new List<Message>();
                Protocol.Read(reader, messages.Add);

                return new Response(messages.ToArray());
            }
        }

        #endregion // Protocol-level reading/writing tests

        #region Low-level reading/writing tests

        [TestMethod]
        public void Write_UTF8()
        {
            WriteUtf("").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0 });
            WriteUtf("a").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1, 97 });
            WriteUtf("A").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1, 65 });
            WriteUtf("0").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1, 48 });
            WriteUtf("\n").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1, 10 });
            // 3 bytes
            WriteUtf("\u0800").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 3, 224, 160, 128 });
            // NUL
            WriteUtf("\u0000").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1, 0 });
            // Supplementary characters
            WriteUtf("\U00010400").Should().BeEquivalentTo(new byte[] { 0, 0, 0, 4, 0xF0, 0x90, 0x90, 0x80 });
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
        public void Write_Int()
        {
            WriteInt(0).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0 });
            WriteInt(1).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 1 });
            WriteInt(int.MaxValue).Should().BeEquivalentTo(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF });
            WriteInt(int.MinValue).Should().BeEquivalentTo(new byte[] { 0x80, 0x00, 0x00, 0x00 });
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

        private byte[] MockResponse(string fileName = "file.cpp")
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 1 issue
                Protocol.WriteUTF(writer, "message");

                Protocol.WriteUTF(writer, "ruleKey");
                Protocol.WriteUTF(writer, fileName);
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

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 0 fixes
                writer.Write(false);
                Protocol.WriteInt(writer, 0);

                // 1 measure
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, fileName);
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

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 0 fixes
                writer.Write(false);
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

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 0 fixes
                writer.Write(false);
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

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 0 fixes
                writer.Write(false);
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

        private byte[] MockResponseWithQuickFixes()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 1 issue
                Protocol.WriteUTF(writer, "message");

                Protocol.WriteUTF(writer, "ruleKey");
                Protocol.WriteUTF(writer, "cpp1.cpp");
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

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 1 fix
                writer.Write(true);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "Fix message");
                // 1 fix edit
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1); // start line
                Protocol.WriteInt(writer, 2); // end line
                Protocol.WriteInt(writer, 3); // start column
                Protocol.WriteInt(writer, 4); // end column
                Protocol.WriteUTF(writer, "Edit message");

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

        private byte[] MockResponseWithDataFlow()
        {
            string fileName = "file.cpp";
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 1 issue
                Protocol.WriteUTF(writer, "message");

                Protocol.WriteUTF(writer, "ruleKey");
                Protocol.WriteUTF(writer, fileName);
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

                // 1 Data Flow
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "DataFlow 1");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "another.cpp");
                Protocol.WriteInt(writer, 24);
                Protocol.WriteInt(writer, 25);
                Protocol.WriteInt(writer, 26);
                Protocol.WriteInt(writer, 27);
                Protocol.WriteUTF(writer, "Data Flow message");

                // 0 fixes
                writer.Write(false);
                Protocol.WriteInt(writer, 0);

                // 1 measure
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, fileName);
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
