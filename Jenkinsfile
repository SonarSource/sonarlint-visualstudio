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
        echo 'This is a dummy build. The real build is performed using Azure DevOps.'
        echo 'See https://sonarsource.visualstudio.com/DotNetTeam%20Project/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id=47'
	  
        githubNotifyBuildResult()
      }
    }
	
	stage('dummy build')  {
      when { 
        not { 
          environment name: 'IS_PULLREQUEST', value: 'true' 
        }
      } 
      steps{
        echo 'This is a dummy build. The real build is performed using Azure DevOps.'
        echo 'See https://sonarsource.visualstudio.com/DotNetTeam%20Project/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id=47'
		
		// Don't send notifications anywhere
      }
    }
    
  }  
}



