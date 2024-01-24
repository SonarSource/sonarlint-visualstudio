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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.LibGit2SharpWrappers
{
    internal class CommitLogWrapperWithEnumerationCount : ICommitLog
    {
        private readonly IEnumerable<Commit> commits;

        public int EnumerateCount { 
            get; 
            private set; }

        public CommitLogWrapperWithEnumerationCount(IEnumerable<Commit> commits)
        {
            this.commits = commits;
            EnumerateCount = 0;
        }

        public CommitSortStrategies SortedBy => throw new System.NotImplementedException();

        public IEnumerator<Commit> GetEnumerator()
        {
            return GetCustomEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetCustomEnumerator();
        }

        private CommitLogEnumerator GetCustomEnumerator()
        {
            return new CommitLogEnumerator(commits.ToArray(), () => EnumerateCount++);
        }
    }

    internal class CommitLogEnumerator : IEnumerator, IEnumerator<Commit>
    {
        private Commit[] commits;

        int position = -1;
        Action incrementEnumCount;

        public CommitLogEnumerator(Commit[] commits, Action incrementEnumCount)
        {
            this.commits = commits;
            this.incrementEnumCount = incrementEnumCount;
        }

        public Commit Current
        {
            get
            {
                try
                {
                    return commits[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            position++;
            if (position < commits.Length)
            {
                incrementEnumCount();
                return true;
            }
            return false;
        }        

        public void Reset() => position = -1;
    }
}
