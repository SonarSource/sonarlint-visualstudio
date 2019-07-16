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
using System.Collections.Generic;
using System.IO;
using System.Text;

/**
 * This is a port of the protocol implemented between Java and C++
 * https://github.com/SonarSource/sonar-cpp/blob/master/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/analyzer/Protocol.java
 */
namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{

    internal class Request
    {
        public const int Verify = 1;
        public const int C99 = 1 << 1;
        public const int C11 = 1 << 2;
        public const int CPlusPlus = 1 << 3;
        public const int CPlusPlus11 = 1 << 4;
        public const int CPlusPlus14 = 1 << 5;
        public const int CPlusPlus17 = 1 << 6;
        public const int ObjC = 1 << 7;
        public const int MS = 1 << 8;
        public const int GNU = 1 << 9;
        public const int OperatorNames = 1 << 10;
        public const int CharIsUnsigned = 1 << 11;
        public const int CreateReproducer = 1 << 14;
        public const int AutoRefCount = 1 << 15;
        public const int Weak = 1 << 16;
        public const int C17 = 1 << 17;

        public string[] Options { get; set; } = Array.Empty<string>();
        public long Flags { get; set; }
        public long MsVersion { get; set; }
        public string[] IncludeDirs { get; set; } = Array.Empty<string>();
        public string[] FrameworkDirs { get; set; } = Array.Empty<string>();
        public string[] VfsOverlayFiles { get; set; } = Array.Empty<string>();
        public string ModuleName { get; set; } = "";
        public string Predefines { get; set; } = "";
        public string[] Macros { get; set; } = Array.Empty<string>();
        public string TargetTriple { get; set; } = "x86_64-unknown-unknown";
        public string File { get; set; } = "";
    }

    internal class Response
    {
        public Message[] Messages { get; }

        public Response(Message[] messages)
        {
            Messages = messages;
        }
    }

    internal class MessagePart
    {
        public string Filename;
        public int Line { get; }
        public int Column { get; }
        public int EndLine { get; }
        public int EndColumn { get; }
        public string Text { get; }

        public MessagePart(string filename, int line, int column, int endLine, int endColumn, string text)
        {
            Filename = filename;
            Line = line;
            Column = column;
            EndLine = endLine;
            EndColumn = endColumn;
            Text = text;
        }
    }

    internal class Message : MessagePart
    {

        public string RuleKey { get; }
        public bool PartsMakeFlow { get; }
        public MessagePart[] Parts { get; }

        public Message(string ruleKey, string filename, int line, int column, int endLine, int endColumn, string message, bool partsMakeFlow, MessagePart[] parts)
            : base(filename, line, column, endLine, endColumn, message)
        {
            RuleKey = ruleKey;
            PartsMakeFlow = partsMakeFlow;
            Parts = parts;
        }

    }

    internal static class Protocol
    {
        /**
          * This method does not close the provided stream.
          */
        public static void Write(BinaryWriter writer, Request request)
        {
            WriteUTF(writer, "IN");
            Write(writer, request.Options);
            WriteLong(writer, request.Flags);
            WriteLong(writer, request.MsVersion);
            Write(writer, request.IncludeDirs);
            Write(writer, request.FrameworkDirs);
            Write(writer, request.VfsOverlayFiles);
            WriteUTF(writer, request.ModuleName);
            WriteUTF(writer, request.Predefines);
            Write(writer, request.Macros);
            WriteUTF(writer, request.TargetTriple);
            WriteUTF(writer, request.File);
            WriteUTF(writer, "END");
        }

        internal /* for testing */ static void WriteLong(BinaryWriter writer, long l)
        {
            // Big endian conversion
            byte[] temp = BitConverter.GetBytes(l);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            writer.Write(temp);
        }

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

        internal /* for testing */ static void WriteUShort(BinaryWriter writer, ushort s)
        {
            // Big endian conversion
            byte[] temp = BitConverter.GetBytes(s);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }
            writer.Write(temp);
        }

        private static void Write(BinaryWriter writer, string[] strings)
        {
            WriteInt(writer, strings.Length);
            foreach (string value in strings)
            {
                WriteUTF(writer, value);
            }
        }

        internal /* for testing */ static void WriteUTF(BinaryWriter writer, string str)
        {
            foreach (char c in str)
            {
                if (Char.IsSurrogate(c))
                {
                    throw new InvalidOperationException("Surrogate characters are not supported");
                }
                if (c == '\0')
                {
                    throw new InvalidOperationException("NUL character is not supported");
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new InvalidOperationException($"String size is too big to be serialized: {bytes.Length}");
            }
            WriteUShort(writer, (ushort)bytes.Length);
            writer.Write(bytes);
        }
        internal /* for testing */ static string ReadUTF(BinaryReader reader)
        {
            ushort size = ReadUShort(reader);
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
        public static Response Read(BinaryReader reader)
        {

            if ("OUT" != ReadUTF(reader))
            {
                throw new InvalidDataException("Communication issue with the C/C++ analyzer: OUT expected");
            }

            Message[] messages = new Message[ReadInt(reader)];
            for (int i = 0; i < messages.Length; i++)
            {
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
                messages[i] = new Message(ruleKey, filename, line, column, endLine, endColumn, text, partsMakeFlow, parts);
            }
            // Skip measures
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

            // Skip symbols
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

            if ("END" != ReadUTF(reader))
            {
                throw new InvalidDataException("Communication issue with the C/C++ analyzer: END expected");
            }

            return new Response(messages);
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
    }
}
