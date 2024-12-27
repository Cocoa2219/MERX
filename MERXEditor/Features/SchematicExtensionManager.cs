using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MERX.Features
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Schematic))]
    public class SchematicExtensionManager : MonoBehaviour
    {
        public List<SchematicExtensionBase> Extensions { get; private set; } = new();

        public void CompileSelected(Dictionary<SchematicExtensionBase, bool> extensionsToCompile, bool compileWithSchematic)
        {
            var failed = 0;
            foreach (var extension in extensionsToCompile.Where(extension => extension.Value))
            {
                if (!extension.Key.Compile())
                {
                    Debug.LogError($"<color=#FF0000>Failed to compile extension {extension.Key.GetType().Name}!</color>");
                    failed++;
                }
            }

            Debug.Log($"<color=#00FF00>Successfully compiled {extensionsToCompile.Count - failed} extensions, <color=#{(failed == 0 ? "00FF00" : "FF0000")}>failed to compile {failed} extensions!</color></color>");

            if (compileWithSchematic)
                CompileSchematic();
        }

        public void CompileAll(bool compileWithSchematic)
        {
            var failed = 0;
            foreach (var extension in Extensions)
            {
                if (!extension.Compile())
                {
                    Debug.LogError($"<color=#FF0000>Failed to compile extension {extension.GetType().Name}!</color>");
                    failed++;
                }
            }

            Debug.Log($"<color=#00FF00>Successfully compiled {Extensions.Count - failed} extensions, <color=#{(failed == 0 ? "00FF00" : "FF0000")}>failed to compile {failed} extensions!</color></color>");

            if (compileWithSchematic)
                CompileSchematic();
        }

        private void CompileSchematic()
        {
            var parentDirectoryPath = Directory.Exists(SchematicManager.Config.ExportPath)
                ? SchematicManager.Config.ExportPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MapEditorReborn_CompiledSchematics");

            var schematicDirectoryPath = Path.Combine(parentDirectoryPath, name);

            var blockList = new SchematicObjectDataList
            {
                RootObjectId = transform.GetInstanceID()
            };
            blockList.Blocks.Clear();
            var rigidbodyDictionary = new Dictionary<int, SerializableRigidbody>();
            rigidbodyDictionary.Clear();
            var teleports = new List<SerializableTeleport>();
            teleports.Clear();

            if (TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbodyDictionary.Add(transform.GetInstanceID(), new SerializableRigidbody(rigidbody));
            }

            foreach (var obj in GetComponentsInChildren<Transform>())
            {
                if (obj.CompareTag("EditorOnly") || obj == transform)
                    continue;

                var objectId = obj.transform.GetInstanceID();

                var block = new SchematicBlockData
                {
                    Name = obj.name,
                    ObjectId = objectId,
                    ParentId = obj.parent.GetInstanceID(),
                    Position = Quaternion.Euler(obj.parent.eulerAngles) * obj.localPosition
                };

                if (obj.TryGetComponent(out SchematicBlock schematicBlock))
                {
                    if (!schematicBlock.Compile(block, GetComponent<Schematic>()))
                        continue;
                }
                else
                {
                    // Light
                    if (obj.TryGetComponent(out Light lightComponent))
                    {
                        block.BlockType = BlockType.Light;
                        block.Properties = new Dictionary<string, object>
                        {
                            { "Color", ColorUtility.ToHtmlStringRGBA(lightComponent.color) },
                            { "Intensity", lightComponent.intensity },
                            { "Range", lightComponent.range },
                            { "Shadows", lightComponent.shadows != LightShadows.None }
                        };
                    }
                    else // Empty transform
                    {
                        obj.localScale = Vector3.one;

                        block.BlockType = BlockType.Empty;
                        block.Rotation = obj.localEulerAngles;
                    }
                }

                if (obj.TryGetComponent(out Animator animator) && animator.runtimeAnimatorController != null)
                {
                    var runtimeAnimatorController = animator.runtimeAnimatorController;
                    block.AnimatorName = runtimeAnimatorController.name;

                    BuildPipeline.BuildAssetBundle(runtimeAnimatorController,
                        runtimeAnimatorController.animationClips,
                        Path.Combine(schematicDirectoryPath, runtimeAnimatorController.name),
                        BuildAssetBundleOptions.ChunkBasedCompression |
                        BuildAssetBundleOptions.ForceRebuildAssetBundle |
                        BuildAssetBundleOptions.StrictMode, EditorUserBuildSettings.activeBuildTarget);
                }

                if (obj.TryGetComponent(out rigidbody))
                    rigidbodyDictionary.Add(objectId, new SerializableRigidbody(rigidbody));

                blockList.Blocks.Add(block);
            }

            File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{name}.json"),
                JsonConvert.SerializeObject(blockList, Formatting.Indented,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            if (rigidbodyDictionary.Count > 0)
                File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{name}-Rigidbodies.json"),
                    JsonConvert.SerializeObject(rigidbodyDictionary, Formatting.Indented,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            if (teleports.Count > 0)
                File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{name}-Teleports.json"),
                    JsonConvert.SerializeObject(teleports, Formatting.Indented,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            if (SchematicManager.Config.ZipCompiledSchematics)
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(schematicDirectoryPath, $"{schematicDirectoryPath}.zip",
                    System.IO.Compression.CompressionLevel.Optimal, true);
                Directory.Delete(schematicDirectoryPath, true);
            }
        }
    }
}