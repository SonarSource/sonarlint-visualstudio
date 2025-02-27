using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal interface ILegacySolutionBindingRepository
    {
        /// <summary>
        /// Retrieves solution binding information
        /// </summary>
        /// <returns>Can be null if not bound</returns>
        BoundSonarQubeProject Read(string configFilePath);
    }
}
