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
using System.IO;
using System.IO.Abstractions;
using System.Xml;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    // TODO - CM cleanup - this class might be useful when cleaning up "old" Connected Mode settings.
    // 
    // The following classes are inter-related:
    // * RuleSetReferenceChecker
    // * SolutionRuleSetsInformationProvider
    // * RuleSetSerializer
    // They are not referenced from any other product code. If they are not needed for settings mode
    // cleanup then they can deleted as a group.


    internal sealed class RuleSetSerializer : IRuleSetSerializer
    {
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public RuleSetSerializer(ILogger logger)
            : this(logger, new FileSystem())
        {
        }

        internal RuleSetSerializer(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public RuleSet LoadRuleSet(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (fileSystem.File.Exists(path))
            {
                try
                {
                    return RuleSet.LoadFromFile(path);
                }
                catch (Exception ex) when (ex is InvalidRuleSetException || ex is XmlException || ex is IOException)
                {
                    logger.WriteLine(Strings.RulesetSerializer_FailedToLoadRuleset, path, ex.Message);
                }
            }
            else
            {
                logger.WriteLine(Strings.RulesetSerializer_RulesetDoesNotExist, path);
            }

            return null;
        }

        public void WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            ruleSet.WriteToFile(path);
        }
    }
}
