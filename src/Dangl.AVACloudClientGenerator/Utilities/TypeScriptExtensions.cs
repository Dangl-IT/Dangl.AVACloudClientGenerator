using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dangl.AVACloudClientGenerator.Utilities
{
    public static class TypeScriptExtensions
    {
        public static string GenerateMethodOverloadsWithOptionsObject(this string originalCode)
        {
            var lines = Regex.Split(originalCode, "\r\n?|\n");

            var updatedApiContent = string.Empty;
            var currentComment = string.Empty;
            var inComment = false;
            foreach (var line in lines)
            {
                if (line.Trim() == "/**")
                {
                    currentComment = line + Environment.NewLine;
                    inComment = true;
                    continue;
                }

                if (line.Trim() == "*/")
                {
                    currentComment += line + Environment.NewLine;
                    inComment = false;
                    continue;
                }

                if (inComment && !string.IsNullOrWhiteSpace(currentComment))
                {
                    currentComment += line + Environment.NewLine;
                    continue;
                }

                if (LineIsLikelyMethodDefinition(line) && TryGenerateMethodWithRequestObject(line, out var generatedMethod))
                {
                    updatedApiContent += "    /**" + Environment.NewLine;
                    updatedApiContent += "    * This is a generated method that accepts an options object, it simply calls the generated method with the parameter list" + Environment.NewLine;
                    updatedApiContent += "    */" + Environment.NewLine;
                    updatedApiContent += generatedMethod + Environment.NewLine;
                    if (!string.IsNullOrWhiteSpace(currentComment))
                    {
                        updatedApiContent += currentComment;
                        currentComment = string.Empty;
                    }

                    updatedApiContent += line;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(currentComment))
                    {
                        updatedApiContent += currentComment;
                        currentComment = string.Empty;
                    }

                    updatedApiContent += line + Environment.NewLine;
                }
            }

            return updatedApiContent;
        }

        private static bool LineIsLikelyMethodDefinition(string line)
        {
            return !line.TrimStart().StartsWith("constructor")
                && Regex.IsMatch(line, "^\\s+(public\\s+)?[a-z][a-zA-Z]+\\s?\\(");
        }

        private static bool TryGenerateMethodWithRequestObject(string line, out string method)
        {
            method = string.Empty;

            var hasOptions = line.Contains("options: any = {}");
            if (hasOptions)
            {
                line = line.Replace(", options: any = {}", string.Empty);
            }

            var parameterDeclaration = Regex.Match(line, "\\((.+)\\)").Value;
            var parameters = Regex.Matches(parameterDeclaration, "[a-zA-Z]+[?]?:(\\s[a-zA-Z0-9'_]+\\s?[|]?)+");
            if (parameters.Count < 2)
            {
                return false;
            }

            var methodName = Regex.Match(line, "^\\s+(public\\s+)?[a-z][a-zA-Z]+").Value.Trim();

            var hasUsedPublicIdentifier = false;
            if (methodName.StartsWith("public"))
            {
                hasUsedPublicIdentifier = true;
                methodName = methodName.Substring("public".Length).Trim();
            }

            if (methodName == "resolve"
                || methodName == "reject"
                || methodName == "setApiKey")
            {
                return false;
            }

            var returnObject = Regex.Match(line, ":\\s[a-zA-Z<>{}:;.? ]+\\s*[{]\\s*$").Value
                .TrimStart(':')
                .Trim()
                .TrimEnd('{')
                .Trim();

            var requestObject = "{ ";
            for (var i = 1; i < parameters.Count; i++)
            {
                var parameter = parameters[i].Value.Replace(":", "?:");
                requestObject += parameter + ", ";
            }

            if (hasOptions)
            {
                requestObject += "options?: any, ";
            }

            requestObject = requestObject.TrimEnd().TrimEnd(',') + " }";

            method = $"    {(hasUsedPublicIdentifier ? "public " : string.Empty)}{methodName}WithRequestObject({parameters[0].Value}, options?: {requestObject}): {returnObject} {{{Environment.NewLine}";

            var callingParameters = parameters
                .Select(p => Regex.Match(p.Value, "^[a-zA-Z]+").Value)
                .Select((parameterName, i) => i == 0 ? parameterName : $"options?.{parameterName}")
                .Aggregate((c, n) => c + ", " + n);
            if (hasOptions)
            {
                callingParameters += ", options.options || {}";
            }

            method += $"      return this.{methodName}({callingParameters});{Environment.NewLine}";

            method += "    }" + Environment.NewLine;

            return true;
        }
    }
}
