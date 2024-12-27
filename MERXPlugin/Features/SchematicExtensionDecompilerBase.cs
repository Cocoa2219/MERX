using System;
using MapEditorReborn.API.Features.Objects;
using MERXPlugin.Features.Decompilers;
using MERXPlugin.Features.Enums;

namespace MERXPlugin.Features;

public abstract class SchematicExtensionDecompilerBase
{
    public abstract Type Extension { get; }
    public abstract ExtensionType Type { get; }

    public abstract bool Decompile(SchematicObject schematicObject, string schematicName);

    public void Initialize(SchematicObject schematicObject, ExtensionData data)
    {
        if (Extension == null) throw new NullReferenceException("ExtensionType is null.");

        var extension = schematicObject.gameObject.AddComponent(Extension) as SchematicExtensionBase;

        if (extension == null) throw new NullReferenceException("Extension is null.");

        extension.Initialize(schematicObject, data);
    }
}