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
using System.IO;
using System.Text;

/**
 * This is a port of the protocol implemented between Java and C++
 * https://github.com/SonarSource/sonar-cpp/blob/master/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/analyzer/Protocol.java
 * 
 * Note: the ported code also needs to deal with the fact that the Java DataOutputStream/DataInputStreams are big-endian whereas
 *       the C# BinaryWriter/Reader are little-endian.
 */
namespace SonarLint.VisualStudio.CFamily.SubProcess
{
    internal static class Protocol
    {
        internal /* for testing */ static void WriteInt(BinaryWriter writer, int i)
        {
            // Big endian conversion
            byte[] temp = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            writer.Write(temp);
        }

        internal /* for testing */ static void WriteUTF(BinaryWriter writer, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            WriteInt(writer, bytes.Length);
            writer.Write(bytes);
        }

        internal /* for testing */ static string ReadUTF(BinaryReader reader)
        {
            int size = ReadInt(reader);
            return Encoding.UTF8.GetString(reader.ReadBytes(size));
        }

        internal /* for testing */ static int ReadInt(BinaryReader reader)
        {
            // Big endian conversion
            byte[] temp = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToInt32(temp, 0);
        }

        internal /* for testing */ static ushort ReadUShort(BinaryReader reader)
        {
            // Big endian conversion
            byte[] temp = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToUInt16(temp, 0);
        }

        /**
           * This method does not close the provided stream.
           */
        public static void Read(BinaryReader reader, Action<Message> handleIssue)
        {
            if ("OUT" != ReadUTF(reader))
            {
                throw new InvalidDataException("Communication issue with the C/C++ analyzer: OUT expected");
            }


            while (true) {
              switch (ReadUTF(reader)) {
              default:
                  throw new InvalidDataException("Communication issue with the C/C++ analyzer");
              case "message":
                  var message = readMessage(reader);
                  handleIssue(message);
                  break;
              case "measures":
                  // Skip measures
                  readMeasures(reader);
                  break;
              case "symbols":
                  // Skip symbols
                  readSymbols(reader);
                  break;
              case "END":
                  return;
              }
            }
        }

        private static Message readMessage(BinaryReader reader) {
            string ruleKey = ReadUTF(reader);
            string filename = ReadUTF(reader);
            int line = ReadInt(reader);
            int column = ReadInt(reader);
            int endLine = ReadInt(reader);
            int endColumn = ReadInt(reader);
            // Skip remediation cost
            ReadInt(reader);
            string text = ReadUTF(reader);
            bool partsMakeFlow = reader.ReadBoolean();
            MessagePart[] parts = ReadMessageParts(reader);
            _ = ReadDataFlows(reader);
            reader.ReadBoolean();
            Fix[] fixes = ReadFixes(reader);
            return new Message(ruleKey, filename, line, column, endLine, endColumn, text, partsMakeFlow, parts, fixes);
        }

        private static Fix[] ReadFixes(BinaryReader reader) {
            int fixesCount = ReadInt(reader);
            if (fixesCount == 0)
            {
                return Array.Empty<Fix>();
            }
            Fix[] fixes = new Fix[fixesCount];
            for (int i = 0; i < fixes.Length; i++)
            {
                fixes[i] = new Fix(
                  /* message= */ ReadUTF(reader),
                  ReadEdits(reader));
            }
            return fixes;
        }

        private static Edit[] ReadEdits(BinaryReader reader) {
            int editsCount = ReadInt(reader);
            Edit[] edits = new Edit[editsCount];
            for (int i = 0; i < edits.Length; i++)
            {
                edits[i] = new Edit(
                  /* startLine= */ ReadInt(reader),
                  /* startColumn= */ ReadInt(reader),
                  /* endLine= */ ReadInt(reader),
                  /* endColumn= */ ReadInt(reader),
                  /* text= */ ReadUTF(reader));
            }
            return edits;
        }

        private static MessagePart[] ReadMessageParts(BinaryReader reader)
        {
            int partsCount = ReadInt(reader);
            if (partsCount == 0)
            {
                return Array.Empty<MessagePart>();
            }
            MessagePart[] parts = new MessagePart[partsCount];
            for (int j = 0; j < parts.Length; j++)
            {
                parts[j] = new MessagePart(
                  /* filename= */ ReadUTF(reader),
                  /* line= */ ReadInt(reader),
                  /* column= */ ReadInt(reader),
                  /* endLine= */ ReadInt(reader),
                  /* endColumn= */ ReadInt(reader),
                  /* text= */ ReadUTF(reader));
            }
            return parts;
        }

        private static void readMeasures(BinaryReader reader) {
            int nbMeasures = ReadInt(reader);
            for (int i = 0; i < nbMeasures; i++)
            {
                /* filename */
                ReadUTF(reader);
                /* classes */
                ReadInt(reader);
                /* functions */
                ReadInt(reader);
                /* statements */
                ReadInt(reader);
                /* complexity */
                ReadInt(reader);
                /* cognitiveComplexity */
                ReadInt(reader);
                /* exec lines */
                reader.ReadBytes(ReadInt(reader));
            }
        }

        private static void readSymbols(BinaryReader reader) {
            int nbSymbols = ReadInt(reader);
            for (int i = 0; i < nbSymbols; i++)
            {
                int nbSymbolRefs = ReadInt(reader);
                for (int j = 0; j < nbSymbolRefs; j++)
                {
                    /* line */
                    ReadInt(reader);
                    /* column */
                    ReadInt(reader);
                    /* endLine */
                    ReadInt(reader);
                    /* endColumn */
                    ReadInt(reader);
                }
            }
        }

        private static DataFlow[] ReadDataFlows(BinaryReader reader) 
        {
            int flowCount = ReadInt(reader);
            if (flowCount == 0) { return null; }

            var dataFlows = new DataFlow[flowCount];
            for (int i = 0; i<flowCount; i++) 
            {
                dataFlows[i] = new DataFlow(ReadUTF(reader), ReadMessageParts(reader));
            }
            return dataFlows;
        }
    }
}
