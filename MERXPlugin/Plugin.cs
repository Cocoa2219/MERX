using System;
using System.IO;
using Exiled.API.Features;
using MapEditorReborn.Events.EventArgs;
using MapEditorReborn.Events.Handlers;

namespace MERXPlugin
{
    public class Plugin : Plugin<Config>
    {
        public override string Name { get; } = "MERXPlugin";
        public override string Author { get; } = "Cocoa";
        public override string Prefix { get; } = "MERXPlugin";
        public override Version Version { get; } = new(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new(9, 0, 0);

        public override void OnEnabled()
        {
            base.OnEnabled();

            Schematic.SchematicSpawned += OnSchematicSpawned;
        }

        public override void OnDisabled()
        {
            Schematic.SchematicSpawned -= OnSchematicSpawned;

            base.OnDisabled();
        }

        private void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
        {

        }
    }
}