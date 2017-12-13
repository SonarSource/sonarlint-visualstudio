#!/bin/groovy
@Library('SonarSource@github') _

pipeline {
  agent { 
    label 'linux' 
  }
  parameters {
    string(name: 'GIT_SHA1', defaultValue: 'master', description: 'Git SHA1 (provided by travisci hook job)')
    string(name: 'CI_BUILD_NAME', defaultValue: 'sonar-license', description: 'Build Name (provided by travisci hook job)')	
    string(name: 'CI_BUILD_NUMBER', description: 'Build Number (provided by travisci hook job)')
    string(name: 'GITHUB_BRANCH', defaultValue: 'master', description: 'Git branch (provided by travisci hook job)')
    string(name: 'GITHUB_REPOSITORY_OWNER', defaultValue: 'SonarSource', description: 'Github repository owner(provided by travisci hook job)')
  }
  environment {         
        MAVEN_TOOL = 'Maven 3.3.x'
        PFX_PASSWORD = credentials('pfx-passphrase')
        GITHUB_TOKEN = credentials('sonartech-github-token')
  }
  stages{
    stage('NotifyStart')  {
      steps{
        burgrNotifyBuildStarted()
        githubNotifyBuildPending()
      }
    }
    stage('Build') {
      parallel {
        stage('vs2015'){
          agent { 
            label 'vs2015' 
          }
          tools {
            jdk 'Java 8'
          }
          steps{
            bat script: 'ci-build.cmd'
            stash includes: 'binaries/*.vsix', name: "vsix-vs2015"
          }
        }
        stage('vs2017') {
          agent { 
            label 'vs2017' 
          }
          tools {
            jdk 'Java 8'
          }
          steps{
            bat script: 'ci-build.cmd'
            stash includes: 'binaries/*.vsix', name: "vsix-vs2017"
          }
        }
      }
      post {
        success {
          burgrNotifyBuildPassed()
          githubNotifyBuildSuccess()
        }
        failure {
          burgrNotifyBuildFailed()
          githubNotifyBuildFailed()
        }
        unstable {
          burgrNotifyBuildFailed()
          githubNotifyBuildFailed()
        }
        aborted {
          burgrNotifyBuildAborted()
          githubNotifyBuildError()
        }
      }       
    }     
      
    stage('Deploy') {   
      //'master'.equals(env.GITHUB_BRANCH) || 'refs/heads/master'.equals(env.GITHUB_BRANCH) || 'true'.equals(env.IS_PULLREQUEST)
      when{
        anyOf {
          environment name: 'GITHUB_BRANCH', value: 'master'
          environment name: 'IS_PULLREQUEST', value: 'true'
        }
      }     
      environment {         
        ARTIFACTORY_DEPLOY_REPO="sonarsource-public-qa"
        REPOX_DEPLOYER=credentials('repox-deploy')
        ARTIFACTORY_DEPLOY_USERNAME="$REPOX_DEPLOYER_USR"
        ARTIFACTORY_DEPLOY_PASSWORD="$REPOX_DEPLOYER_PSW"
        PROJECT_VERSION="${version}"
        BUILD_ID="${env.BUILD_NUMBER}"
      }
      steps{
        unstash 'vsix-vs2015'
        unstash 'vsix-vs2017'
        script {
          version = sh returnStdout: true, script: 'cat build/Version.props | grep MainVersion\\> | cut -d\'>\' -f 2 | cut -c 1-5'
          version = version.trim() + ".${env.BUILD_NUMBER}"
        } 
        echo "${version}"
        dir('build/poms') {
          withMaven(maven: MAVEN_TOOL) {
            sh "mvn -B versions:set -DgenerateBackupPoms=false -DnewVersion=${version}"
            sh "mvn deploy -Pdeploy-sonarsource -B -e -V"            
          }
        }             
      }       
      post {
        success {
          burgrNotifyBuildPassed()
          githubNotifyBuildSuccess()
        }
        failure {
          burgrNotifyBuildFailed()
          githubNotifyBuildFailed()
        }
        unstable {
          burgrNotifyBuildFailed()
          githubNotifyBuildFailed()
        }
        aborted {
          burgrNotifyBuildAborted()
          githubNotifyBuildError()
        }
      }
    }

    stage('QA')  {
      when{
        anyOf {
          environment name: 'GITHUB_BRANCH', value: 'master'
          environment name: 'IS_PULLREQUEST', value: 'true'
        }
      } 
      steps{
        //at the moment no QA is executed for sonarlint-visualstudio
        burgrNotifyQaPassed()
        githubNotifyQaSuccess()
      }
    } 

    stage('Promote') {
      when{
        anyOf {
          environment name: 'GITHUB_BRANCH', value: 'master'
          environment name: 'IS_PULLREQUEST', value: 'true'
        }
      } 
      steps {
        repoxPromoteBuild()
      }
      post {
        success {
          burgrNotifyPromotePassed()
          githubNotifyPromoteSuccess()
        }
        failure {
          burgrNotifyPromoteFailed()
          githubNotifyPromoteFailed()
        }
      }
    }     
  }  
}



