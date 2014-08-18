using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.Build.AssemblyNeutral
{
    public class EmbeddedReferenceHelper
    {
        public static IList<string> GetRequiredEmbeddedReferences(string outputAssemblyPath,
                                                                 IEnumerable<string> embeddedReferences,
                                                                 IEnumerable<string> allReferences)
        {
            var embeddedRefs = embeddedReferences.ToDictionary(Path.GetFileNameWithoutExtension);
            var allRefs = allReferences.Except(embeddedReferences).ToDictionary(Path.GetFileNameWithoutExtension);

            return GetUsedReferences(outputAssemblyPath, embeddedRefs, allRefs);
        }

        private static IList<string> GetUsedReferences(string outputAssemblyPath,
                                                         IDictionary<string, string> embeddedReferences,
                                                         IDictionary<string, string> references)
        {
            var results = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var stack = new Stack<string>();
            stack.Push(outputAssemblyPath);

            while (stack.Count > 0)
            {
                var path = stack.Pop();

                if (!seen.Add(path))
                {
                    continue;
                }

                foreach (var reference in GetReferences(path))
                {
                    string referencePath;
                    if (embeddedReferences.TryGetValue(reference, out referencePath))
                    {
                        results.Add(referencePath);
                        stack.Push(referencePath);
                    }

                    if (references.TryGetValue(reference, out referencePath))
                    {
                        stack.Push(referencePath);
                    }
                }
            }

            return results;
        }

        private static IList<string> GetReferences(string path)
        {
            var references = new List<string>();

            using (var stream = File.OpenRead(path))
            {
                var peReader = new PEReader(stream);

                var reader = peReader.GetMetadataReader();

                foreach (var a in reader.AssemblyReferences)
                {
                    var reference = reader.GetAssemblyReference(a);
                    var referenceName = reader.GetString(reference.Name);

                    references.Add(referenceName);
                }

                return references;
            }
        }
    }
}
