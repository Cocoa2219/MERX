using System;
using System.Collections.Generic;
using System.IO;
using MapEditorReborn.API.Features.Objects;
using MERXPlugin.Features.Enums;
using MERXPlugin.Features.Extensions;
using DamageType = Exiled.API.Enums.DamageType;
using DeathType = MERXPlugin.Features.Enums.DeathType;
using ExtensionType = MERXPlugin.Features.Enums.ExtensionType;
using RoleTypeId = PlayerRoles.RoleTypeId;

namespace MERXPlugin.Features.Decompilers;

public class DamageableExtensionDecompiler : SchematicExtensionDecompilerBase
{
    /// <inheritdoc />
    public override Type Extension { get; } = typeof(DamageableExtension);

    /// <inheritdoc />
    public override ExtensionType Type => ExtensionType.Damageable;

    /// <inheritdoc />
    public override bool Decompile(SchematicObject schematicObject, string schematicName)
    {
        var path = schematicObject.SchematicData.Path;
        var file = $"{path}/{schematicName}-Damageable.merx";

        if (!File.Exists(file))
        {
            return false;
        }

        using var fileStream = File.OpenRead(file);
        using var reader = new BinaryReader(fileStream);

        var data = new DamageableExtensionData
        {
            Hitboxes = new List<DamageableExtensionData.HitboxData>(),
            IgnoredDamageTypes = new List<DamageType>(),
            IgnoredRoles = new List<RoleTypeId>(),
        };

        var maxHealth = reader.ReadInt32();
        data.MaxHealth = maxHealth;

        var invincible = reader.ReadBoolean();
        data.Invincible = invincible;

        var hitboxCount = reader.ReadInt32();
        for (var i = 0; i < hitboxCount; i++)
        {
            var hitboxData = new DamageableExtensionData.HitboxData
            {
                ObjectId = reader.ReadInt32(),
                Multiplier = reader.ReadSingle(),
            };
            data.Hitboxes.Add(hitboxData);
        }

        var ignoredDamageTypeCount = reader.ReadInt32();
        for (var i = 0; i < ignoredDamageTypeCount; i++)
        {
            var damageType = (DamageType) reader.ReadInt32();
            data.IgnoredDamageTypes.Add(damageType);
        }

        var ignoredRoleCount = reader.ReadInt32();
        for (var i = 0; i < ignoredRoleCount; i++)
        {
            var role = (RoleTypeId) reader.ReadByte();
            data.IgnoredRoles.Add(role);
        }

        var deathType = (DeathType) reader.ReadByte();

        if (deathType.HasFlag(DeathType.Broadcast))
        {
            var broadcastData = new DamageableExtensionData.BroadcastData();
            broadcastData.Read(reader);
            data.Broadcast = broadcastData;
        }

        if (deathType.HasFlag(DeathType.SpawnItem))
        {
            var spawnItemData = new DamageableExtensionData.SpawnItemData();
            spawnItemData.Read(reader);
            data.SpawnItem = spawnItemData;
        }

        if (deathType.HasFlag(DeathType.Explode))
        {
            var explosionData = new DamageableExtensionData.ExplodeData();
            explosionData.Read(reader);
            data.Explosion = explosionData;
        }

        if (deathType.HasFlag(DeathType.PlaySound))
        {
            var playSoundData = new DamageableExtensionData.PlaySoundData();
            playSoundData.Read(reader);
            data.PlaySound = playSoundData;
        }

        if (deathType.HasFlag(DeathType.Dynamic))
        {
            var dynamicData = new DamageableExtensionData.DynamicData();
            dynamicData.Read(reader);
            data.Dynamic = dynamicData;
        }

        data.DeathType = deathType;

        Initialize(schematicObject, data);

        return true;
    }
}

public class DamageableExtensionData : ExtensionData
{
    public int MaxHealth { get; set; }
    public bool Invincible { get; set; }
    public List<HitboxData> Hitboxes { get; set; }
    public List<DamageType> IgnoredDamageTypes { get; set; }
    public List<RoleTypeId> IgnoredRoles { get; set; }
    public DeathType DeathType { get; set; }
    public BroadcastData Broadcast { get; set; }
    public SpawnItemData SpawnItem { get; set; }
    public ExplodeData Explosion { get; set; }
    public PlaySoundData PlaySound { get; set; }
    public DynamicData Dynamic { get; set; }

