using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    public static class LanguageHelper
    {
        public static ServerLanguage ToServerLanguage(this Language language)
        {
            if (language == Language.CSharp)
            {
                return ServerLanguage.CSharp;
            }
            else if (language == Language.VBNET)
            {
                return ServerLanguage.VbNet;
            }
            else
            {
                return null;
            }
        }
    }
}
