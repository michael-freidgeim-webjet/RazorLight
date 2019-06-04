﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System.IO;
using System.Reflection.PortableExecutable;

namespace RazorLight.Compilation
{
    public class DefaultMetadataReferenceManager : IMetadataReferenceManager
    {
        private readonly HashSet<MetadataReference> additionalMetadataReferences;
        public HashSet<MetadataReference> AdditionalMetadataReferences => additionalMetadataReferences;

        public DefaultMetadataReferenceManager()
        {
            additionalMetadataReferences = new HashSet<MetadataReference>();
        }

        public DefaultMetadataReferenceManager(HashSet<MetadataReference> metadataReferences)
        {
            if(metadataReferences == null)
            {
                throw new ArgumentNullException(nameof(metadataReferences));
            }

            additionalMetadataReferences = metadataReferences;
        }

        public IReadOnlyList<MetadataReference> Resolve(Assembly assembly)
        {
            var dependencyContext = DependencyContext.Load(assembly);

            return Resolve(dependencyContext);
        }

        internal IReadOnlyList<MetadataReference> Resolve(DependencyContext dependencyContext)
        {
            var libraryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = dependencyContext.CompileLibraries.SelectMany(library => library.ResolveReferencePaths());

            if (!references.Any())
            {
                throw new RazorLightException("Can't load metadata reference from the entry assembly. " +
                    "Make sure PreserveCompilationContext is set to true in *.csproj file");
            }

            var metadataRerefences = new List<MetadataReference>();

            foreach (var reference in references)
            {
                if (libraryPaths.Add(reference))
                {
                    using (var stream = File.OpenRead(reference))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);

                        metadataRerefences.Add(assemblyMetadata.GetReference(filePath: reference));
                    }
                }
            }

            if (AdditionalMetadataReferences.Any())
            {
                metadataRerefences.AddRange(AdditionalMetadataReferences);
            }

            return metadataRerefences;
        }
    }
}
