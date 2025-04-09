pipeline {
    options {
        disableConcurrentBuilds()
    }
    agent {
        node {
            label 'linux'
            customWorkspace 'workspace/Dangl.AVACloudClientGenerator'
        }
    }
    environment {
        KeyVaultBaseUrl = credentials('AzureCiKeyVaultBaseUrl')
        KeyVaultClientId = credentials('AzureCiKeyVaultClientId')
        KeyVaultClientSecret = credentials('AzureCiKeyVaultClientSecret')
        KeyVaultTenantId = credentials('AzureKeyVaultTenantId')
    }
    stages {
        stage ('Test') {
            steps {
                sh 'docker pull swaggerapi/swagger-generator:latest'
                sh 'docker pull openapitools/openapi-generator-online:latest'
                sh 'bash build.sh Test -Configuration Debug'
            }
            post {
                always {
                    recordIssues(
                        tools: [
                            msBuild(), 
                            taskScanner(
                                excludePattern: '**/*node_modules/**/*', 
                                highTags: 'HACK, FIXME', 
                                ignoreCase: true, 
                                includePattern: '**/*.cs, **/*.g4, **/*.ts, **/*.js', 
                                normalTags: 'TODO')
                            ])
                    xunit(
                        testTimeMargin: '3000',
                        thresholdMode: 1,
                        thresholds: [
                            failed(failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0'),
                            skipped(failureNewThreshold: '1', failureThreshold: '1', unstableNewThreshold: '1', unstableThreshold: '1')
                        ],
                        tools: [
                            xUnitDotNet(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/*testresults.xml', stopProcessingIfError: true)
                        ])
                }
            }
        }
        stage ('Publish GitHub Release') {
            steps {
                sh 'bash build.sh Publish'
            }
        }
    }
    post {
        always {
            step([$class: 'Mailer',
                notifyEveryUnstableBuild: true,
                recipients: "georg@dangl.me",
                sendToIndividuals: true])
        }
    }
}