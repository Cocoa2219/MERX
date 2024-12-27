using System.Collections.Generic;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using MapEditorReborn.API.Features.Objects;
using MERXPlugin.Features.Decompilers;
using MERXPlugin.Features.Enums;
using Mirror;
using UnityEngine;
using AttackerDamageHandler = PlayerStatsSystem.AttackerDamageHandler;
using DamageHandlerBase = PlayerStatsSystem.DamageHandlerBase;
using DamageType = Exiled.API.Enums.DamageType;
using DeathType = MERXPlugin.Features.Enums.DeathType;
using RoleTypeId = PlayerRoles.RoleTypeId;

namespace MERXPlugin.Features.Extensions;

public class DamageableExtension : SchematicExtensionBase
{
    /// <inheritdoc />
    public override void Initialize(SchematicObject schematicObject, ExtensionData data)
    {
        if (data is not DamageableExtensionData damageableData)
        {
            return;
        }

        MaxHealth = damageableData.MaxHealth;
        Health = MaxHealth;

        Invincible = damageableData.Invincible;

        Hitboxes = new List<Hitbox>();
        foreach (var hitboxData in damageableData.Hitboxes)
        {
            var go = Plugin.Instance.IdToTransform[schematicObject][hitboxData.ObjectId].gameObject;
            var hitboxObj = go.AddComponent<HitboxObject>();
            hitboxObj.Initialize(this, hitboxData.Multiplier);

            var hitbox = new Hitbox
            {
                Object = hitboxObj,
                Multiplier = hitboxData.Multiplier,
            };
            Hitboxes.Add(hitbox);
        }

        IgnoredDamageTypes = damageableData.IgnoredDamageTypes;
        IgnoredRoles = damageableData.IgnoredRoles;

        DeathType = damageableData.DeathType;

        Broadcast = new BroadcastOptions() { Messages = new List<BroadcastOptions.BroadcastMessage>() };
        foreach (var message in damageableData.Broadcast.Messages)
        {
            var broadcastMessage = new BroadcastOptions.BroadcastMessage
            {
                Message = message.Message,
                Duration = message.Duration,
                Type = message.Type,
            };
            Broadcast.Messages.Add(broadcastMessage);
        }

        SpawnItem = new SpawnItemOptions() { Items = new List<SpawnItemOptions.ItemSpawn>() };
        foreach (var item in damageableData.SpawnItem.Items)
        {
            var itemSpawn = new SpawnItemOptions.ItemSpawn
            {
                Type = item.Type,
                Amount = item.Amount,
            };
            SpawnItem.Items.Add(itemSpawn);
        }

        Explosion = new ExplosionOptions() { Explosions = new List<ExplosionOptions.Explosion>() };
        foreach (var explode in damageableData.Explosion.Explosions)
        {
            var explosion = new ExplosionOptions.Explosion
            {
                MaxRadius = explode.MaxRadius,
                ScpDamageMultiplier = explode.ScpDamageMultiplier,
                BurnDuration = explode.BurnDuration,
                ConcussionDuration = explode.ConcussionDuration,
                FuseTime = explode.FuseTime,
            };
            Explosion.Explosions.Add(explosion);
        }

        PlaySound = new PlaySoundOptions() { Sounds = new List<PlaySoundOptions.Sound>() };
        foreach (var sound in damageableData.PlaySound.Sounds)
        {
            var playSound = new PlaySoundOptions.Sound
            {
                ClipName = sound.ClipName,
                Volume = sound.Volume,
                IsSpatial = sound.IsSpatial,
                MinDistance = sound.MinDistance,
                MaxDistance = sound.MaxDistance,
            };
            PlaySound.Sounds.Add(playSound);
        }

        Dynamic = new DynamicOptions() { Exec = new List<DynamicOptions.DynamicExecution>() };
        foreach (var exec in damageableData.Dynamic.Exec)
        {
            var dynamicExecution = new DynamicOptions.DynamicExecution
            {
                Code = exec.Code,
            };
            Dynamic.Exec.Add(dynamicExecution);
        }
    }

