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
        public void Write_UTF()
        {
            WriteUtf("").Should().BeEquivalentTo(new byte[] { 0, 0 });
            WriteUtf("a").Should().BeEquivalentTo(new byte[] { 0, 1, 97 });
            WriteUtf("A").Should().BeEquivalentTo(new byte[] { 0, 1, 65 });
            WriteUtf("0").Should().BeEquivalentTo(new byte[] { 0, 1, 48 });
            WriteUtf("\u0000").Should().BeEquivalentTo(new byte[] { 0, 2, 192, 128 });
            WriteUtf("\n").Should().BeEquivalentTo(new byte[] { 0, 1, 10 });
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
            WriteInt(int.MaxValue).Should().BeEquivalentTo(new byte[] { 127, 255, 255, 255 });
        }

        [TestMethod]
        public void Write_Long()
        {
            WriteLong(0).Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            WriteLong(long.MaxValue).Should().BeEquivalentTo(new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 });
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

        private byte[] WriteShort(ushort s)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteShort(writer, s);

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

        private byte[] WriteLong(long l)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteLong(writer, l);

                return stream.ToArray();
            }
        }

    }
}
