using System.IO;

namespace MERXPlugin.Features.Decompilers;

public interface ReadableData
{
    public void Read(BinaryReader reader);
}