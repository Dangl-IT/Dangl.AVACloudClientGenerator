using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;
using System.Collections.Generic;
using System.Linq;

namespace Dangl.AVACloudClientGenerator.TypeScriptFetchGenerator
{
    public class TypeScriptFetchClientOperationNameGenerator : IOperationNameGenerator
    {
        public bool SupportsMultipleClients => true;

        public string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
        {
            var splitNames = operation.OperationId.Split("_");
            return $"{splitNames.First()}Api";
        }

        public string GetOperationName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
        {
            var splitNames = operation.OperationId.Split("_");
            var operationName = $"{splitNames.First()}{splitNames.Last()}";
            GeneratedOperationNames.Add(operationName);
            return operationName;
        }

        public List<string> GeneratedOperationNames { get; } = new List<string>();
    }
}
