/*
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
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Education
{
    /// <summary>
    /// This is a helper class to encode a SonarCompositeRuleId into a valid URI and decode it vice versa.
    /// </summary>
    internal static class SonarRuleIdUriEncoderDecoder
    {
        private const string prefix = "sonarlintrulecrossref";

        public static Uri EncodeToUri(SonarCompositeRuleId compositeRuleId)
        {
            var link = $"{prefix}://{compositeRuleId.RepoKey}/{compositeRuleId.RuleKey}";

            return new Uri(link);
        }

        public static bool TryDecodeToCompositeRuleId(Uri uri, out SonarCompositeRuleId compositeRuleId)
        {
            compositeRuleId = null;

            // If the URI can be decoded it would be in the form of 'sonarlintrulecrossref://ruleRepo/ruleKey'
            if (uri.Scheme != prefix)
            {
                return false;
            }

            // It is not required to use uri.Host here. It would have been fine to use any
            // other property that had the information on the repository. 'host' is just the most simple.
            var ruleRepo = uri.Host;

            Debug.Assert(uri.AbsolutePath[0] == '/',
                $"Incorrect URI.AbsolutePath format for decoding to SonarCompositeRuleId. URI: {uri.AbsolutePath}");
            var ruleKey = uri.AbsolutePath.Substring(1);

            compositeRuleId = new SonarCompositeRuleId(ruleRepo, ruleKey);

            return true;
        }
    }
}
