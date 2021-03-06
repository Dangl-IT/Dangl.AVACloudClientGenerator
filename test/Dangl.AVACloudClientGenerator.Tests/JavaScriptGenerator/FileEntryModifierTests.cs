﻿using Dangl.AVACloudClientGenerator.JavaScriptGenerator;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AVACloudClientGenerator.Tests.JavaScriptGenerator
{
    public class FileEntryModifierTests
    {
        [Fact]
        public async Task ReplacesConstructFromObjectMethodForIElement()
        {
            var sourceStream = GetSourceZipArchiveStream();
            var modifiedStream = await new FileEntryModifier(sourceStream).UpdatePackageJsonAndFixInheritanceAsync();
            var javaScriptFileContent = GetIElementDtoJavaScriptFile(modifiedStream);

            Assert.Contains("return data;", javaScriptFileContent); // To ensure the function is altered to return the original element
            Assert.DoesNotContain("obj['id']", javaScriptFileContent); // To ensure the original property assignment is no longer present
        }

        private Stream GetSourceZipArchiveStream()
        {
            var memoryStream = new MemoryStream();
            using (var zipArchive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                var packageJsonEntry = zipArchive.CreateEntry("package.json");
                using (var packageJsonStream = packageJsonEntry.Open())
                {
                    using (var streamWriter = new StreamWriter(packageJsonStream))
                    {
                        streamWriter.Write(GetPackageJsonContent());
                    }
                }

                var iElementDtoEntry = zipArchive.CreateEntry("IElementDto.js");
                using (var iElementDtoStream = iElementDtoEntry.Open())
                {
                    using (var streamWriter = new StreamWriter(iElementDtoStream))
                    {
                        streamWriter.Write(GetIElementDtoContent());
                    }
                }

                var apiClientEntry = zipArchive.CreateEntry("ApiClient.js");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private string GetPackageJsonContent()
        {
            return @"{
  ""name"": ""ava_cloud_api_130"",
  ""version"": ""1.3.0"",
  ""description"": ""AVACloud_API_specification"",
  ""license"": ""Unlicense"",
  ""main"": ""src/index.js"",
  ""scripts"": {
    ""test"": ""./node_modules/mocha/bin/mocha --recursive""
  },
  ""browser"": {
    ""fs"": false
  },
  ""dependencies"": {
    ""superagent"": ""3.5.2""
  },
  ""devDependencies"": {
    ""mocha"": ""~2.3.4"",
    ""sinon"": ""1.17.3"",
    ""expect.js"": ""~0.3.1""
  }
}
";
        }

        private string GetIElementDtoContent()
        {
            return @"/**
 * AVACloud API 1.3.0
 * AVACloud API specification
 *
 * OpenAPI spec version: 1.3.0
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 *
 * Swagger Codegen version: 2.3.1
 *
 * Do not edit the class manually.
 *
 */

(function(root, factory) {
  if (typeof define === 'function' && define.amd) {
    // AMD. Register as an anonymous module.
    define(['ApiClient'], factory);
  } else if (typeof module === 'object' && module.exports) {
    // CommonJS-like environments that support module.exports, like Node.
    module.exports = factory(require('../ApiClient'));
  } else {
    // Browser globals (root is window)
    if (!root.AvaCloudApi130) {
      root.AvaCloudApi130 = {};
    }
    root.AvaCloudApi130.IElementDto = factory(root.AvaCloudApi130.ApiClient);
  }
}(this, function(ApiClient) {
  'use strict';

  /**
   * The IElementDto model module.
   * @module model/IElementDto
   * @version 1.3.0
   */

  /**
   * Constructs a new <code>IElementDto</code>.
   * @alias module:model/IElementDto
   * @class
   * @param id {String}
   * @param elementTypeDiscriminator {String}
   */
  var exports = function(id, elementTypeDiscriminator) {
    var _this = this;

    _this['id'] = id;

    _this['elementTypeDiscriminator'] = elementTypeDiscriminator;
  };

  /**
   * Constructs a <code>IElementDto</code> from a plain JavaScript object, optionally creating a new instance.
   * Copies all relevant properties from <code>data</code> to <code>obj</code> if supplied or a new instance if not.
   * @param {Object} data The plain JavaScript object bearing properties of interest.
   * @param {module:model/IElementDto} obj Optional instance to populate.
   * @return {module:model/IElementDto} The populated <code>IElementDto</code> instance.
   */
  exports.constructFromObject = function(data, obj) {
    if (data) {
      obj = obj || new exports();

      if (data.hasOwnProperty('id')) {
        obj['id'] = ApiClient.convertToType(data['id'], 'String');
      }
      if (data.hasOwnProperty('gaebXmlId')) {
        obj['gaebXmlId'] = ApiClient.convertToType(data['gaebXmlId'], 'String');
      }
      if (data.hasOwnProperty('elementTypeDiscriminator')) {
        obj['elementTypeDiscriminator'] = ApiClient.convertToType(data['elementTypeDiscriminator'], 'String');
      }
    }
    return obj;
  }

  /**
   * @member {String} id
   */
  exports.prototype['id'] = undefined;
  /**
   * @member {String} gaebXmlId
   */
  exports.prototype['gaebXmlId'] = undefined;
  /**
   * @member {String} elementTypeDiscriminator
   */
  exports.prototype['elementTypeDiscriminator'] = undefined;

  return exports;
}));

";
        }

        private string GetIElementDtoJavaScriptFile(Stream modifiedStream)
        {
            using (var zipArchive = new System.IO.Compression.ZipArchive(modifiedStream))
            {
                var entry = zipArchive.Entries.First(e => e.FullName == "IElementDto.js");
                using (var entryStream = entry.Open())
                {
                    using (var streamReader = new StreamReader(entryStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
