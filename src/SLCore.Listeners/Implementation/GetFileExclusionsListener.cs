using System.ComponentModel.Composition;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class GetFileExclusionsListener : IGetFileExclusionsListener
{
    public Task<GetFileExclusionsResponse> GetFileExclusionsAsync(GetFileExclusionsParams parameters) => throw new NotImplementedException();
}
