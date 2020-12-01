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

using System.Collections.Generic;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.ViewModels
{
    /// <summary>
    /// Based on https://github.com/SonarSource/sonarqube/blob/master/server/sonar-web/src/main/js/helpers/standards.json#L3622
    /// </summary>
    internal class SecurityCategoryDisplayNames
    {
        public static readonly IDictionary<string, string> Mapping = new Dictionary<string, string>
        {
            {"buffer-overflow", "Buffer Overflow"},
            {"sql-injection", "SQL Injection"},
            {"rce", "Code Injection (RCE)"},
            {"object-injection", "Object Injection"},
            {"command-injection", "Command Injection"},
            {"path-traversal-injection", "Path Traversal Injection"},
            {"ldap-injection", "LDAP Injection"},
            {"xpath-injection", "XPath Injection"},
            {"expression-lang-injection", "Expression Language Injection"},
            {"log-injection", "Log Injection"},
            {"xxe", "XML External Entity (XXE)"},
            {"xss", "Cross-Site Scripting (XSS)"},
            {"dos", "Denial of Service (DoS)"},
            {"ssrf", "Server-Side Request Forgery (SSRF)"},
            {"csrf", "Cross-Site Request Forgery (CSRF)"},
            {"http-response-splitting", "HTTP Response Splitting"},
            {"open-redirect", "Open Redirect"},
            {"weak-cryptography", "Weak Cryptography"},
            {"auth", "Authentication"},
            {"insecure-conf", "Insecure Configuration"},
            {"file-manipulation", "File Manipulation"},
            {"others", "Others"}
        };
    }
}
