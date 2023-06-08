using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    public interface IConfigurationPersister
    {
        BindingConfiguration Persist(BoundSonarQubeProject project);
    }
}
