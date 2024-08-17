using Microsoft.Xna.Framework.Content.Pipeline;

namespace Intel8080BinaryLoader;

[ContentProcessor(DisplayName = "Pass-Through Processor")]
public class PassThroughProcessor : ContentProcessor<byte[], byte[]>
{
    public override byte[] Process(byte[] input, ContentProcessorContext context)
    {
        return input;
    }
}