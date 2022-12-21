using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(IStatefulServerBranchProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    /// <summary>
    /// Stateful decorator for <see cref="ServerBranchProvider"/> that updates when the bound solution changes
    /// </summary>
    internal class CachingServerBranchProvider : IStatefulServerBranchProvider, IBoundSolutionObserver
    {
        private readonly IServerBranchProvider branchProvider;

        [ImportingConstructor]
        public CachingServerBranchProvider(IServerBranchProvider serverBranchProvider)
        {
            this.branchProvider = serverBranchProvider;
        }

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            // TODO: caching

            return await branchProvider.GetServerBranchNameAsync(token);
        }

        public void OnSolutionBindingChanged()
        {
            // TODO: cache refresh
        }
    }
}