    public class HitboxData
    {
        public int ObjectId { get; set; }
        public float Multiplier { get; set; }
    }

    public class BroadcastData : ReadableData
    {
        public List<BroadcastMessageData> Messages { get; set; }

        public class BroadcastMessageData
        {
            public string Message { get; set; }
            public ushort Duration { get; set; }
            public BroadcastType Type { get; set; }
        }

        /// <inheritdoc />
        public void Read(BinaryReader reader)
        {
            var messageCount = reader.ReadInt32();
            Messages = new List<BroadcastMessageData>();

            for (var i = 0; i < messageCount; i++)
            {
                var message = new BroadcastMessageData
                {
                    Message = reader.ReadString(),
                    Duration = reader.ReadUInt16(),
                    Type = (BroadcastType) reader.ReadByte(),
                };
                Messages.Add(message);
            }
        }
    }

    public class SpawnItemData : ReadableData
    {
        public List<ItemData> Items { get; set; }

        public class ItemData
        {
            public ItemType Type { get; set; }
            public int Amount { get; set; }
        }

        /// <inheritdoc />
        public void Read(BinaryReader reader)
        {
            var itemCount = reader.ReadInt32();
            Items = new List<ItemData>();

            for (var i = 0; i < itemCount; i++)
            {
                var item = new ItemData
                {
                    Type = (ItemType) reader.ReadByte(),
                    Amount = reader.ReadInt32(),
                };
                Items.Add(item);
            }
        }
    }

    public class ExplodeData : ReadableData
    {
        public List<ExplosionData> Explosions { get; set; }

        public class ExplosionData
        {
            public float MaxRadius { get; set; }
            public float ScpDamageMultiplier { get; set; }
            public float BurnDuration { get; set; }
            public float ConcussionDuration { get; set; }
            public float FuseTime { get; set; }
        }

        /// <inheritdoc />
        public void Read(BinaryReader reader)
        {
            var explosionCount = reader.ReadInt32();
            Explosions = new List<ExplosionData>();

            for (var i = 0; i < explosionCount; i++)
            {
                var explosion = new ExplosionData
                {
                    MaxRadius = reader.ReadSingle(),
                    ScpDamageMultiplier = reader.ReadSingle(),
                    BurnDuration = reader.ReadSingle(),
                    ConcussionDuration = reader.ReadSingle(),
                    FuseTime = reader.ReadSingle(),
                };
                Explosions.Add(explosion);
            }
        }
    }

    public class PlaySoundData : ReadableData
    {
        public List<SoundData> Sounds { get; set; }

        public class SoundData
        {
            public string ClipName { get; set; }
            public float Volume { get; set; }
            public bool IsSpatial { get; set; }
            public float MinDistance { get; set; }
            public float MaxDistance { get; set; }
        }

        /// <inheritdoc />
        public void Read(BinaryReader reader)
        {
            var soundCount = reader.ReadInt32();
            Sounds = new List<SoundData>();

            for (var i = 0; i < soundCount; i++)
            {
                var sound = new SoundData
                {
                    ClipName = reader.ReadString(),
                    Volume = reader.ReadSingle(),
                    IsSpatial = reader.ReadBoolean(),
                    MinDistance = reader.ReadSingle(),
                    MaxDistance = reader.ReadSingle(),
                };
                Sounds.Add(sound);
            }
        }
    }

    public class DynamicData : ReadableData
    {
        public List<ExecutionData> Exec { get; set; }

        public class ExecutionData
        {
            public string Code { get; set; }
        }

        /// <inheritdoc />
        public void Read(BinaryReader reader)
        {
            var execCount = reader.ReadInt32();
            Exec = new List<ExecutionData>();

            for (var i = 0; i < execCount; i++)
            {
                var exec = new ExecutionData
                {
                    Code = reader.ReadString(),
                };
                Exec.Add(exec);
            }
        }
    }
}