//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetConflict.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that a <see cref="RuleConflictInfo"/> and also captures the information used to find that conflict
    /// </summary>
    /// <seealso cref="IRuleSetInspector"/>
    public class ProjectRuleSetConflict
    {
        public ProjectRuleSetConflict(RuleConflictInfo conflict, RuleSetAggregate aggregate)
        {
            if (conflict == null)
            {
                throw new ArgumentNullException(nameof(conflict));
            }

            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }


            this.Conflict = conflict;
            this.RuleSetInfo = aggregate;
        }

        public RuleSetAggregate RuleSetInfo { get; }

        public RuleConflictInfo Conflict { get; }
    }
}