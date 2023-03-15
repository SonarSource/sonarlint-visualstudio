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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Suppression;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    public interface IIssuesFilter
    {
        /// <summary>
        /// Returns the list of supplied objects that match the filter
        /// </summary>
        /// <remarks>The returned objects are a subset of the input objects i.e. no new objects will be returned</remarks>
        IEnumerable<IFilterableIssue> GetMatches(IEnumerable<IFilterableIssue> issues);
    }

    [Export(typeof(IIssuesFilter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class IssuesFilter : IIssuesFilter
    {
        private readonly ISuppressedIssueMatcher issueMatcher;

        [ImportingConstructor]
        public IssuesFilter(IServerIssuesStore serverIssuesStore)
            : this(new SuppressedIssueMatcher(serverIssuesStore))
        {
        }

        internal  /* for testing */ IssuesFilter(ISuppressedIssueMatcher issueMatcher)
        {
            this.issueMatcher = issueMatcher ?? throw new ArgumentNullException(nameof(issueMatcher));
        }

        public IEnumerable<IFilterableIssue> GetMatches(IEnumerable<IFilterableIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var matches = issues
                .Where(i => issueMatcher.SuppressionExists(i))
                .ToArray();
            return matches;
        }
    }
}
