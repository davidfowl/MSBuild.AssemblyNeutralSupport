using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Framework.Runtime;

namespace Microsoft.Build.AssemblyNeutral
{
    public class ExpandEmbeddedAssemblies : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        [Required]
        public ITaskItem BaseIntermediateOutputPath { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        [Output]
        public ITaskItem[] EmbeddedReferences { get; set; }

        public bool Execute()
        {
            var outputs = new List<ITaskItem>();
            var embeddedReferences = new Dictionary<string, byte[]>();

            string outputPathBase = BaseIntermediateOutputPath.GetMetadata("FullPath");

            foreach (var reference in References)
            {
                var referencePath = reference.GetMetadata("FullPath");

                // TODO: Cache this
                foreach (var pair in EmbeddedAssemblyExtractor.GetEmbeddedReferences(referencePath))
                {
                    string name = pair.Item1;
                    byte[] buffer = pair.Item2;

                    embeddedReferences[name] = buffer;
                }
            }

            foreach (var reference in embeddedReferences)
            {
                var path = Path.Combine(outputPathBase, reference.Key + ".dll");

                if (!Directory.Exists(outputPathBase))
                {
                    Directory.CreateDirectory(outputPathBase);
                }

                File.WriteAllBytes(path, reference.Value);

                var item = new TaskItem(path);

                outputs.Add(item);
            }


            EmbeddedReferences = outputs.ToArray();

            return true;
        }
    }
}
