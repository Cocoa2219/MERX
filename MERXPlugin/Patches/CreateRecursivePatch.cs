using System.Collections.Generic;
using System.Reflection.Emit;
using Exiled.API.Features.Pools;
using HarmonyLib;
using MapEditorReborn.API.Features.Objects;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace MERXPlugin.Patches;

[HarmonyPatch(typeof(SchematicObject), nameof(SchematicObject.CreateRecursiveFromID))]
public class CreateRecursivePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);
        var index = newInstructions.FindIndex(instruction =>
            instruction.Calls(PropertyGetter(typeof(Component), nameof(Component.transform))));
        index += 2;

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Call, Method(typeof(CreateRecursivePatch), nameof(SaveObject))),
        });

        foreach (var instruction in newInstructions)
        {
            yield return instruction;
        }

        ListPool<CodeInstruction>.Pool.Return(newInstructions);
    }

    private static void SaveObject(SchematicObject schematicObject, int id, Transform transform)
    {
        var idToTransform = Plugin.Instance.IdToTransform;

        if (!idToTransform.TryGetValue(schematicObject, out var transforms))
        {
            transforms = new Dictionary<int, Transform>();
            idToTransform[schematicObject] = transforms;
        }

        transforms[id] = transform;
    }
}