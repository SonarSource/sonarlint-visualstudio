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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.ServerSentEvents;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.ServerSentEvents
{
    [TestClass]
    public class TaintServerEventsListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintServerEventsListener, ITaintServerEventsListener>(
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
                MefTestHelpers.CreateExport<ITaintStore>(),
                MefTestHelpers.CreateExport<ITaintServerEventSource>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<ILogger>());
        }
    }
}
