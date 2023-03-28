namespace SonarLint.VisualStudio.Rules
{
    public static class RuleInfoExtensions
    {
        public static bool IsRichRuleDescription(this IRuleInfo ruleInfo)
        {
            return ruleInfo.DescriptionSections != null && ruleInfo.DescriptionSections.Count > 0;
        }
    }
}