    public void Damage(float damage, Player attacker = null)
    {
        Health -= damage;

        if (Health <= 0)
        {
            if (DeathType.HasFlag(DeathType.Destroy))
            {
                Destroy(gameObject);
            }

            if (DeathType.HasFlag(DeathType.Broadcast))
            {
                foreach (var message in Broadcast.Messages)
                {
                    switch (message.Type)
                    {
                        case BroadcastType.Attacker:
                            if (attacker != null)
                            {
                                attacker.Broadcast(message.Duration, message.Message);
                            }
                            break;
                        case BroadcastType.ToAll:
                            Map.Broadcast(message.Duration, message.Message);
                            break;
                    }
                }
            }

            if (DeathType.HasFlag(DeathType.SpawnItem))
            {
                foreach (var item in SpawnItem.Items)
                {
                    for (var i = 0; i < item.Amount; i++)
                    {
                        Pickup.CreateAndSpawn(item.Type, transform.position + Vector3.up);
                    }
                }
            }

            if (DeathType.HasFlag(DeathType.Explode))
            {
                foreach (var explode in Explosion.Explosions)
                {
                    var item = Item.Create(ItemType.GrenadeHE, Server.Host);
                    var grenade = item.As<ExplosiveGrenade>();
                    grenade.MaxRadius = explode.MaxRadius;
                    grenade.ScpDamageMultiplier = explode.ScpDamageMultiplier;
                    grenade.BurnDuration = explode.BurnDuration;
                    grenade.ConcussDuration = explode.ConcussionDuration;
                    grenade.FuseTime = explode.FuseTime;

                    grenade.SpawnActive(transform.position + Vector3.up, Server.Host);
                }
            }

            if (DeathType.HasFlag(DeathType.PlaySound))
            {
                // TODO: Play sound
            }

            if (DeathType.HasFlag(DeathType.Dynamic))
            {
                foreach (var exec in Dynamic.Exec)
                {
                    // TODO: Execute dynamic code
                }
            }
        }
    }

    public float Health { get; set; }
    public bool IsDead => Health <= 0;

    public int MaxHealth { get; set; }
    public bool Invincible { get; set; }
    public List<Hitbox> Hitboxes { get; set; }
    public List<DamageType> IgnoredDamageTypes { get; set; }
    public List<RoleTypeId> IgnoredRoles { get; set; }
    public DeathType DeathType { get; set; }
    public BroadcastOptions Broadcast { get; set; }
    public SpawnItemOptions SpawnItem { get; set; }
    public ExplosionOptions Explosion { get; set; }
    public PlaySoundOptions PlaySound { get; set; }
    public DynamicOptions Dynamic { get; set; }

    public class Hitbox
    {
        public HitboxObject Object { get; set; }
        public float Multiplier { get; set; }
    }

    public class HitboxObject : MonoBehaviour, IDestructible
    {
        /// <inheritdoc />
        public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
        {
            if (_damageable.Invincible || _damageable.IsDead)
            {
                return false;
            }

            var damageHandler = new CustomDamageHandler(null, handler);

            if (handler is AttackerDamageHandler attackerDamageHandler)
            {
                if (_damageable.IgnoredRoles.Contains(attackerDamageHandler.Attacker.Role))
                {
                    return false;
                }
            }

            if (_damageable.IgnoredDamageTypes.Contains(damageHandler.Type))
            {
                return false;
            }

            _damageable.Damage(damage * _multiplier, damageHandler.Attacker);
            return true;
        }

        public void Initialize(DamageableExtension damageable, float multiplier)
        {
            _damageable = damageable;
            _multiplier = multiplier;

            if (GetComponent<PrimitiveObject>() == null)
            {
                Destroy(this);
                return;
            }

            if (!TryGetComponent(out NetworkIdentity netIdentity))
            {
                Destroy(this);
                return;
            }

            NetworkId = netIdentity.netId;
        }

        /// <inheritdoc />
        public uint NetworkId { get; private set; }

        /// <inheritdoc />
        public Vector3 CenterOfMass => transform.position;

        private DamageableExtension _damageable;
        private float _multiplier;
    }
    
    public class BroadcastOptions
    {
        public List<BroadcastMessage> Messages { get; set; }

        public class BroadcastMessage
        {
            public string Message { get; set; }
            public ushort Duration { get; set; }
            public BroadcastType Type { get; set; }
        }
    }

    public class SpawnItemOptions
    {
        public List<ItemSpawn> Items { get; set; }

        public class ItemSpawn
        {
            public ItemType Type { get; set; }
            public int Amount { get; set; }
        }
    }

    public class ExplosionOptions
    {
        public List<Explosion> Explosions { get; set; }

        public class Explosion
        {
            public float MaxRadius { get; set; }
            public float ScpDamageMultiplier { get; set; }
            public float BurnDuration { get; set; }
            public float ConcussionDuration { get; set; }
            public float FuseTime { get; set; }
        }
    }

    public class PlaySoundOptions
    {
        public List<Sound> Sounds { get; set; }

        public class Sound
        {
            public string ClipName { get; set; }
            public float Volume { get; set; }
            public bool IsSpatial { get; set; }
            public float MinDistance { get; set; }
            public float MaxDistance { get; set; }
        }
    }

    public class DynamicOptions
    {
        public List<DynamicExecution> Exec { get; set; }

        public class DynamicExecution
        {
            public string Code { get; set; }
        }
    }
}