using MapEditorReborn.API.Features.Objects;
using MERXPlugin.Features.Decompilers;
using UnityEngine;

namespace MERXPlugin.Features;

public abstract class SchematicExtensionBase : MonoBehaviour
{
    public abstract void Initialize(SchematicObject schematicObject, ExtensionData data);
}