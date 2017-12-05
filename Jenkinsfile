#!/bin/groovy
@Library('SonarSource@github') _

timestamps {
  stage('init')  {
    if (env.GITHUB_BRANCH != 'master' && !env.GITHUB_BRANCH.startsWith("PULLREQUEST-") && !env.GITHUB_BRANCH.startsWith("branch-")) {
        println 'not on master, branch-* or pullrequest'
        currentBuild.result = 'ABORTED'
        error('No build for branches')
    }else{
        println 'building'
    }
  }
  stage('NotifyStart')  {
    node('ops'){
        burgrNotifyBuildStarted()
        githubNotifyBuildPending()
    }
  }
  def status = 'failed'
  def state = 'failure'
  def triggerQA = 'master'.equals(env.GITHUB_BRANCH) || 'refs/heads/master'.equals(env.GITHUB_BRANCH) || 'true'.equals(env.IS_PULLREQUEST)
  try {
    parallel vs2015: {
      node('vs2015') {
        stage('Build on vs2015') {
          checkoutSCM()
          withCredentials([usernamePassword(credentialsId: '911c81fa-2ac1-4d0f-8223-6439a027e237', passwordVariable: 'GITHUB_TOKEN', usernameVariable: 'NOT_USED_1'), usernamePassword(credentialsId: '801cdaef-4a65-4edf-9a5d-9437f671d2a5', passwordVariable: 'PFX_PASSWORD', usernameVariable: 'NOT_USED_2')]) {
            withEnv(["CI_PRODUCT=jenkins", "CI_COMMIT=${env.GIT_SHA1}", "CI_REPO_SLUG=${env.GITHUB_REPO}", "CI_BUILD_NUMBER=${env.BUILD_NUMBER}", "CI_BRANCH=${env.GITHUB_BRANCH}"]) {
              build()
            }
          }
          stash('vs2015')
        }  
      }
    }, vs2017: {
      node('vs2017') {
        stage('Build on vs2017') {
          checkoutSCM()
          withCredentials([usernamePassword(credentialsId: '801cdaef-4a65-4edf-9a5d-9437f671d2a5', passwordVariable: 'PFX_PASSWORD', usernameVariable: 'NOT_USED_2')]) {
            build()
          }
          stash('vs2017')
        }
      }
    }
    if (triggerQA) {
      stage('Deploy') {
        node('linux') {
          checkoutSCM()
          unstash 'vsix-vs2015'
          unstash 'vsix-vs2017'
          def version = sh returnStdout: true, script: 'cat build/Version.props | grep MainVersion\\> | cut -d\'>\' -f 2 | cut -c 1-5'
          version = version.trim() + ".${env.BUILD_NUMBER}"
          echo "$version"
          dir('build/poms') {
            withMaven() {
              sh "mvn -B versions:set -DgenerateBackupPoms=false -DnewVersion=$version"
              withEnv(["ARTIFACTORY_DEPLOY_REPO=sonarsource-public-qa", "ARTIFACTORY_DEPLOY_USERNAME=public-qa-deployer", "ARTIFACTORY_DEPLOY_PASSWORD=LSfCL6vPuU6ZqvfJ", "PROJECT_VERSION=${version}", "BUILD_ID=${env.BUILD_NUMBER}"]) {
                sh "mvn deploy -Pdeploy-sonarsource -B -e -V"
              }
            }
          }
        }
      }
    }
    status = 'passed'
    state = 'success'
  } finally {
    stage('NotifyEnd') {
        node('ops'){
            burgrNotifyBuild(status)      
            githubNotifyBuild(state)
        }
    }
  }
  if (triggerQA) {
    build job: 'sonarlint-visualstudio-qa', parameters: [string(name: 'GIT_SHA1', value: "$GIT_SHA1"), string(name: 'CI_BUILD_NAME', value: "$CI_BUILD_NAME"), string(name: 'CI_BUILD_NUMBER', value: "$BUILD_NUMBER"), string(name: 'GITHUB_BRANCH', value: "$GITHUB_BRANCH"), string(name: 'GIT_URL', value: "$GIT_URL"), string(name: 'IS_PULLREQUEST', value: "$IS_PULLREQUEST"), string(name: 'PULL_REQUEST', value: "$PULL_REQUEST"), string(name: 'GITHUB_REPO', value: "$GITHUB_REPO"), string(name: 'GITHUB_REPOSITORY_OWNER_NAME', value: "$GITHUB_REPOSITORY_OWNER_NAME")],  wait: false
  }
}

def checkoutSCM() {
  //checkout scm
  checkout([$class: 'GitSCM', branches: [[name: '$GIT_SHA1']], doGenerateSubmoduleConfigurations: false, extensions: [[$class: 'CleanBeforeCheckout']], submoduleCfg: [], userRemoteConfigs: [[credentialsId: '765cc011-6f03-4509-992e-62b49c3fccfd', url: '$GIT_URL']]])
}

def build() {
  withJava() {
    bat script: 'ci-build.cmd'
  }
}

def stash(String node) {
  stash includes: 'binaries/*.vsix', name: "vsix-$node"
}

def withMaven(def body) {
  withJava() {
    def mvnHome = tool name: 'Maven 3.3.x', type: 'hudson.tasks.Maven$MavenInstallation'
    withEnv(["PATH+MAVEN=${mvnHome}/bin"]) {
      body.call()
    }
  }
}

def withJava(def body) {
  def javaHome = tool name: 'Java 8', type: 'hudson.model.JDK'
  withEnv(["JAVA_HOME=${javaHome}"]) {
    body.call()
  }
}
