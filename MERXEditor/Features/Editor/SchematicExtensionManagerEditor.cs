using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MERX.Features.Editor
{
    [CustomEditor(typeof(SchematicExtensionManager))]
    public class SchematicExtensionManagerEditor : UnityEditor.Editor
    {
        private Dictionary<SchematicExtensionBase, bool> _extensionsToCompile = new();
        private bool _compileWithSchematic = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (SchematicExtensionManager)target;

            GUILayout.Label("Compile Extensions", EditorStyles.boldLabel);

            foreach (var extensionBase in manager.Extensions.ToList())
            {
                if (extensionBase == null)
                {
                    manager.Extensions.Remove(extensionBase);
                    continue;
                }

                _extensionsToCompile.TryAdd(extensionBase, true);

                _extensionsToCompile[extensionBase] = EditorGUILayout.ToggleLeft(extensionBase.GetType().Name, _extensionsToCompile[extensionBase]);
            }

            if (_extensionsToCompile.Count == 0)
            {
                GUILayout.Label("No extensions found.", EditorStyles.miniLabel);
                return;
            }

            GUILayout.Space(10);

            _compileWithSchematic = EditorGUILayout.ToggleLeft("Compile with Schematic", _compileWithSchematic);

            GUI.enabled = _extensionsToCompile.Any(extension => extension.Value);

            if (GUILayout.Button("Compile Selected"))
            {
                manager.CompileSelected(_extensionsToCompile, _compileWithSchematic);
            }

            GUI.enabled = true;

            if (GUILayout.Button("Compile All"))
            {
                manager.CompileAll(_compileWithSchematic);
            }
        }
    }
}