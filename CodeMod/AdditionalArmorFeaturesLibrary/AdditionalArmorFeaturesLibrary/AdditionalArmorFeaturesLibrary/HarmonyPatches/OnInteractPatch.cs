using AdditionalArmorFeaturesLibrary.Collectible.Behavior;
using AdditionalArmorFeaturesLibrary.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace AdditionalArmorFeaturesLibrary.HarmonyPatches
{
    [HarmonyPatch(typeof(EntityAgent), nameof(EntityAgent.OnInteract))]
    public static class OnInteractPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var getAttackPower = AccessTools.Method(
                typeof(CollectibleObject),
                "GetAttackPower"
            );

            var getToolTier = AccessTools.Method(
                typeof(CollectibleObject),
                "GetToolTier"
            );

            var getStats = AccessTools.PropertyGetter(
                typeof(EntityAgent),
                "Stats"
            );

            var getBlended = AccessTools.Method(
                typeof(EntityStats),
                "GetBlended",
                new[] { typeof(string) }
            );

            for (int i = 0; i < codes.Count; i++)
            {
                // 1) Find GetAttackPower call
                if (codes[i].Calls(getAttackPower))
                {
                    // We jump forward until after damage is stored but BEFORE GetToolTier starts
                    int insertIndex = -1;

                    for (int j = i; j < codes.Count; j++)
                    {
                        // find first GetToolTier call → that's our boundary
                        if (codes[j].Calls(getToolTier))
                        {
                            insertIndex = j;
                            break;
                        }
                    }

                    if (insertIndex == -1)
                        return codes;

                    // Insert: damage += entity.Stats.GetBlended("damageBonus");

                    // load entity (byEntity = ldarg.1)
                    codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldarg_1));

                    // get Stats
                    codes.Insert(insertIndex + 1, new CodeInstruction(OpCodes.Callvirt, getStats));

                    // push string
                    codes.Insert(insertIndex + 2, new CodeInstruction(OpCodes.Ldstr, "damageBonus"));

                    // call GetBlended
                    codes.Insert(insertIndex + 3, new CodeInstruction(OpCodes.Callvirt, getBlended));

                    // add to damage (damage is still on stack via local OR reloaded)
                    // easiest safe method: reload local damage

                    // NOTE: we assume damage is in local 0 (common in VS builds, but safer would be local search)
                    codes.Insert(insertIndex + 4, new CodeInstruction(OpCodes.Ldloc_0));
                    codes.Insert(insertIndex + 5, new CodeInstruction(OpCodes.Add));
                    codes.Insert(insertIndex + 6, new CodeInstruction(OpCodes.Stloc_0));

                    break;
                }
            }

            return codes;
        }

    }
}
