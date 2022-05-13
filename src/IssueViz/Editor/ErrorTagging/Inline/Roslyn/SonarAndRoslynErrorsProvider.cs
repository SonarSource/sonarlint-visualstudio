using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline.Roslyn
{
    public interface ISonarAndRoslynErrorsProvider
    {
        event EventHandler<IssuesChanged> IssuesChanged;
    }

    public class IssuesChanged
    {
        public ITableEntriesSnapshotFactory Factory { get; }

        public IssuesChanged(ITableEntriesSnapshotFactory factory)
        {
            Factory = factory;
        }
    }

    [Export(typeof(ISonarAndRoslynErrorsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SonarAndRoslynErrorsProvider : ISonarAndRoslynErrorsProvider, ITableDataSink, IDisposable
    {
        private readonly ITableManager tableManager;
        private IDisposable roslynSinkHandle;
        private IDisposable sonarSinkHandle;

        [ImportingConstructor]
        public SonarAndRoslynErrorsProvider(ITableManagerProvider tableManagerProvider)
        {
            tableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            tableManager.SourcesChanged += TableManager_SourcesChanged;

            FetchSources();
        }

        private void FetchSources()
        {
            var roslynDataSource = tableManager.Sources.FirstOrDefault(x => x.DisplayName == "C#/VB Diagnostics Table Data Source");
            roslynSinkHandle?.Dispose();
            roslynSinkHandle = roslynDataSource?.Subscribe(this);

            var sonarDataSource = tableManager.Sources.FirstOrDefault(x => x.DisplayName == SonarLintTableControlConstants.ErrorListDataSourceIdentifier);
            sonarSinkHandle?.Dispose();
            sonarSinkHandle = sonarDataSource?.Subscribe(this);
        }

        private void TableManager_SourcesChanged(object sender, EventArgs e)
        {
            FetchSources();
        }
        
        public void Dispose()
        {
            tableManager.SourcesChanged -= TableManager_SourcesChanged;
        }

        public void AddEntries(IReadOnlyList<ITableEntry> newEntries, bool removeAllEntries = false)
        {
        }

        public void RemoveEntries(IReadOnlyList<ITableEntry> oldEntries)
        {
        }

        public void ReplaceEntries(IReadOnlyList<ITableEntry> oldEntries, IReadOnlyList<ITableEntry> newEntries)
        {
        }

        public void RemoveAllEntries()
        {
        }

        public void AddSnapshot(ITableEntriesSnapshot newSnapshot, bool removeAllSnapshots = false)
        {
        }

        public void RemoveSnapshot(ITableEntriesSnapshot oldSnapshot)
        {
        }

        public void RemoveAllSnapshots()
        {
        }

        public void ReplaceSnapshot(ITableEntriesSnapshot oldSnapshot, ITableEntriesSnapshot newSnapshot)
        {
        }

        public void AddFactory(ITableEntriesSnapshotFactory newFactory, bool removeAllFactories = false)
        {
        }

        public void RemoveFactory(ITableEntriesSnapshotFactory oldFactory)
        {
        }

        public void ReplaceFactory(ITableEntriesSnapshotFactory oldFactory, ITableEntriesSnapshotFactory newFactory)
        {
        }

        public void FactorySnapshotChanged(ITableEntriesSnapshotFactory factory)
        {
            IssuesChanged?.Invoke(this, new IssuesChanged(factory));
        }

        public void RemoveAllFactories()
        {
        }

        public bool IsStable { get; set; } = true;
        public event EventHandler<IssuesChanged> IssuesChanged;
    }
}
