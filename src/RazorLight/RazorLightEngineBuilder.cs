﻿using Microsoft.CodeAnalysis;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace RazorLight
{
    public class RazorLightEngineBuilder
    {
        protected Assembly operatingAssembly;

        protected HashSet<string> namespaces;

        protected ConcurrentDictionary<string, string> dynamicTemplates;

        protected HashSet<MetadataReference> metadataReferences;

        protected List<Action<ITemplatePage>> prerenderCallbacks;

        protected RazorLightProject project;

        protected ICachingProvider cachingProvider;

        public virtual RazorLightEngineBuilder UseProject(RazorLightProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            this.project = project;

            return this;
        }

        public RazorLightEngineBuilder UseEmbeddedResourcesProject(Type rootType)
        {
            project = new EmbeddedRazorProject(rootType);

            return this;
        }

        public RazorLightEngineBuilder UseFilesystemProject(string root)
        {
            project = new FileSystemRazorProject(root);

            return this;
        }

        public virtual RazorLightEngineBuilder UseMemoryCachingProvider()
        {
            cachingProvider = new MemoryCachingProvider();

            return this;
        }

        public virtual RazorLightEngineBuilder UseCachingProvider(ICachingProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            cachingProvider = provider;

            return this;
        }

        public virtual RazorLightEngineBuilder AddDefaultNamespaces(params string[] namespaces)
        {
            if (namespaces == null)
            {
                throw new ArgumentNullException(nameof(namespaces));
            }

            this.namespaces = new HashSet<string>();

            foreach (string @namespace in namespaces)
            {
                this.namespaces.Add(@namespace);
            }

            return this;
        }

        public virtual RazorLightEngineBuilder AddMetadataReferences(params MetadataReference[] metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            metadataReferences = new HashSet<MetadataReference>();

            foreach (var reference in metadata)
            {
                metadataReferences.Add(reference);
            }

            return this;
        }

        public virtual RazorLightEngineBuilder AddPrerenderCallbacks(params Action<ITemplatePage>[] callbacks)
        {
            if (callbacks == null)
            {
                throw new ArgumentNullException(nameof(callbacks));
            }

            prerenderCallbacks = new List<Action<ITemplatePage>>();
            prerenderCallbacks.AddRange(callbacks);

            return this;
        }

        public virtual RazorLightEngineBuilder AddDynamicTemplates(IDictionary<string, string> dynamicTemplates)
        {
            if (dynamicTemplates == null)
            {
                throw new ArgumentNullException(nameof(dynamicTemplates));
            }

            this.dynamicTemplates = new ConcurrentDictionary<string, string>();

            foreach (var pair in dynamicTemplates)
            {
                dynamicTemplates.Add(pair);
            }

            return this;
        }

        public virtual RazorLightEngineBuilder SetOperatingAssembly(Assembly assembly)
        {
            if(assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            operatingAssembly = assembly;

            return this;
        }

        public virtual RazorLightEngine Build()
        {
            var options = new RazorLightOptions();

            if (namespaces != null)
            {
                options.Namespaces = namespaces;
            }

            if (dynamicTemplates != null)
            {
                options.DynamicTemplates = dynamicTemplates;
            }

            if (metadataReferences != null)
            {
                options.AdditionalMetadataReferences = metadataReferences;
            }

            if (prerenderCallbacks != null)
            {
                options.PreRenderCallbacks = prerenderCallbacks;
            }

            var sourceGenerator = new RazorSourceGenerator(DefaultRazorEngine.Instance, project, options.Namespaces);
            var metadataReferenceManager = new DefaultMetadataReferenceManager(options.AdditionalMetadataReferences);

            var assembly = operatingAssembly ?? Assembly.GetEntryAssembly();

            var compiler = new RoslynCompilationService(metadataReferenceManager, assembly);
            var templateFactoryProvider = new TemplateFactoryProvider(sourceGenerator, compiler, options);

            return new RazorLightEngine(options, templateFactoryProvider, cachingProvider);
        }
    }
}
