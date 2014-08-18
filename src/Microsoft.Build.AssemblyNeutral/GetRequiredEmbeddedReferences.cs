using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.AssemblyNeutral
{
    public class GetRequiredEmbeddedReferences : ITask
    {
        [Required]
        public ITaskItem TargetPath { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public ITaskItem[] EmbeddedReferences { get; set; }

        [Output]
        public ITaskItem[] RequiredEmbeddedReferences { get; set; }

        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            var targetPath = TargetPath.GetMetadata("FullPath");

            var embeddedReferences = EmbeddedReferences.Select(i => i.GetMetadata("FullPath"))
                                                       .ToList();
            var allReferences = References.Select(i => i.GetMetadata("FullPath"))
                                          .ToList();

            RequiredEmbeddedReferences = EmbeddedReferenceHelper.GetRequiredEmbeddedReferences(targetPath, embeddedReferences, allReferences)
                                                                .Select(p => new TaskItem(p))
                                                                .ToArray();

            return true;
        }
    }
}
