using Dangl.AVACloudClientGenerator.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.TypeScriptNodeGenerator
{
    public class CodeGeneratorTests
    {
        private readonly AVACloudVersion _avaCloudVersion = new AVACloudVersion();

        [Fact]
        public async Task DoesNotIncludeBufferAsClassOrReference()
        {
            // The API should always use a FileParameter
            var typeScriptNodeOptionsGenerator = new Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator.OptionsGenerator(_avaCloudVersion);
            var typeScriptNodeGenerator = new Dangl.AVACloudClientGenerator.TypeScriptNodeGenerator.CodeGenerator(typeScriptNodeOptionsGenerator, _avaCloudVersion);
            using (var zippedClientCodeStream = await typeScriptNodeGenerator.GetGeneratedCodeZipPackageAsync(Constants.COMPLETE_SWAGGER_DEFINITION_ENDPOINT))
            {
                using (var zipArchive = new ZipArchive(zippedClientCodeStream))
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        using (var entryStream = entry.Open())
                        {
                            using (var entryReader = new StreamReader(entryStream))
                            {
                                var entryText = await entryReader.ReadToEndAsync();
                                Assert.DoesNotContain("file?:Buffer", entryText);
                                Assert.DoesNotContain("file?: Buffer", entryText);
                                Assert.DoesNotContain("file:Buffer", entryText);
                                Assert.DoesNotContain("file: Buffer", entryText);
                                Assert.DoesNotContain("file? :Buffer", entryText);
                                Assert.DoesNotContain("file? : Buffer", entryText);
                                Assert.DoesNotContain("file :Buffer", entryText);
                                Assert.DoesNotContain("file : Buffer", entryText);
                                Assert.DoesNotContain("File?:Buffer", entryText);
                                Assert.DoesNotContain("File?: Buffer", entryText);
                                Assert.DoesNotContain("File:Buffer", entryText);
                                Assert.DoesNotContain("File: Buffer", entryText);
                                Assert.DoesNotContain("File? :Buffer", entryText);
                                Assert.DoesNotContain("File? : Buffer", entryText);
                                Assert.DoesNotContain("File :Buffer", entryText);
                                Assert.DoesNotContain("File : Buffer", entryText);
                            }
                        }
                    }
                }
            }
        }
    }
}
