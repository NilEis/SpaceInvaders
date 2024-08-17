using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using TImport = byte[];

namespace Intel8080BinaryLoader;

[ContentImporter(".bin", DisplayName = "Intel8080Binary", DefaultProcessor = "PassThroughProcessor")]
public class Intel8080BinaryImporter : ContentImporter<TImport>
{
    public override TImport Import(string filename, ContentImporterContext context)
    {
        return File.ReadAllBytes(filename);
    }
}