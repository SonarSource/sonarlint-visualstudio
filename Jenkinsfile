#!/bin/groovy
@Library('SonarSource@2.1.1') _

pipeline {
  agent none
  environment {         
    MAVEN_TOOL = 'Maven 3.3.x'
    GITHUB_TOKEN = credentials('sonartech-github-token')
  }
  stages {

    /* The PR build is being done by the Azure DevOps build definition
         https://sonarsource.visualstudio.com/DotNetTeam%20Project/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id=47
       However, the cix build is triggered at SonarSource organization level and is a
       required GitHub check. So, we want the build to send a "status ok" message to
       GitHub to unblock the PR, but nothing else.
    */
    stage('NotifyGitHubToSatisfyTheRequiredPRCheck')  {
      when { 
        environment name: 'IS_PULLREQUEST', value: 'true' 
      } 
      steps{	  
        githubNotifyBuildResult()
      }
    }
    
    stage('dummy build')  {
      steps{
        // Add some explanatory output for the CIX build
        echo
        echo
        echo '***********************************************************************'
        echo '***********************************************************************'
        echo 'This is a dummy build. The real build is performed using Azure DevOps.'
        echo 'See https://sonarsource.visualstudio.com/DotNetTeam%20Project/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id=47'
        echo '***********************************************************************'
        echo '***********************************************************************'
        echo
        echo
		// Send a build notification to burgr so there is at least a link in
		// the burgr UI to the dummy build on CIX.
		burgrNotifyBuildStarted()
		burgrNotifyBuildResult()
      }
    }
    
  }  
}
