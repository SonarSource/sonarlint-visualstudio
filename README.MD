<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://assets-eu-01.kc-usercontent.com/ef593040-b591-0198-9506-ed88b30bc023/a23fc7ba-23f0-489a-829d-ed88c0748521/Sonar_Logo_Dark%20Backgrounds.svg">
    <img src="https://assets-eu-01.kc-usercontent.com/ef593040-b591-0198-9506-ed88b30bc023/82c13eba-d95c-4bb8-8007-7ce77c14e043/Sonar_Logo_Light%20Backgrounds.svg" alt="Sonar logo" width="400">
  </picture>
</p>

# SonarQube for Visual Studio

<p>
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://assets-eu-01.kc-usercontent.com/ef593040-b591-0198-9506-ed88b30bc023/7934b7e0-d989-42aa-9c75-73af00b948b7/SQ_Logo_IDE_Dark%20Backgrounds.svg">
    <img src="https://assets-eu-01.kc-usercontent.com/ef593040-b591-0198-9506-ed88b30bc023/b839fc43-d33c-44b5-9e14-f9dee4e9a047/SQ_Logo_IDE_Light%20Backgrounds.png" alt="SonarQube for IDE logo" width="400">
  </picture>
</p>

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=sonarlint-visualstudio&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=sonarlint-visualstudio)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=sonarlint-visualstudio&metric=coverage)](https://sonarcloud.io/component_measures?id=sonarlint-visualstudio&metric=new_lines_to_cover&view=list)
[![GitHub stars](https://img.shields.io/github/stars/SonarSource/sonarlint-visualstudio?style=flat)](https://github.com/SonarSource/sonarlint-visualstudio)
[![Visual Studio Marketplace](https://img.shields.io/visual-studio-marketplace/v/SonarSource.SonarLintforVisualStudio2022)](https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2022)
[![Community forum](https://img.shields.io/badge/community-forum-blue)](https://community.sonarsource.com/c/sl/11)


SonarQube for IDE is a Visual Studio 2017, 2019, 2022 and 2026 extension that provides on-the-fly feedback to developers on new bugs and quality issues in C#, VB.NET, C/C++, TypeScript and JavaScript to help produce [integrated code quality and security](https://www.sonarsource.com/solutions/for-developers/).

This repository contains the source for the Visual Studio extension. It applies analysis to code written by developers and AI assistants, and it can connect to SonarQube Server or SonarQube Cloud to share team rules and settings.

SonarQube for Visual Studio is free, open source, and available in the Visual Studio Marketplace:
* [SonarQube for Visual Studio 2022 & 2026](https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2022)
* [SonarQube for Visual Studio 2019 (out of support)](https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2019)
* [SonarQube for Visual Studio 2017 (out of support)](https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2017)

Note: SonarQube for Visual Studio 2017 and 2019 has been deprecated. New versions of SonarQube for Visual Studio 2017 and 2019 will not be shipped after March 1, 2022 and April 2, 2024 respectively. 


## Features
* On-the-fly feedback
* Support C#, VB.Net, Javascript, C/C++ etc.
* Support for standalone analysis.
* Support for connected mode.

[Connected Mode](https://docs.sonarsource.com/sonarqube-for-visual-studio/connect-your-ide/connected-mode/) provides suppression of issues marked as **False Positive** or **Won't Fix** on SonarQube (Server, Cloud). Follow the link for more information.

## Useful links
* [SonarQube for Visual Studio documentation](https://docs.sonarsource.com/sonarqube-for-visual-studio/)
* [Community Forum](https://community.sonarsource.com/)
* [SonarQube for Visual Studio overview](https://www.sonarsource.com/products/sonarlint/features/visual-studio/)
* [Issue tracking](https://github.com/SonarSource/sonarlint-visualstudio/issues)

## Have questions or feedback?

The preferred way to discuss SonarQube for Visual Studio is by posting on the [SonarSource Community Forum](https://community.sonarsource.com/c/sl/11). Feel free to ask questions, report issues, and give suggestions.

**False positives:** please report false positives in analysis rules in [this section](https://community.sonarsource.com/c/bug/fp/7) of the community forum. 

## How to contribute

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## License

Copyright SonarSource.

Licensed under the [GNU Lesser General Public License, Version 3.0](http://www.gnu.org/licenses/lgpl.txt)
