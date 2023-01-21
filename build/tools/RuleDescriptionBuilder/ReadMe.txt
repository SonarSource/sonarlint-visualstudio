RuleDescriptionBuilder
======================

This is a build-time tool that converts rule help in JSON format to XAML that can be rendered in a
XAML DocumentReader/Viewer.

Assumptions:
* the analyzer JAR files have already been unloaded and the rule json extracted (one file per language)


Processing steps
----------------
1. Load the json file
2. Extract the HTML description for each rule
3. ?if necessary, fix up the HTML (e.g. remove &nbsp;)
4. Transform the HTML to XAML
5. Save the XAML to the destination directory
