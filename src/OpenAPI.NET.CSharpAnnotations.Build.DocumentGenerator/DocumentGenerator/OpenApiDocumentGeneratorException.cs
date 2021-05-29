// Copyright (c) Vatsan Madhavan. All rights reserved.

namespace OpenAPI.NET.CSharpAnnotations.Build
{
    using System;
    using System.Text;
    using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration;
    using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;

    /// <summary>
    /// Thrown when <see cref="OpenApiGenerator"/> encounters errors when generating OpenAPI documents.
    /// </summary>
    [Serializable]
    internal class OpenApiDocumentGeneratorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiDocumentGeneratorException"/> class.
        /// </summary>
        /// <param name="generatorDiagnostic"><see cref="GenerationDiagnostic"/> instance.</param>
        public OpenApiDocumentGeneratorException(GenerationDiagnostic generatorDiagnostic)
            : base(GeneratorDiagnosticToString(generatorDiagnostic))
        {
        }

        private static string GeneratorDiagnosticToString(GenerationDiagnostic generatorDiagnostic)
        {
            var builder = new StringBuilder();

            if (generatorDiagnostic.DocumentGenerationDiagnostic?.Errors?.Count > 0)
            {
                builder.AppendLine($"{nameof(GenerationDiagnostic.DocumentGenerationDiagnostic)}:");
                foreach (var error in generatorDiagnostic.DocumentGenerationDiagnostic.Errors)
                {
                    builder.AppendLine($"\t{error.ExceptionType}: {error.Message}");
                }

                builder.AppendLine();
            }

            if (generatorDiagnostic.OperationGenerationDiagnostics?.Count > 0)
            {
                builder.AppendLine($"{nameof(GenerationDiagnostic.OperationGenerationDiagnostics)}:");
                foreach (var diag in generatorDiagnostic.OperationGenerationDiagnostics)
                {
                    if (diag?.Errors?.Count > 0)
                    {
                        builder.AppendLine($"\t{diag.OperationMethod}:");
                        foreach (var error in diag.Errors)
                        {
                            builder.AppendLine($"\t\t{error.ExceptionType}: {error.Message}");
                        }
                    }
                }
            }

            return builder.ToString();
        }
    }
}