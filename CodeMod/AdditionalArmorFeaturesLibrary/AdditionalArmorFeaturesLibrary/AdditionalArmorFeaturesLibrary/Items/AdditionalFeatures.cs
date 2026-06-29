using AdditionalArmorFeaturesLibrary.Collectible.Behavior;
using AdditionalArmorFeaturesLibrary.Utils;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace AdditionalArmorFeaturesLibrary.Items
{
    public class ItemAdditionalFeatures : Item
    {
        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack)
        {
            var stackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorLight>();

            if (!stack.Attributes.GetBool("togglelight"))
            {
                return new byte[] { 0, 0, 0 };
            }

            return stackBehavior.lightHSV
                ?? new byte[] { 0, 0, 0 };
        }

    }
}
