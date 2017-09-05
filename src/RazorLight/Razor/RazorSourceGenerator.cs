﻿using Microsoft.AspNetCore.Razor.Language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RazorLight.Razor
{
    public class RazorSourceGenerator
    {
        public RazorSourceGenerator(RazorEngine engine, RazorLightProject project)
        {
            Engine = engine;
            Project = project;
            DefaultImports = GetDefaultImports();
        }

        public RazorEngine Engine { get; set; }

        public RazorLightProject Project { get; set; }

        public RazorSourceDocument DefaultImports { get; set; }

        /// <summary>
        /// Parses the template specified by the project item <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The template path.</param>
        /// <returns>The <see cref="RazorCSharpDocument"/>.</returns>
        public RazorCSharpDocument GenerateCode(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException();
            }

            RazorLightProjectItem projectItem = Project.GetItem(key);
            return GenerateCode(projectItem);
        }

        /// <summary>
        /// Parses the template specified by <paramref name="projectItem"/>.
        /// </summary>
        /// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
        /// <returns>The <see cref="RazorCSharpDocument"/>.</returns>
        public RazorCSharpDocument GenerateCode(RazorLightProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            if (!projectItem.Exists)
            {
                throw new InvalidOperationException($"Project can not find template with key {projectItem.Key}");
            }

            RazorCodeDocument codeDocument = CreateCodeDocument(projectItem);
            return GenerateCode(codeDocument);
        }

        /// <summary>
        /// Parses the template specified by <paramref name="codeDocument"/>.
        /// </summary>
        /// <param name="codeDocument">The <see cref="RazorLightProjectItem"/>.</param>
        /// <returns>The <see cref="RazorCSharpDocument"/>.</returns>
        public virtual RazorCSharpDocument GenerateCode(RazorCodeDocument codeDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            Engine.Process(codeDocument);
            return codeDocument.GetCSharpDocument();
        }

        /// <summary>
        /// Generates a <see cref="RazorCodeDocument"/> for the specified <paramref name="templateKey"/>.
        /// </summary>
        /// <param name="templateKey">The template path.</param>
        /// <returns>The created <see cref="RazorCodeDocument"/>.</returns>
        public virtual RazorCodeDocument CreateCodeDocument(string templateKey)
        {
            if (string.IsNullOrEmpty(templateKey))
            {
                throw new ArgumentException();
            }

            var projectItem = Project.GetItem(templateKey);
            return CreateCodeDocument(projectItem);
        }

        /// <summary>
        /// Generates a <see cref="RazorCodeDocument"/> for the specified <paramref name="projectItem"/>.
        /// </summary>
        /// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
        /// <returns>The created <see cref="RazorCodeDocument"/>.</returns>
        public virtual RazorCodeDocument CreateCodeDocument(RazorLightProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            if (!projectItem.Exists)
            {
                throw new InvalidOperationException();
            }

            RazorSourceDocument source = RazorSourceDocument.ReadFrom(projectItem.Read(), projectItem.Key);
            IEnumerable<RazorSourceDocument> imports = GetImports(projectItem);

            return RazorCodeDocument.Create(source, imports);
        }

        /// <summary>
        /// Gets <see cref="RazorSourceDocument"/> that are applicable to the specified <paramref name="projectItem"/>.
        /// </summary>
        /// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
        /// <returns>The sequence of applicable <see cref="RazorSourceDocument"/>.</returns>
        public virtual IEnumerable<RazorSourceDocument> GetImports(RazorLightProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }
            var result = new List<RazorSourceDocument>();

            IEnumerable<RazorLightProjectItem> importProjectItems = Project.GetImports(projectItem.Key);
            foreach (var importItem in importProjectItems)
            {
                if (importItem.Exists)
                {
                    // We want items in descending order. FindHierarchicalItems returns items in ascending order.
                    result.Insert(0, RazorSourceDocument.ReadFrom(importItem.Read(), null));
                }
            }

            if (DefaultImports != null)
            {
                result.Insert(0, DefaultImports);
            }

            return result;
        }

        // Internal for testing.
        internal static RazorSourceDocument GetDefaultImports()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("@using System");
                writer.WriteLine("@using System.Collections.Generic");
                writer.WriteLine("@using System.Linq");
                writer.WriteLine("@using System.Threading.Tasks");
                //writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> Html");
                //writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json");
                //writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component");
                //writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.IUrlHelper Url");
                //writer.WriteLine("@inject global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider");
                //writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                //writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                //writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper, Microsoft.AspNetCore.Mvc.Razor");
                writer.Flush();

                stream.Position = 0;
                return RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
            }
        }
    }
}
