﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.Core
{
    public class SonarCompositeRuleId
    {
        private const string Separator = ":";

        /// <summary>
        /// Attempts to parse an error code from the VS Error List as a Sonar rule in the form "[repo key]:[rule key]"
        /// </summary>
        public static bool TryParse(string errorListErrorCode, out SonarCompositeRuleId ruleInfo)
        {
            ruleInfo = null;

            if (!string.IsNullOrEmpty(errorListErrorCode))
            {
                var keys = errorListErrorCode.Split(new string[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
                if (keys.Length == 2)
                {
                    ruleInfo = new SonarCompositeRuleId(errorListErrorCode, keys[0], keys[1]);
                }
            }

            return ruleInfo != null;
        }

        private SonarCompositeRuleId(string errorListErrorCode, string repoKey, string ruleKey)
        {
            ErrorListErrorCode = errorListErrorCode ?? throw new ArgumentNullException(nameof(errorListErrorCode));
            RepoKey = repoKey ?? throw new ArgumentNullException(nameof(repoKey));
            RuleKey = ruleKey ?? throw new ArgumentNullException(nameof(ruleKey));
        }

        public string ErrorListErrorCode { get; }
        public string RepoKey { get; }
        public string RuleKey { get; }
        public override string ToString() => ErrorListErrorCode;
    }
}
