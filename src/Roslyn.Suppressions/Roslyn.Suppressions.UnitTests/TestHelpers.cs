/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Globalization;
using System.Threading;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    internal static class TestHelper
    {
        public static SuppressedIssue CreateIssue(string ruleId = "ruleId",
            string path = "path",
            int? startLine = 0,
            string hash = "hash")
        {
            return new SuppressedIssue(
                path,
                hash,
                RoslynLanguage.CSharp,
                ruleId,
                startLine);
        }

        public static SonarQubeIssue CreateSonarQubeIssue(string ruleId = "any",
            int? line = null,
            string filePath = "filePath",
            string hash = "hash") =>
            new SonarQubeIssue(
                "issuedId",
                filePath,
                hash,
                "message",
                "moduleKey",
                ruleId,
                false, // isResolved
                SonarQubeIssueSeverity.Info,
                System.DateTimeOffset.UtcNow,
                System.DateTimeOffset.UtcNow,
                line.HasValue ? new IssueTextRange(line.Value, line.Value, 1, 999) : null,
                null
                );
    }

    //This is to make sure normalising the keys done correctly with culture invariant
    //Lower case of SETTINGSKEY in Turkish is not settingskey but settıngskey
    //https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
    public class TemporaryCultureSwitch : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public TemporaryCultureSwitch(CultureInfo cultureInfo)
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        public TemporaryCultureSwitch(string cultureName) : this(new CultureInfo(cultureName)) { }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
