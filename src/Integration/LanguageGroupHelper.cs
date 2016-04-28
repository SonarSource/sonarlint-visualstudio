//-----------------------------------------------------------------------
// <copyright file="LanguageGroupHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration
{
    internal static class LanguageGroupHelper
    {
        public static Language GetLanguage(LanguageGroup group)
        {
            switch(group)
            {
                case LanguageGroup.CSharp:
                    return Language.CSharp;

                case LanguageGroup.VB:
                    return Language.VBNET;

                case LanguageGroup.Unknown:
                    return Language.Unknown;

                default:
                    Debug.Fail("Unexpected group: " + group);
                    throw new InvalidOperationException();
            }
        }

        public static LanguageGroup GetLanguageGroup(Language language)
        {
            if (language == Language.CSharp)
            {
                return LanguageGroup.CSharp;
            }

            if (language == Language.VBNET)
            {
                return LanguageGroup.VB;
            }

            return LanguageGroup.Unknown;
        }

        public static LanguageGroup GetProjectGroup(Project project)
        {
            if (ProjectSystemHelper.IsCSharpProject(project))
            {
                return LanguageGroup.CSharp;
            }

            if (ProjectSystemHelper.IsVBProject(project))
            {
                return LanguageGroup.VB;
            }

            return LanguageGroup.Unknown;
        }
    }
}
