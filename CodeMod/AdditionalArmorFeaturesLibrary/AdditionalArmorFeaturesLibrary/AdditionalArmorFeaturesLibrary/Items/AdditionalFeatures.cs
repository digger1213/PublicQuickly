using AdditionalArmorFeaturesLibrary.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace AdditionalArmorFeaturesLibrary.Items
{
    public class ItemAdditionalFeatures : Item
    {
        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack)
        {
            if (!stack.Attributes.GetBool("togglelight"))
            {
                return new byte[] { 0, 0, 0 };
            }

            return ArmorFeaturesProp.ReadFrom(stack)?.lightHSV
                ?? new byte[] { 0, 0, 0 };
        }

        //For when Equiping/Deequiping armors.
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);

            if (slot?.Inventory is InventoryBasePlayer inv)
            {
                var player = inv.Player as EntityPlayer;
                if (player == null) return;

                // Check if this is a gear slot
                if (slot.Inventory is InventoryCharacter invChar)
                {
                    HandleGearChange(player, slot, extractedStack);
                }
            }
        }

        private void HandleGearChange(EntityPlayer player, ItemSlot slot, ItemStack oldStack)
        {
            if (player == null || slot == null) return;

            ItemStack newStack = slot.Itemstack;

            // UNEQUIP
            if (oldStack != null && newStack == null)
            {
                ApplyItemStats(player, oldStack, remove: true);
                return;
            }

            // EQUIP
            if (oldStack == null && newStack != null)
            {
                ApplyItemStats(player, newStack, remove: false);
                return;
            }

            // SWAP (replace item)
            if (oldStack != null && newStack != null)
            {
                ApplyItemStats(player, oldStack, remove: true);
                ApplyItemStats(player, newStack, remove: false);
            }
        }

        private void ApplyItemStats(EntityPlayer player, ItemStack stack, bool remove)
        {
            if (stack?.Collectible == null) return;

            float bonus = stack.Collectible.Attributes?["damageBonus"]?.AsFloat(0) ?? 0;
            string key = "damageBonus";
            float current = player.WatchedAttributes.GetFloat(key);

            if (remove)
                current -= bonus;
            else
                current += bonus;

            player.WatchedAttributes.SetFloat(key, current);
        }

    }
}
