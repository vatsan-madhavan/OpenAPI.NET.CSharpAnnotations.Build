// Copyright (c) Vatsan Madhavan. All rights reserved.

namespace OpenAPI.NET.CSharpAnnotations.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.OpenApi;

    /// <summary>
    /// MSBuild Task to generate an OpenAPI document.
    /// </summary>
    public class GenerateOpenApiDocumentTask : Task
    {
        /// <summary>
        /// Gets or sets the OpenAPI Document Version. This is the version of the document to be produced,
        /// and not the version of the OpenApi specification.
        /// </summary>
        [Required]
        public string OpenApiDocumentVersion { get; set; }

        /// <summary>
        /// Gets or sets the list of assemblies used to resolve the types found in <see cref="XmlDocumentationFile"/>.
        /// </summary>
        [Required]
        public ITaskItem[] AssemblyPath { get; set; }

        /// <summary>
        /// Gets or sets teh list of C# XML documentation files that wil be processed and transformed into OpenApi documents.
        /// </summary>
        [Required]
        public ITaskItem[] XmlDocumentationFile { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI document description.
        /// </summary>
        public string DocumentDescription { get; set; }

        /// <summary>
        /// Gets or sets the path to folder where the OpenAPI documents are produced.
        /// The default location when this property is unspecified is $(IntermediateOutputPath).
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the $(IntermediateOuputPath) value that's used as the default/fallback path
        /// for <see cref="OutputPath"/> when its value is not specified.
        /// </summary>
        [Required]
        public string IntermediateOutputPath { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI spec version.
        /// Defaults to "3.0". Valid values are {"2.0", "3.0"}.
        /// </summary>
        public string OpenApiSpecVersion { get; set; } = "3.0";

        /// <summary>
        /// Gets or sets the prefix of the OpenAPI filenames produced.
        /// Defaults to 'OpenApiDocument'.
        /// </summary>
        public string OutputFileNamePrefix { get; set; } = "OpenApiDocument";

        /// <summary>
        /// Gets or sets the default format of the OpenAPI documents generated.
        /// Defaults to JSON; valid values are {'JSON', 'YAML'}.
        /// </summary>
        public string OutputFormat { get; set; } = "JSON";

        /// <summary>
        /// Gets or sets the list of generated OpenAPI documents.
        /// </summary>
        [Output]
        public ITaskItem[] OpenAPIDocument { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (!this.ValidateAssemblyPaths(out var assemblyPaths))
            {
                this.Log.LogError(
                    "Invalid {0}: {1}",
                    nameof(this.AssemblyPath),
                    string.Join(';', this.AssemblyPath.Select(a => a.ItemSpec)));
                return false;
            }

            if (!this.ValidateXmlDocumentationFiles(out var xmlDocumentationFiles))
            {
                this.Log.LogError(
                    "Invalid {0}: {1}",
                    nameof(this.XmlDocumentationFile),
                    string.Join(';', this.XmlDocumentationFile.Select(a => a.ItemSpec)));
                this.Log.LogError(string.Empty);
                return false;
            }

            if (!this.ValidateOutputPath(out var outputPath))
            {
                this.Log.LogError("Invalid {0}: {1}", nameof(this.OutputPath), this.OutputPath);
                return false;
            }

            if (!this.ValidateOpenApiSpecVersion(out var openApiSpecVersion))
            {
                this.Log.LogError("Invalid {0}: {1}", nameof(this.OpenApiSpecVersion), this.OpenApiSpecVersion);
                return false;
            }

            if (!this.ValidateOutputFormat(
                out var outputFormat))
            {
                this.Log.LogError("Invalid {0}: {1}", nameof(this.OutputFormat), this.OutputFormat);
                return false;
            }

            try
            {
                var generatedDocuments =
                    OpenApiDocumentGenerator.GenerateOpenApiDocuments(
                        this.OpenApiDocumentVersion,
                        assemblyPaths,
                        xmlDocumentationFiles,
                        this.DocumentDescription,
                        outputPath,
                        openApiSpecVersion,
                        this.OutputFileNamePrefix,
                        outputFormat);

                this.OpenAPIDocument =
                    generatedDocuments
                    .Select((openApidocument) => new TaskItem(openApidocument))
                    .ToArray();
            }
            catch (OpenApiDocumentGeneratorException e)
            {
                this.Log.LogError(e.ToString());
                return false;
            }

            return true;
        }

        private bool ValidateOutputFormat(
            out OpenApiFormat outputFormat)
        {
            bool errors = false;

            if (this.OutputFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                outputFormat = OpenApiFormat.Json;
            }
            else if (this.OutputFormat.Equals("YAML", StringComparison.OrdinalIgnoreCase))
            {
                outputFormat = OpenApiFormat.Yaml;
            }
            else
            {
                outputFormat = OpenApiFormat.Json;
                errors = true;
            }

            return errors;
        }

        private bool ValidateOpenApiSpecVersion(out OpenApiSpecVersion openApiSpecVersion)
        {
            bool errors = false;
            if (this.OpenApiSpecVersion.Equals("2.0", StringComparison.OrdinalIgnoreCase))
            {
                openApiSpecVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
            }
            else if (this.OpenApiSpecVersion.Equals("3.0", StringComparison.OrdinalIgnoreCase))
            {
                openApiSpecVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            }
            else
            {
                openApiSpecVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                errors = true;
            }

            return errors;
        }

        private bool ValidateOutputPath(out string outputPath)
        {
            bool errors = false;

            outputPath =
                string.IsNullOrEmpty(this.OutputPath)
                ? this.IntermediateOutputPath
                : this.OutputPath;

            if (string.IsNullOrEmpty(outputPath))
            {
                errors = true;
            }

            return errors;
        }

        private bool ValidateXmlDocumentationFiles(out IEnumerable<string> xmlDocumentationFiles)
        {
            bool errors = false;
            xmlDocumentationFiles = new List<string>();
            foreach (var xmlDocumentationFile in this.XmlDocumentationFile)
            {
                if (!File.Exists(xmlDocumentationFile.ItemSpec))
                {
                    errors = true;
                    continue;
                }

                (xmlDocumentationFiles as List<string>).Add(xmlDocumentationFile.ItemSpec);
            }

            return errors;
        }

        private bool ValidateAssemblyPaths(out IEnumerable<string> assemblyPaths)
        {
            bool errors = false;
            assemblyPaths = new List<string>();
            foreach (var assemblyPath in this.AssemblyPath)
            {
                if (!File.Exists(assemblyPath.ItemSpec))
                {
                    errors = true;
                    continue;
                }

                (assemblyPaths as List<string>).Add(assemblyPath.ItemSpec);
            }

            return errors;
        }
    }
}
