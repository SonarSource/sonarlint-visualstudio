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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    [ExcludeFromCodeCoverage]
    internal class RuleSplitDescription : IVisualNodeProducer
    {
        private readonly string introductionHtml;
        private readonly List<RuleDescriptionTab> tabs;

        public RuleSplitDescription(string introductionHtml, List<RuleDescriptionTab> tabs)
        {
            this.introductionHtml = introductionHtml;
            this.tabs = tabs;
        }

        public IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters)
        {
            throw new System.NotImplementedException();
        }
    }

    [ExcludeFromCodeCoverage]
    internal abstract class RuleDescriptionTab : IVisualNodeProducer
    {
        protected RuleDescriptionTab(string title)
        {
            Title = title;
        }

        public string Title { get; }
        public abstract IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters);
    }
    
    [ExcludeFromCodeCoverage]
    internal class NonContextualRuleDescriptionTab : RuleDescriptionTab
    {
        private readonly string htmlContent;

        public NonContextualRuleDescriptionTab(string title, string htmlContent) : base(title)
        {
            this.htmlContent = htmlContent;
        }

        public override IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters)
        {
            throw new System.NotImplementedException();
        }
    }
    
    [ExcludeFromCodeCoverage]
    internal class ContextualRuleDescriptionTab : RuleDescriptionTab
    {
        private readonly List<ContextContentTab> contexts;
        private readonly string defaultContext;
        
        public ContextualRuleDescriptionTab(string title, string defaultContext, List<ContextContentTab> contexts) : base(title)
        {
            this.contexts = contexts;
            this.defaultContext = defaultContext;
        }
        
        public override IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters)
        {
            throw new System.NotImplementedException();
        }
        
        internal class ContextContentTab 
        {
            public ContextContentTab(string title, string contextKey, string htmlContent)
            {
                Title = title;
                ContextKey = contextKey;
                HtmlContent = htmlContent;
            }

            public string Title { get; }
            public string ContextKey { get; }
            public string HtmlContent { get; }
        }
    }
}
