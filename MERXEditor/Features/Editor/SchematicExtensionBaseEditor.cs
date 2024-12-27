using NaughtyAttributes.Editor;
using UnityEngine;

namespace MERX.Features.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(SchematicExtensionBase), true)]
    public class SchematicExtensionBaseEditor : NaughtyInspector
    {
        private SchematicExtensionManager manager;

        protected override void OnEnable()
        {
            var extension = (SchematicExtensionBase)target;
            if (manager == null)
            {
                manager = extension.GetComponent<SchematicExtensionManager>();
            }

            manager.Extensions.Add(extension);
        }

        protected override void OnDisable()
        {
            var extension = (SchematicExtensionBase)target;

            if (manager == null)
            {
                manager = extension.GetComponent<SchematicExtensionManager>();
            }

            manager.Extensions.Remove(extension);
        }
    }
}