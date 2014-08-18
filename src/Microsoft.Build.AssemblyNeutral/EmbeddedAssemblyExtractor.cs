﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.Framework.Runtime
{
    internal static class EmbeddedAssemblyExtractor
    {
        public static IList<Tuple<string, byte[]>> GetEmbeddedReferences(string path)
        {
            using (var fileStream = File.OpenRead(path))
            using (var reader = new PEReader(fileStream))
            {
                return reader.GetEmbeddedReferences();
            }
        }

        private static IList<Tuple<string, byte[]>> GetEmbeddedReferences(this PEReader reader)
        {
            var items = new List<Tuple<string, byte[]>>();

            var mdReader = reader.GetMetadataReader();
            foreach (var resourceHandle in mdReader.ManifestResources)
            {
                var resource = mdReader.GetManifestResource(resourceHandle);
                var resourceName = mdReader.GetString(resource.Name);

                // Embedded interface
                if (resourceName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var buffer = GetEmbeddedResourceContents(reader, resource);

                    // Remove .dll
                    var nameWithoutDll = resourceName.Substring(0, resourceName.Length - 4);

                    items.Add(Tuple.Create(nameWithoutDll, buffer));
                }
            }

            return items;
        }

        private static unsafe byte[] GetEmbeddedResourceContents(PEReader peReader, ManifestResource resource)
        {
            if (!resource.Implementation.IsNil)
            {
                throw new ArgumentException("Resource is not embedded in the PE file.", "resource");
            }

            checked // arithmetic overflow here could cause AV
            {
                // Locate start and end of PE image in unmanaged memory.
                var block = peReader.GetEntireImage();
                IntPtr peImageStartAsIntPtr = block.Pointer;
                int peImageSize = block.Length;
                byte* peImageStart = (byte*)peImageStartAsIntPtr;
                byte* peImageEnd = peImageStart + peImageSize;
                Debug.Assert(peImageStart != null && peImageSize > 0);

                // Locate offset to resources within PE image.
                int offsetToResources;
                if (!peReader.PEHeaders.TryGetDirectoryOffset(peReader.PEHeaders.CorHeader.ResourcesDirectory, out offsetToResources))
                {
                    throw new InvalidDataException("Failed to get offset to resources in PE file.");
                }
                Debug.Assert(offsetToResources > 0);
                byte* resourceStart = peImageStart + offsetToResources + resource.Offset;

                // Get the length of the the resource from the first 4 bytes.
                if (resourceStart >= peImageEnd - sizeof(int))
                {
                    throw new InvalidDataException("resource offset out of bounds.");
                }
                int resourceLength = *(int*)(resourceStart);
                resourceStart += sizeof(int);
                if (resourceLength < 0 || resourceStart >= peImageEnd - resourceLength)
                {
                    throw new InvalidDataException("resource offset or length out of bounds.");
                }

                // TODO: Use UmanagedMemoryStream when available on core clr
                var buffer = new byte[resourceLength];
                for (int i = 0; i < resourceLength; i++)
                {
                    buffer[i] = *(resourceStart + i);
                }

                return buffer;
            }
        }
    }
}