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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class ChecksumCalculatorTests
    {
        [TestMethod]
        public void Checksum_ThrowsOnNull()
        {
            Exceptions.Expect<ArgumentNullException>(() => ChecksumCalculator.Calculate(null));
        }

        [TestMethod]
        public void Checksum_WhitespaceIsIgnored()
        {
            // 1. If the input only varies by whitespace then the checksums should be the same
            var checksum1 = ChecksumCalculator.Calculate("abc");
            ChecksumCalculator.Calculate(" a  b  \r\n\t c \n\n").Should().Be(checksum1);

            ChecksumCalculator.Calculate("\ra\rb\r\rc\r").Should().Be(checksum1);
            ChecksumCalculator.Calculate("\na\nb\n\nc\n").Should().Be(checksum1);
            ChecksumCalculator.Calculate("\r\na\r\nb\r\n\r\nc\r\n").Should().Be(checksum1);


            // 2. Logically, a whitespace-only string should have the same checksum as an empty string
            var emptyChecksum = ChecksumCalculator.Calculate("");
            var whitespaceOnlyChecksum = ChecksumCalculator.Calculate("\r \t\n\n\r\t  ");
            emptyChecksum.Should().Be(whitespaceOnlyChecksum);
        }

        [TestMethod]
        public void Checksum_ExpectedValues()
        {
            // The expected values for this test were generated using a small Java app that used the same code as SLI/SLE to encode the strings.
            // See https://github.com/SonarSource/sonarlint-intellij/blob/6b0431be1cbdd892e310a6aedbc81ec5c468b6f9/src/main/java/org/sonarlint/intellij/issue/LiveIssue.java#L92
            // for the implementation as at 09 September 2017

            // Alpha-numerical
            AssertExpectedChecksum("1234567890abcdefghijklmnopqrstuvwxyz", "928f7bcdcd08869cc44c1bf24e7abec6");
            AssertExpectedChecksum("1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ", "b8ea760cc575698b7aaf5afadd72f639");

            // Punctuation
            AssertExpectedChecksum("`-=¬!\"£$%^&*()_+)[];'#,./{}:@~<>?\\|", "3468fe90cd69365b03bbf015dd6374c3");

            // Other special characters
            AssertExpectedChecksum("\t\n\r\f\b\u0001\u058F\u319F\u23F0", "5378ea4677edcf84c17a91053a3fb0f9");
            AssertExpectedChecksum("àÀáÁâÂèÈéÉêÊíÍòÒóÓôÔçÇùÙúÚûÛ", "55105c3496f8eb5ec78ab14304a5b96f");

            // Empty string
            AssertExpectedChecksum("", "d41d8cd98f00b204e9800998ecf8427e");
        }

        [TestMethod]
        public void Checksum_RealIssueData()
        {
            // Real issues taken from an analysis of Akka.Net
            AssertExpectedChecksum("        private HeliosTransport _transport;", "d60a97d7b258e0b4dfac90a65543eb50");

            AssertExpectedChecksum("                Console.WriteLine(\"Performance benchmark starting...\");",
                "7e42b5c3c797cb1e18c7d2d0ded681f5");

            AssertExpectedChecksum("Console.WriteLine(\"All actors have been initialized...\");",
                "a458ed466cea5d4a81253f622683a8f5");
            
            AssertExpectedChecksum("Console.WriteLine($\"{ActorCount} actors stored {MessagesPerActor} events each in {elapsed/1000.0} sec. Average: {ActorCount*MessagesPerActor*1000.0/elapsed} events/sec\"); ",
                "d9ca5e3753b12fd89b31948418264426");

            AssertExpectedChecksum("// TODO: SSL handlers", "ab08fdbcb40c33996e5469f7bedcc241");

            AssertExpectedChecksum("if (Settings.ReceiveBufferSize != null) client.Option(ChannelOption.SoRcvbuf, (int)(Settings.ReceiveBufferSize));",
                "861aa9c1f5bfd6412a7fd7c2f31b97d0");

            AssertExpectedChecksum("if (Settings.SendBufferSize != null) client.Option(ChannelOption.SoSndbuf, (int)(Settings.SendBufferSize));",
                "c9c77be96622f74fa6a2287c2be39d0f");

            AssertExpectedChecksum("if (Settings.WriteBufferHighWaterMark != null) client.Option(ChannelOption.WriteBufferHighWaterMark, (int)(Settings.WriteBufferHighWaterMark));",
                "89c8ae0663b409561a344975aa643e87");

            AssertExpectedChecksum("throw new NotImplementedException(\"Haven't implemented UDP transport at this time\");",
                "69e463bf530ff3522f3cb0a7e55a40f7");
        }


        private static void AssertExpectedChecksum(string text, string expected)
        {
            string checksum = ChecksumCalculator.Calculate(text);
            Assert.AreEqual(expected, checksum, $"Unexpected checksum. Input text: {text}");
        }
    }
}
