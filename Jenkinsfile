pipeline {
    agent {
        node {
            label 'master'
            customWorkspace 'workspace/Dangl.AVACloudClientGenerator'
        }
    }
    environment {
        KeyVaultBaseUrl = credentials('AzureCiKeyVaultBaseUrl')
        KeyVaultClientId = credentials('AzureCiKeyVaultClientId')
        KeyVaultClientSecret = credentials('AzureCiKeyVaultClientSecret')
    }
    stages {
        stage ('Test') {
            steps {
                powershell './build.ps1 Test -Configuration Debug'
            }
            post {
                always {
                    warnings(
                        canComputeNew: false,
                        canResolveRelativePaths: false,
                        categoriesPattern: '',
                        consoleParsers: [[parserName: 'MSBuild']],
                        defaultEncoding: '',
                        excludePattern: '',
                        healthy: '',
                        includePattern: '',
                        messagesPattern: '',
                        unHealthy: '')
                    openTasks(
                        canComputeNew: false,
                        defaultEncoding: '',
                        excludePattern: '',
                        healthy: '',
                        high: 'HACK, FIXME',
                        ignoreCase: true,
                        low: '',
                        normal: 'TODO',
                        pattern: '**/*.cs, **/*.g4, **/*.ts, **/*.js',
                        unHealthy: '')
                    xunit(
                        testTimeMargin: '3000',
                        thresholdMode: 1,
                        thresholds: [
                            failed(failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0'),
                            skipped(failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0')
                        ],
                        tools: [
                            xUnitDotNet(deleteOutputFiles: true, failIfNotNew: true, pattern: '**/*testresults.xml', stopProcessingIfError: true)
                        ])
                }
            }
        }
        stage ('Publish GitHub Release') {
            steps {
                powershell './build.ps1 Publish'
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