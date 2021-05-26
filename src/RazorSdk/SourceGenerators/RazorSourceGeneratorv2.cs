// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    [Generator]
    public class RazorSourceGeneratorv2 : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // while (!System.Diagnostics.Debugger.IsAttached)
            // {
            //     System.Console.WriteLine($"Waiting to attach on ${System.Diagnostics.Process.GetCurrentProcess().Id}");
            //     System.Threading.Thread.Sleep(1000);
            // }
            initContext.RegisterExecutionPipeline(context =>
            {
                var tagHelperFeature = new StaticCompilationTagHelperFeature();
                // Resolve tag helpers from references via transform
                var references = context.Sources.Compilation
                    .Transform(c => {
                        while (!System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Console.WriteLine($"Waiting to attach on ${System.Diagnostics.Process.GetCurrentProcess().Id}");
                            System.Threading.Thread.Sleep(1000);
                        }
                        return c.References;
                    });
                var tagHelpersFromReferences = references
                    .Join(context.Sources.Compilation)
                    .Transform<(IEnumerable<MetadataReference>, ImmutableArray<Compilation>), IReadOnlyList<TagHelperDescriptor>>(pair => {
                        while (!System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Console.WriteLine($"Waiting to attach on ${System.Diagnostics.Process.GetCurrentProcess().Id}");
                            System.Threading.Thread.Sleep(1000);
                        }
                        var references = pair.Item1;
                        var compilation = pair.Item2.Single();
                        return GetTagHelpers(references, tagHelperFeature, compilation);
                    });
                // Resolve tag helpers from compilation via transform
                var sourceItems = context.Sources.AdditionalTexts.Join(context.Sources.AnalyzerConfigOptions).Transform<(AdditionalText, ImmutableArray<AnalyzerConfigOptionsProvider>), RazorProjectItem?>(pair => {
                    var additionalText = pair.Item1;
                    var options = pair.Item2.Single();
                    options.GetOptions(additionalText).TryGetValue("build_metadata.AdditionalFiles.TargetPath", out var relativePath);
                    if (relativePath is not null)
                    {
                    return new SourceGeneratorProjectItem(
                    basePath: "/",
                    filePath: '/' + relativePath
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace("//", "/"),
                    relativePhysicalPath: relativePath,
                    fileKind: additionalText.Path.EndsWith(".razor") ? FileKinds.Component : FileKinds.Legacy,
                    additionalText: additionalText,
                    cssScope: "fill-this-in");
                    }
                    return null;
                });
                
                var config = RazorConfiguration.Create(RazorLanguageVersion.Latest, "default", Enumerable.Empty<RazorExtension>(), true);
                var discoveryProjectEngine = RazorProjectEngine.Create(config, RazorProjectFileSystem.Empty, b =>
                {
                    b.Features.Add(new DefaultTypeNameFeature());
                    b.Features.Add(new ConfigureRazorCodeGenerationOptions(options =>
                    {
                        options.SuppressPrimaryMethodBody = true;
                        options.SuppressChecksum = true;
                    }));

                    b.SetRootNamespace("ASP");

                    // b.Features.Add(new DefaultMetadataReferenceFeature { References = references.ToImmutableArray().ToList() });

                    b.Features.Add(tagHelperFeature);
                    b.Features.Add(new DefaultTagHelperDescriptorProvider());

                    CompilerFeatures.Register(b);
                    RazorExtensions.Register(b);

                    b.SetCSharpLanguageVersion(LanguageVersion.Preview);
                });
                var syntaxTrees = sourceItems.Filter<RazorProjectItem?>(item => item is not null).Transform<RazorProjectItem?, SyntaxTree>(item =>
                {
                    var codeGen = discoveryProjectEngine.Process(item!);
                    var generatedCode = codeGen.GetCSharpDocument().GeneratedCode;
                    return CSharpSyntaxTree.ParseText(generatedCode);
                });
                var tagHelpersFromCompilation = context.Sources.Compilation.Transform(c => GetTagHelpersFromCompilation(c, tagHelperFeature));
                
                var tagHelpers = tagHelpersFromCompilation.Join(tagHelpersFromReferences);
                // Map CSHTML files to generated code via transform
                sourceItems
                    .Filter(item => item is not null)
                    .Join(tagHelpers).GenerateSource((context, pair) => {
                        while (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Console.WriteLine($"Waiting to attach on ${System.Diagnostics.Process.GetCurrentProcess().Id}");
                System.Threading.Thread.Sleep(1000);
            }
                    var projectItem = pair.Item1;
                    // var tagHelpers = new ReadOnlyCollectionBuilder<TagHelperDescriptor>((pair.Item2[0]).Concat(pair.Item2[1][0])).ToReadOnlyCollection();
                    var projectEngine = RazorProjectEngine.Create(config, RazorProjectFileSystem.Empty, b =>
            {
                b.Features.Add(new DefaultTypeNameFeature());
                b.SetRootNamespace("ASP");

                // b.Features.Add(new ConfigureRazorCodeGenerationOptions(options =>
                // {
                //     options.SuppressMetadataSourceChecksumAttributes = !_razorContext.GenerateMetadataSourceChecksumAttributes;
                // }));

                // b.Features.Add(new StaticTagHelperFeature { TagHelpers = tagHelpers, });
                b.Features.Add(new DefaultTagHelperDescriptorProvider());

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(LanguageVersion.Preview);
            });
                var codeDocument = projectEngine.Process(projectItem!);
            var csharpDocument = codeDocument.GetCSharpDocument();
            var generatedCode = csharpDocument.GeneratedCode;
            context.AddSource(GetIdentifierFromPath(projectItem!.RelativePhysicalPath), generatedCode);
                });
            });
        }

        public IReadOnlyList<TagHelperDescriptor> GetTagHelpers(IEnumerable<MetadataReference> references, StaticCompilationTagHelperFeature tagHelperFeature, Compilation compilation)
        {
            List<TagHelperDescriptor> descriptors = new();
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    tagHelperFeature.TargetAssembly = assembly;
            descriptors.AddRange(tagHelperFeature.GetDescriptors());
            
                }
            }
            return descriptors;
        }

        public IReadOnlyList<TagHelperDescriptor> GetTagHelpersFromCompilation(Compilation compilation, ITagHelperFeature tagHelperFeature)
        {
            return new List<TagHelperDescriptor>();
        }

        private static string GetIdentifierFromPath(string filePath)
        {
            var builder = new StringBuilder(filePath.Length);

            for (var i = 0; i < filePath.Length; i++)
            {
                switch (filePath[i])
                {
                    case ':' or '\\' or '/':
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(filePath[i]);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}