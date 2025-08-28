/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList;

public class Sink(ILogger perfLogger, string name, IThreadHandling threadHandling, IAnalysisStopwatchService analysisStopwatchService) : ITableDataSink
{
    public string Name { get; } = name;

    public void AddEntries(IReadOnlyList<ITableEntry> newEntries, bool removeAllEntries = false) {}

    public void RemoveEntries(IReadOnlyList<ITableEntry> oldEntries) {}

    public void ReplaceEntries(IReadOnlyList<ITableEntry> oldEntries, IReadOnlyList<ITableEntry> newEntries) {}

    public void RemoveAllEntries() {}

    public void AddSnapshot(ITableEntriesSnapshot newSnapshot, bool removeAllSnapshots = false)
    {
        if (newSnapshot.Count == 0)
        {
            return;
        }

        Log(newSnapshot);
    }

    private void Log(ITableEntriesSnapshot newSnapshot)
    {
        var currentTime = DateTime.Now;
        var (stopwatch, startTime) = analysisStopwatchService.Current;
        var elapsedTotalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

        threadHandling.RunOnBackgroundThread(() =>
        {
            var statsString = CountStats(newSnapshot);

            perfLogger.WriteLine("{2}, Update {3}: [ {4} ] | {0} -> {1}", startTime.ToString("O"), currentTime.ToString("O"), elapsedTotalMilliseconds, newSnapshot.Count, statsString);
        }).Forget();
    }

    public void RemoveSnapshot(ITableEntriesSnapshot oldSnapshot) {}

    public void RemoveAllSnapshots() {}

    public void ReplaceSnapshot(ITableEntriesSnapshot oldSnapshot, ITableEntriesSnapshot newSnapshot)
    {
        if (newSnapshot.Count - oldSnapshot.Count <= 0)
        {
            return;
        }


        Log(newSnapshot);
    }

    private static string CountStats(ITableEntriesSnapshot newSnapshot)
    {
        var stats = new Dictionary<string, int>();
        for (int i = 0; i < newSnapshot.Count; i++)
        {
            string id = "other";
            if (newSnapshot.TryGetValue(i ,StandardTableColumnDefinitions.ErrorCode, out var obj) && obj is string str && str[0] == 'S')
            {
                id = str;
            }

            if(!stats.ContainsKey(id))
            {
                stats[id] = 0;
            }
            stats[id]++;
        }

        var statsString = string.Join(", ", stats.OrderByDescending(x => x.Key).Select(x => $"{x.Key}: {x.Value}"));
        return statsString;
    }

    public void AddFactory(ITableEntriesSnapshotFactory newFactory, bool removeAllFactories = false)
    {

    }

    public void RemoveFactory(ITableEntriesSnapshotFactory oldFactory) {}

    public void ReplaceFactory(ITableEntriesSnapshotFactory oldFactory, ITableEntriesSnapshotFactory newFactory) {}

    public void FactorySnapshotChanged(ITableEntriesSnapshotFactory factory)
    {
        var tableEntriesSnapshot = factory.GetCurrentSnapshot();
        if (tableEntriesSnapshot.Count == 0)
        {
            return;
        }

        Log(tableEntriesSnapshot);

    }

    public void RemoveAllFactories() {}

    public bool IsStable { get; set; }
}
