﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

/**
 * This is a port of the protocol implemented between Java and C++
 * https://github.com/SonarSource/sonar-cpp/blob/master/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/analyzer/Analyzer.java
 */
namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{

    internal partial class Request
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
        public const int SonarLint = 1 << 18;
        public const int BuildPreamble = 1 << 19;
        public const int MainFileIsHeader = 1 << 20;
        public const int CPlusPlus20 = 1 << 21;
        public const int NoDelayedTemplateParsing = 1 << 22;
        public const int AlignedNew = 1 << 23;

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

        // File and PchFile are also now part of the C# RequestContext class
        // TODO: refactor to remove them from this class
        public string File { get; set; } = "";
        public string PchFile { get; set; } = "";
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
        public Fix[] Fixes { get; }

        // SLVS: we expect the class to be JSON-serializable for the ease of testing, and therefore the ctor parameter names must match the property names
        // for the default Newtonsoft serializer to work correctly.
        public Message(string ruleKey, string filename, int line, int column, int endLine, int endColumn, string text, bool partsMakeFlow, MessagePart[] parts, Fix[] fixes)
            : base(filename, line, column, endLine, endColumn, text)
        {
            RuleKey = ruleKey;
            PartsMakeFlow = partsMakeFlow;
            Parts = parts;
            Fixes = fixes;
        }

    }

    internal class Fix
    {
        public string Message { get; }
        public Edit[] Edits { get; }

        public Fix(string message, Edit[] edits)
        {
            Message = message;
            Edits = edits;
        }
    }

    internal class Edit
    {
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }
        public string Text { get; }

        public Edit(int startLine, int startColumn, int endLine, int endColumn, string text)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Text = text;
        }
    }
}
