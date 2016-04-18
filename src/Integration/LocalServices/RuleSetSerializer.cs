//-----------------------------------------------------------------------
// <copyright file="RuleSetSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace SonarLint.VisualStudio.Integration
{
    internal sealed class RuleSetSerializer : IRuleSetSerializer
    {
        private readonly IServiceProvider serviceProvider;

        public RuleSetSerializer(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        public RuleSet LoadRuleSet(string ruleSetPath)
        {
            if (string.IsNullOrWhiteSpace(ruleSetPath))
            {
                throw new ArgumentNullException(nameof(ruleSetPath));
            }

            var fileSystem = this.serviceProvider.GetService<IFileSystem>();
            fileSystem.AssertLocalServiceIsNotNull();

            if (fileSystem.FileExist(ruleSetPath))
            {
                try
                {
                    return RuleSet.LoadFromFile(ruleSetPath);
                }
                catch (Exception ex) when (ex is InvalidRuleSetException || ex is XmlException || ex is IOException)
                {
                    // Log this for testing purposes
                    Trace.WriteLine(ex.ToString(), nameof(LoadRuleSet));
                }
            }

            return null;

        }

        public void WriteRuleSetFile(RuleSet ruleSet, string ruleSetPath)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            if (string.IsNullOrWhiteSpace(ruleSetPath))
            {
                throw new ArgumentNullException(nameof(ruleSetPath));
            }

            ruleSet.WriteToFile(ruleSetPath);
        }
    }
}
