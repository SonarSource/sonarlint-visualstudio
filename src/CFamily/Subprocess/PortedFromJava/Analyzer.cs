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

/**
 * This is a port of the protocol implemented between Java and C++
 * https://github.com/SonarSource/sonar-cpp/blob/master/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/analyzer/Analyzer.java
 */
namespace SonarLint.VisualStudio.CFamily.SubProcess
{
    internal class Response
    {
        public Message[] Messages { get; }

        public Response(Message[] messages)
        {
            Messages = messages;
        }
    }

    public class DataFlow
    {
        public string Description { get; }
        public MessagePart[] Steps { get; }

        public DataFlow(string description, MessagePart[] steps)
        {
            this.Description = description;
            this.Steps = steps;
        }
    }

    public class MessagePart
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

    public class Message : MessagePart
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

    public class Fix
    {
        public string Message { get; }
        public Edit[] Edits { get; }

        public Fix(string message, Edit[] edits)
        {
            Message = message;
            Edits = edits;
        }
    }

    public class Edit
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
