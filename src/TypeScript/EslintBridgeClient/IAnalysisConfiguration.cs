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

using System;
using System.Linq;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IAnalysisConfiguration
    {
        string[] GetGlobals();
        string[] GetEnvironments();
    }

    internal class AnalysisConfiguration : IAnalysisConfiguration
    {
        internal const string EnvironmentsVarName = "SONARLINT_JAVASCRIPT_ENVIRONMENTS";
        internal const string EnvironmentsDefaultValue =
            "amd, applescript, atomtest, browser, commonjs, couch, embertest, flow, greasemonkey, jasmine, jest, jquery, " +
            "meteor, mocha, mongo, nashorn, node, phantomjs, prototypejs, protractor, qunit, rhino, serviceworker, shared-node-browser, shelljs, webextensions, worker, wsh, yui";

        internal const string GlobalsVarName = "SONARLINT_JAVASCRIPT_GLOBALS";
        internal const string GlobalsDefaultValue = "angular,goog,google,OpenLayers,d3,dojo,dojox,dijit,Backbone,moment,casper";

        public string[] GetGlobals() =>
            GetStringArray(Environment.GetEnvironmentVariable(GlobalsVarName) ?? GlobalsDefaultValue);

        public string[] GetEnvironments() =>
            GetStringArray(Environment.GetEnvironmentVariable(EnvironmentsVarName) ?? EnvironmentsDefaultValue);

        private string[] GetStringArray(string settingArray)
        {
            return settingArray.Split(',').Select(x => x.Trim()).ToArray();
        }
    }
}
