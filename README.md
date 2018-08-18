# Dangl.AVACloudClientGenerator

[![Built with Nuke](http://nuke.build/rounded)](https://www.nuke.build)  
[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=Dangl.AVACloudClientGenerator/develop)](https://jenkins.dangl.me/job/Dangl.AVACloudClientGenerator/job/develop/)

## CLI Usage

You can use the code generator from the command line. It is available for download in the GitHub Release
section of this repository. To run it, it requires `dotnet` to be installed.  
Example:

    dotnet Dangl.AVACloudClientGenerator.dll -l <Language >-o <OutputFolder>

| Parameter | Description |
|-----------|-------------|
| -o        | Path where to save the generated client to |
| -l        | Language of the client. See below for available options |
| --help    | Display options |

### Supported Languages

Currently, the converter supports the following values for the `language` argument:

| Parameter | Language |
|-----------|----------|
| `Java`    | Produces a Java 8 compatible client |
| `TypeScriptNode`    | Produces a TypeScript / JavaScript package compatible with the NodeJs runtime |

## Build Target

By executing the following command in the project root, all available clients are generated:

    powershell ./build.ps1 GenerateClients

## Swagger API

Internally, it uses the [Swagger Generator](https://generator.swagger.io) to generate the [client API for Dangl.AVACloud](https://avacloud-api.dangl-it.com/swagger-internal).

---
[License](./LICENSE.md)
