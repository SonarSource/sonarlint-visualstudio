/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class ProtocolTest
    {
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
            Request request = new Request();
            request.Options = new string[] { "" };
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
            using (MemoryStream stream = new MemoryStream(MockEmptyResponse()))
            {
                BinaryReader reader = new BinaryReader(stream);
                Response response = Protocol.Read(reader);

                response.Messages.Length.Should().Be(0);
            }
        }

        [TestMethod]
        public void Read_Response()
        {
            using (MemoryStream stream = new MemoryStream(MockResponse()))
            {
                BinaryReader reader = new BinaryReader(stream);
                Response response = Protocol.Read(reader);

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
        }

        [TestMethod]
        public void Read_Bad_Response_Throw()
        {
            using (MemoryStream stream = new MemoryStream(MockBadStartResponse()))
            {
                BinaryReader reader = new BinaryReader(stream);

                Action act = () => Protocol.Read(reader);
                act.Should().ThrowExactly<InvalidDataException>();
            }

            using (MemoryStream stream = new MemoryStream(MockBadEndResponse()))
            {
                BinaryReader reader = new BinaryReader(stream);

                Action act = () => Protocol.Read(reader);
                act.Should().ThrowExactly<InvalidDataException>();
            }

        }

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

        private byte[] MockEmptyResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 0 issues
                Protocol.WriteInt(writer, 0);

                // 0 measures
                Protocol.WriteInt(writer, 0);

                // 0 symbols
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

                // 0 issues
                Protocol.WriteInt(writer, 0);

                // 0 measures
                Protocol.WriteInt(writer, 0);

                // 0 symbols
                Protocol.WriteInt(writer, 0);

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
                Protocol.WriteInt(writer, 1);

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
