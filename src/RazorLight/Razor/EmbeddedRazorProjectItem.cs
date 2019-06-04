﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RazorLight.Razor
{
    public class EmbeddedRazorProjectItem : RazorLightProjectItem
    {
        private string fullTemplateKey;

        public EmbeddedRazorProjectItem(Type rootType, string key)
        {
            if(rootType == null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            RootType = rootType;
            Assembly = RootType.GetTypeInfo().Assembly;

            fullTemplateKey = RootType.Namespace + "." + Key;
        }

        public Assembly Assembly { get; set; }

        public Type RootType { get; set; }

        public override string Key { get; }

        public override bool Exists
        {
            get
            {
                return Assembly.GetManifestResourceNames().Any(f => f == fullTemplateKey);
            }
        }

        public override Stream Read()
        {
            return Assembly.GetManifestResourceStream(fullTemplateKey);
        }
    }
}
