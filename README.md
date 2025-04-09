# Dangl.AVACloudClientGenerator

[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=Dangl.AVACloudClientGenerator/develop)](https://jenkins.dangl.me/job/Dangl.AVACloudClientGenerator/job/develop/)

## CLI Usage

You can use the code generator from the command line. It is available for download in the GitHub Release
section of this repository. To run it, it requires `dotnet` to be installed.  
Example:

    dotnet Dangl.AVACloudClientGenerator.dll -l <Language> -o <OutputFolder>

| Parameter | Description |
|-----------|-------------|
| -o        | Path where to save the generated client to |
| -l        | Language of the client. See below for available options |
| -u        | Optional. Url of the Swagger definitions document. Defaults to 'https://avacloud-api.dangl-it.com/swagger/swagger.json' |
| -d        | Optional. Use local Docker containers for the generation. This is required for some languages |
| -a        | Optional url to the OpenAPI generator client gen endpoint. Will be ignored if using a local Docker generator |
| -s        | Optional url to the swagger generator client gen endpoint. Will be ignored if using a local Docker generator |
| --help    | Display options |

### Supported Languages

Currently, the converter supports the following values for the `language` argument:

| Parameter | Language |
|-----------|----------|
| `Java`    | Produces a Java 8 compatible client |
| `TypeScriptNode`    | Produces a TypeScript / JavaScript package compatible with the NodeJs runtime and Browser environments |
| `JavaScript`    | Produces a JavaScript package to be used in browsers |
| `Php`    | Produces a Php client |
| `Python`    | Produces a Python client |
| `Dart`    | Produces a Dart client |

## Build Target

By executing the following command in the project root, all available clients are generated:

    powershell ./build.ps1 GenerateClients

You can optionally pass the `CustomSwaggerDefinitionUrl`, e.g.:

    powershell ./build.ps1 GenerateClients -CustomSwaggerDefinitionUrl https://avacloud-api-dev.dangl-it.com/swagger/swagger.json

## Client Distribution

### Java

The Java client is available for download in this repositories Releases section on GitHub.

### TypeScripe Node

The TypeScript / JavaScript client for NodeJs is [published as npm package](https://www.npmjs.com/package/@dangl/avacloud-client-node) `@dangl/avacloud-client-node`.
It is generated and published by running the following build script:

    powershell ./build.ps1 GenerateAndPublishTypeScriptNpmClient

Use the optional `NodePublishVersionOverride` parameter to supply a custom version instead of syncing with the AVACloud version.

### JavaScript

The JavaScript client for Browsers is [published as npm package](https://www.npmjs.com/package/@dangl/avacloud-client-javascript) `@dangl/avacloud-client-javascript`
It is generated and published by running the following build script:

    powershell ./build.ps1 GenerateAndPublishJavaScriptNpmClient

Use the optional `NodePublishVersionOverride` parameter to supply a custom version instead of syncing with the AVACloud version.

### PHP

The PHP client is available for download in this repositories Releases section on GitHub.

Additionally, it's published on Packagist to be directly consumed via `composer.json`: [https://packagist.org/packages/dangl/avacloud](https://packagist.org/packages/dangl/avacloud)

The package is published by running this command:

    powershell ./build.ps1 GenerateAndPublishPhpClient

### Python

The Python client is available for download in this repositories Releases section on GitHub and on https://github.com/Dangl-IT/avacloud-client-python.

## Swagger API

Internally, it uses the [Swagger Generator](https://generator.swagger.io) to generate the [client API for Dangl.AVACloud](https://avacloud-api.dangl-it.com/swagger).

---
[License](./LICENSE.md)
