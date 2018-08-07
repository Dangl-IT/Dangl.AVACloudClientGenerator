# Dangl.AVACloudClientGenerator

[![Built with Nuke](http://nuke.build/rounded)](https://www.nuke.build)  

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

---
[License](./LICENSE.md)
