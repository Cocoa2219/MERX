using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MERX.Features.Enums;
using MERX.Features.Misc.Opus;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace MERX.Features.Extensions
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SchematicExtensionManager))]
    public class DamageableSchematic : SchematicExtensionBase
    {
        /// <inheritdoc />
        public override ExtensionType Type => ExtensionType.Damageable;

        /// <inheritdoc />
        public override bool Compile(bool suppressLogs = false)
        {
            CheckHitboxes();
            m_rolesIgnored = CheckDuplicates(m_rolesIgnored, "There are duplicate roles in the ignored roles list.");
            m_damageTypesIgnored = CheckDuplicates(m_damageTypesIgnored,
                "There are duplicate damage types in the ignored damage types list.");

            var parentDirectoryPath = Directory.Exists(SchematicManager.Config.ExportPath)
                ? SchematicManager.Config.ExportPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MapEditorReborn_CompiledSchematics");

            var schematicDirectoryPath = Path.Combine(parentDirectoryPath, name);

            if (!Directory.Exists(parentDirectoryPath))
                Directory.CreateDirectory(parentDirectoryPath);

            if (Directory.Exists(schematicDirectoryPath))
                DeleteDirectory(schematicDirectoryPath);

            if (File.Exists($"{schematicDirectoryPath}.zip"))
                File.Delete($"{schematicDirectoryPath}.zip");

            Directory.CreateDirectory(schematicDirectoryPath);

            if (m_hitboxes.Count == 0)
            {
                var ask = EditorUtility.DisplayDialog("No Hitboxes",
                    "There are no hitboxes on this object. Do you want to continue?", "Yes", "No");

                if (!ask) return false;
            }

            using var fileStream = File.Create(Path.Combine(schematicDirectoryPath, $"{name}-Damageables.merx"));
            using var writer = new BinaryWriter(fileStream);

            writer.Write(m_maxHealth);
            writer.Write(m_isInvincible);

            writer.Write(m_hitboxes.Count);
            foreach (var hitbox in m_hitboxes)
            {
                if (hitbox.Primitive == null)
                {
                    Debug.LogError("Hitbox primitive is null.");

                    writer.Close();
                    fileStream.Close();

                    File.Delete(Path.Combine(schematicDirectoryPath, $"{name}-Damageable.merx"));

                    return false;
                }

                writer.Write(hitbox.Primitive.GetInstanceID());
                writer.Write(hitbox.Multiplier);
            }

            writer.Write(m_damageTypesIgnored.Count);
            foreach (var damageType in m_damageTypesIgnored) writer.Write((byte)damageType);

            writer.Write(m_rolesIgnored.Count);
            foreach (var role in m_rolesIgnored) writer.Write((byte)role);

            writer.Write((byte)m_deathType);

            if (m_deathType.HasFlag(DeathType.Broadcast))
                m_broadcastOptions.Write(writer);
            if (m_deathType.HasFlag(DeathType.SpawnItem))
                m_spawnItemOptions.Write(writer);
            if (m_deathType.HasFlag(DeathType.Explode))
                m_explosionOptions.Write(writer);
            if (m_deathType.HasFlag(DeathType.PlaySound))
                m_playSoundOptions.Write(writer);
            if (m_deathType.HasFlag(DeathType.Dynamic))
                m_dynamicOptions.Write(writer);

            if (!suppressLogs)
                Debug.Log($"<color=#00FF00>Successfully compiled damageable schematic <b>{name}</b>!</color>");

            writer.Close();
            fileStream.Close();

            return true;
        }

        private static void DeleteDirectory(string path)
        {
            var files = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs) DeleteDirectory(dir);

            Directory.Delete(path, false);
        }

        #region Inspector Fields

        [Header("Damageable Settings")]
        [SerializeField]
        [Tooltip("The maximum health of the object. Object will reset to this value when spawned.")]
        private int m_maxHealth = 100;

        [SerializeField] [Tooltip("Determines if the object is invincible.")]
        private bool m_isInvincible;

        [SerializeField] [Tooltip("Hitboxes of the object.")]
        private List<Hitbox> m_hitboxes = new();

        [SerializeField] [Tooltip("List of damage types that the object is immune to.")]
        private List<DamageType> m_damageTypesIgnored = new();

        [SerializeField] [Tooltip("List of roles that the object is immune to.")]
        private List<RoleTypeId> m_rolesIgnored = new();

        [Space]
        [Header("Death Settings")]
        [SerializeField]
        [Tooltip("Determines what happens when the object dies (the health reaches 0 or below).")]
        private DeathType m_deathType = DeathType.Destroy;

        [SerializeField]
        [Tooltip("Options for broadcasting a message when the object dies.")]
        [ShowIf("m_deathType", DeathType.Broadcast)]
        private BroadcastOptions m_broadcastOptions = new();

        [SerializeField]
        [Tooltip("Options for spawning items when the object dies.")]
        [ShowIf("m_deathType", DeathType.SpawnItem)]
        private SpawnItemOptions m_spawnItemOptions = new();

        [SerializeField]
        [Tooltip("Options for exploding when the object dies.")]
        [ShowIf("m_deathType", DeathType.Explode)]
        private ExplosionOptions m_explosionOptions = new();

        [SerializeField]
        [Tooltip("Options for playing sounds when the object dies.")]
        [ShowIf("m_deathType", DeathType.PlaySound)]
        private PlaySoundOptions m_playSoundOptions = new();

        [SerializeField]
        [Tooltip("Options for executing dynamic code when the object dies.")]
        [ShowIf("m_deathType", DeathType.Dynamic)]
        private DynamicOptions m_dynamicOptions = new();

        private void OnValidate()
        {
            CheckHitboxes();
            m_rolesIgnored = CheckDuplicates(m_rolesIgnored, "There are duplicate roles in the ignored roles list.");
            m_damageTypesIgnored = CheckDuplicates(m_damageTypesIgnored,
                "There are duplicate damage types in the ignored damage types list.");
        }

        private List<T> CheckDuplicates<T>(List<T> list, string message)
        {
            if (list.Count != list.Distinct().Count())
            {
                EditorUtility.DisplayDialog("Duplicate Values", message, "OK");
            }

            return list.Distinct().ToList();
        }

        private void CheckHitboxes()
        {
            if (m_hitboxes.Any(x => x.Primitive != null && x.Primitive.Collidable == false))
            {
                var invalidHitboxes = m_hitboxes.Where(x => x.Primitive != null && x.Primitive.Collidable == false)
                    .ToList();

                foreach (var hitbox in invalidHitboxes) hitbox.Primitive = null;

                EditorUtility.DisplayDialog("Invalid Hitbox", "Hitboxes cannot have collidable set to false.", "OK");
            }
        }

        #endregion
    }

    [Serializable]
    public class Hitbox
    {
        public PrimitiveComponent Primitive;
        public float Multiplier = 1f;
    }

    public abstract class DeathOptions
    {
        public abstract void Write(BinaryWriter writer);
    }

    [Serializable]
    public class BroadcastOptions : DeathOptions
    {
        public List<BroadcastMessage> Messages;

        [Serializable]
        public class BroadcastMessage
        {
            public string Message;
            public ushort Duration;
            public BroadcastType Type;

            public enum BroadcastType
            {
                Attacker,
                ToAll
            }
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Messages.Count);
            foreach (var message in Messages)
            {
                writer.Write(message.Message);
                writer.Write(message.Duration);
                writer.Write((byte)message.Type);
            }
        }
    }

    [Serializable]
    public class SpawnItemOptions : DeathOptions
    {
        public List<ItemSpawn> Items;

        [Serializable]
        public struct ItemSpawn
        {
            public ItemType Item;
            public int Amount;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Items.Count);
            foreach (var item in Items)
            {
                writer.Write((byte)item.Item);
                writer.Write(item.Amount);
            }
        }
    }

    [Serializable]
    public class ExplosionOptions : DeathOptions
    {
        public List<Explosion> Explosions;

        [Serializable]
        public class Explosion
        {
            public float MaxRadius;
            public float ScpDamageMultiplier;
            public float BurnDuration;
            public float ConcussionDuration;
            public float FuseTime;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Explosions.Count);
            foreach (var explosion in Explosions)
            {
                writer.Write(explosion.MaxRadius);
                writer.Write(explosion.ScpDamageMultiplier);
                writer.Write(explosion.BurnDuration);
                writer.Write(explosion.ConcussionDuration);
                writer.Write(explosion.FuseTime);
            }
        }
    }

    [Serializable]
    public class PlaySoundOptions : DeathOptions
    {
        public List<Sound> Sounds;

        [Serializable]
        public class Sound
        {
            public AudioClip Clip;
            public float Volume;
            public bool IsSpatial;
            public float MinDistance;
            public float MaxDistance;
        }

        public override void Write(BinaryWriter writer)
        {
            var fileStream = (FileStream)writer.BaseStream;
            var baseDir = Path.GetDirectoryName(fileStream.Name);

            if (baseDir == null)
            {
                Debug.LogError("Failed to get directory of the file stream.");
                return;
            }

            var directory = Path.Combine(baseDir, "MERX_Assets");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            writer.Write(Sounds.Count);
            foreach (var sound in Sounds)
            {
                if (sound.Clip == null) continue;

                if (sound.Clip.frequency != 48000 || sound.Clip.channels != 1)
                {
                    Debug.LogError("Sound clip should have a frequency of 48000 and 1 channel.");
                    continue;
                }

                using var encoder = new OpusEncoder(OpusApplicationType.Voip);
                var data = new byte[sound.Clip.samples * 2];
                var samples = new float[sound.Clip.samples];

                sound.Clip.GetData(samples, 0);

                var encoded = new byte[encoder.Encode(samples, data)];

                File.WriteAllBytes(Path.Combine(directory, $"{sound.Clip.name}.audio"), encoded);

                writer.Write(sound.Clip.name);
                writer.Write(sound.Volume);
                writer.Write(sound.IsSpatial);
                writer.Write(sound.MinDistance);
                writer.Write(sound.MaxDistance);
            }
        }
    }

    [Serializable]
    public class DynamicOptions : DeathOptions
    {
        public List<DynamicExecution> Exec;

        [Serializable]
        public class DynamicExecution
        {
            public string Code;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Exec.Count);
            foreach (var exec in Exec) writer.Write(exec.Code);
        }
    }
}