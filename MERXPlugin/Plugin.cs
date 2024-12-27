using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using MapEditorReborn.API.Features.Objects;
using MapEditorReborn.Events.EventArgs;
using MapEditorReborn.Events.Handlers;
using MERXPlugin.Features;
using MERXPlugin.Features.Enums;
using UnityEngine;

namespace MERXPlugin
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name { get; } = "MERXPlugin";
        public override string Author { get; } = "Cocoa";
        public override string Prefix { get; } = "MERXPlugin";
        public override Version Version { get; } = new(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new(9, 0, 0);

        public override void OnEnabled()
        {
            base.OnEnabled();

            Instance = this;

            Schematic.SchematicSpawned += OnSchematicSpawned;
        }

        public override void OnDisabled()
        {
            Schematic.SchematicSpawned -= OnSchematicSpawned;

            Instance = null;

            base.OnDisabled();
        }

        public Dictionary<SchematicObject, Dictionary<int, Transform>> IdToTransform { get; } = new();

        private Dictionary<ExtensionType, SchematicExtensionDecompilerBase> _extensionDecompilers;

        private void LoadDecompilers()
        {
            _extensionDecompilers = new();

            var decompilerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SchematicExtensionDecompilerBase)));

            foreach (var decompilerType in decompilerTypes)
            {
                var instance = (SchematicExtensionDecompilerBase) Activator.CreateInstance(decompilerType);

                if (_extensionDecompilers.ContainsKey(instance.Type))
                    Log.Warn($"Duplicate decompiler for extension type {instance.Type} found in {decompilerType.Assembly.FullName}");
                else
                    _extensionDecompilers.Add(instance.Type, instance);
            }
        }

        private void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
        {
            if (_extensionDecompilers == null)
                LoadDecompilers();

            foreach (var decompiler in _extensionDecompilers!)
            {
                if (decompiler.Value.Decompile(ev.Schematic, ev.Name))
                    Log.Debug($"Decompiled extension {decompiler.Key} in schematic {ev.Name}");
            }
        }
    }
}