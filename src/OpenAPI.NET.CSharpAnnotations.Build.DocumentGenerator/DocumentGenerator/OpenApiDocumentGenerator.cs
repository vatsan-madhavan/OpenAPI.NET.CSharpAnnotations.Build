// Copyright (c) Vatsan Madhavan. All rights reserved.

namespace OpenAPI.NET.CSharpAnnotations.Build
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.OpenApi;
    using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration;
    using Microsoft.OpenApi.Extensions;

    /// <summary>
    /// Generates Open API Document.
    /// </summary>
    public class OpenApiDocumentGenerator
    {
        /// <summary>
        /// Generates OpenAPI Documents (formerly Swagger docs) from C# documentation XML file annotations consistent with tags
        /// defined in the <a href="https://github.com/microsoft/OpenAPI.NET.CSharpAnnotations/wiki/C%23-Comment-Tag-Guide">OpenAPI.NET.CSharpAnnotations tag guide</a>.
        /// </summary>
        /// <param name="openApiDocumentVersion">OpenAPI Document version. This is the version of the document to produce and not the version of the OpenApi specification.</param>
        /// <param name="assemblyPaths">List of assemblies used to resolve types found in <paramref name="xmlDocumentationFiles"/>.</param>
        /// <param name="xmlDocumentationFiles">List of C# documentation XML files to process and transform into OpenAPI documents.</param>
        /// <param name="documentDescription">[Optional] OpenAPI document description.</param>
        /// <param name="outputPath">Path to folder where the OpenApi documents are produced.</param>
        /// <param name="openApiSpecVersion">OpenApi spec version. Defaults to <see cref="OpenApiSpecVersion.OpenApi3_0"/>.</param>
        /// <param name="outputFileNamePrefix">Prefix of the OpenApi filenames produced - defaults to 'OpenApiDocument'.</param>
        /// <param name="outputFormat">Default format of OpenApi documents - defaults to JSON.</param>
        /// <returns>List of paths to generated OpenAPI documents.</returns>
        public static IEnumerable<string> GenerateOpenApiDocuments(
            string openApiDocumentVersion,
            IEnumerable<string> assemblyPaths,
            IEnumerable<string> xmlDocumentationFiles,
            string documentDescription,
            string outputPath,
            OpenApiSpecVersion openApiSpecVersion = OpenApiSpecVersion.OpenApi3_0,
            string outputFileNamePrefix = "OpenApiDocument",
            OpenApiFormat outputFormat = OpenApiFormat.Json)
        {
            var generatorConfig = new OpenApiGeneratorConfig(
                GetXDocuments(xmlDocumentationFiles),
                assemblyPaths.ToList().AsReadOnly(),
                openApiDocumentVersion,
                FilterSetVersion.V1);

            // Should CamelCasePropertyNameResolver be used here?
            // Not sure - using default resolver for now.
            var openApiDocumentGenerationSettings =
                new OpenApiDocumentGenerationSettings(
                    new SchemaGenerationSettings(
                        new DefaultPropertyNameResolver()),
                    removeRoslynDuplicateStringFromParamName: true);

            var generator = new OpenApiGenerator();
            var results = generator.GenerateDocuments(
                generatorConfig,
                out var generatorDiagnostic,
                openApiDocumentGenerationSettings);

            if (generatorDiagnostic.DocumentGenerationDiagnostic?.Errors?.Count > 0 ||
                generatorDiagnostic.OperationGenerationDiagnostics?.Count > 0)
            {
                throw new OpenApiDocumentGeneratorException(generatorDiagnostic);
            }

            var openApiDocuments = new List<string>();
            foreach (var documentVariantInfo in results.Keys)
            {
                var openApiDocument = results[documentVariantInfo];
                if (!string.IsNullOrEmpty(documentDescription))
                {
                    openApiDocument.Info.Description = documentDescription;
                }

                var fileName = string.IsNullOrEmpty(documentVariantInfo.Title)
                    ? string.Join('.', outputFileNamePrefix, outputFormat.ToString())
                    : string.Join('.', outputFileNamePrefix, documentVariantInfo.Title, outputFormat.ToString());

                var fullFileName = Path.Combine(outputPath, fileName);
                var serializedDocument = OpenApiSerializableExtensions.Serialize(
                    openApiDocument,
                    openApiSpecVersion,
                    outputFormat);

                File.WriteAllText(fullFileName, serializedDocument);
                openApiDocuments.Add(fullFileName);
            }

            return openApiDocuments.AsEnumerable();
        }

        private static IList<XDocument> GetXDocuments(IEnumerable<string> xmlDocumentationFiles)
        {
            var xDocuments = new List<XDocument>();
            foreach (var xmlDocumentationFile in xmlDocumentationFiles)
            {
                xDocuments.Add(XDocument.Load(xmlDocumentationFile));
            }

            return xDocuments.AsReadOnly();
        }
    }
}
