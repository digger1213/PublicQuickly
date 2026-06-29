using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Utils;
using AdditionalArmorFeaturesLibrary.Network;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Server;

namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior

{
    public class CollectibleBehaviorFuel : CollectibleBehavior, IPowerSource
    {

#nullable enable
        private ICoreAPI? api { get; set; }
        public AdditionalArmorFeaturesLibrarySystem? NetWorkHandler { get; set; }

        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);

        [JsonProperty]
        public string PowerType { get; set; } = "fuel";
        [JsonProperty]
        public float? fuelCapacity;
        [JsonProperty]
        public bool? UseFuel { get; set; }
        [JsonProperty]
        public bool? OnCraftedFueled { get; set; }
        [JsonProperty]
        public Dictionary<string, float> FuelList { get; set; } = new();

        public CollectibleBehaviorFuel(CollectibleObject collObj) : base(collObj) { }

       

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            if (properties.Exists)
            {
                properties.Token.Populate(this);
            }
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;

            _ = collObj.Attributes;

            NetWorkHandler = api.ModLoader.GetModSystem<AdditionalArmorFeaturesLibrarySystem>();

            base.OnLoaded(api);

        }

        private void SetFuelOnCrafted(ItemSlot slot)
        {
            if (slot.Empty) return;

            SetPower(slot.Itemstack, GetHighestFuel());
        }


        public float GetHighestFuel()
        {
            return 50f;
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling)
        {
            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);

            if (!(outputSlot is DummySlot))
            {
                ItemStack itemStack = outputSlot.Itemstack;
                var fuelBehavior = itemStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
                if (fuelBehavior.OnCraftedFueled ?? false)
                {
                    SetFuelOnCrafted(outputSlot);
                }
            }
        }

        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority, ref EnumHandling handling)
        {
            var sinkFuelBehavior = sinkStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
            var sourceFuelBehavior = sourceStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
            if (priority == EnumMergePriority.DirectMerge)
            {
                if (!(sinkFuelBehavior?.UseFuel ?? false) )
                {
                    return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
                }

                float fuel = GetStackFuel(sourceStack, sinkStack);

                if (fuel <= 0)
                {
                    return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
                }

                double fuelHours = GetPower(sinkStack);

                handling = EnumHandling.PreventDefault;

            if (fuelHours >= (sinkFuelBehavior?.fuelCapacity ?? 0f))
                {
                    Console.WriteLine("You still have " + sinkStack.Attributes.GetDouble("fuelHours", 0) +  " fuel left");
                    return 0;
                }
                return 1;
            }
            return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
        }

        public override void TryMergeStacks(ItemStackMergeOperation op, ref EnumHandling handling)
        {
            Console.WriteLine("TryMergeCall!");
            if (op.CurrentPriority != EnumMergePriority.DirectMerge)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            if (op.SourceSlot.Empty || op.SinkSlot.Empty)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            ItemStack sourceStack = op.SourceSlot.Itemstack;
            ItemStack sinkStack = op.SinkSlot.Itemstack;
            var sinkFuelBehavior = sinkStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();

            if (sourceStack == null || sinkStack == null)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            if ((sinkFuelBehavior?.UseFuel ?? false))
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            float stackFuel = GetStackFuel(sourceStack, sinkStack);

            if (stackFuel <= 0f)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            double fuelHours = GetPower(sinkStack);

            if (fuelHours >= (sinkFuelBehavior?.fuelCapacity ?? 0f))
            {
                if (api?.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(
                        this,
                        "Itemfull",
                        Lang.Get(
                            "armorlib:ingameerror-item-full",
                            sinkStack.Collectible.GetHeldItemName(sinkStack)
                        )
                    );
                }

                handling = EnumHandling.PreventDefault;
                return;
            }

            AddPower(sourceStack, sinkStack, stackFuel);

            op.MovedQuantity = 1;
            op?.SourceSlot?.TakeOut(1);
            op?.SourceSlot?.MarkDirty();

            handling = EnumHandling.PreventDefault;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (inSlot == null || inSlot.Empty) return;

            ItemStack itemStack = inSlot.Itemstack;
            var stackBehavior = itemStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (!(stackBehavior?.UseFuel ?? false))
            {
                return;
            }

            var fuel = GetPower(itemStack).ToString("0.0");
            var capfuel = (stackBehavior?.fuelCapacity ?? 0f).ToString("0.0");


            dsc.AppendLine();
            dsc.AppendLine();

            dsc.AppendLine(Lang.Get("armorlib:fuel-hours", fuel, capfuel));

        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            if (inSlot.Empty) return base.GetHeldInteractionHelp(inSlot, ref handling);

            ItemStack itemStack = inSlot.Itemstack;
            var stackBehavior = itemStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();

            if ((stackBehavior?.UseFuel ?? false) == false || stackBehavior?.FuelList == null || stackBehavior?.FuelList.Count == 0)
            {
                return base.GetHeldInteractionHelp(inSlot, ref handling);
            }

            List<ItemStack> fuelStacks = new();

            foreach (var fuel in stackBehavior.FuelList)
            {
                string key = fuel.Key;
                if (string.IsNullOrEmpty(key)) continue;

                var asset = new AssetLocation(key);

                // Item

                var item = api?.World.GetItem(asset);

                if (item != null)
                {
                    fuelStacks.AddIfNotPresent(new ItemStack(item));
                }

                // Wildcard match
                var matches = api?.World.SearchItems(asset);

                if (matches != null)
                {
                    foreach (var match in matches)
                    {
                        fuelStacks.AddIfNotPresent(new ItemStack(match));
                    }
                }

            }

            if (fuelStacks.Count == 0)
            {
                return base.GetHeldInteractionHelp(inSlot, ref handling);
            }

            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "armorlib:heldhelp-fueltypes",
                    MouseButton = EnumMouseButton.None,
                    Itemstacks = fuelStacks.ToArray()
                }
            }
            .Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }

        public virtual float GetStackFuel(ItemStack sourceStack, ItemStack sinkStack)
        {
            if (sourceStack == null || sinkStack == null) return 0f;

            var sinkStackBehavior = sinkStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();

            if (!(sinkStackBehavior?.UseFuel ?? false)) return 0f;

            if (sinkStackBehavior?.FuelList == null || sinkStackBehavior?.FuelList.Count == 0) return 0f;

            AssetLocation sourceCode = sourceStack.Collectible.Code;

            foreach (var (code, value) in sinkStackBehavior.FuelList)
            {
                if (WildcardUtil.Match(new(code), sourceCode))
                {
                    return value;
                }
            }

            return 0f;
        }


        public virtual double GetPower(ItemStack sourceStack)
        {
            return sourceStack.Attributes.GetDouble("fuelHours", 0);
        }

        public virtual void SetPower(ItemStack sourceStack, double amount)
        {
            if (sourceStack == null) return;
            Console.WriteLine("Gets in SetPower");
            var sourceSinkStackBehavior = sourceStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();

            sourceStack.Attributes.SetDouble("fuelHours", GameMath.Clamp(amount, 0, sourceSinkStackBehavior?.fuelCapacity ?? 0f));
        }

        //Main fuel consumption. Usually constant.
        public virtual void ConsumePower(ItemSlot slot, EntityPlayer player, double amount)
        {
            if (slot.Empty) return;
            var powerPiece =
                slot.Itemstack.Collectible
                    .GetCollectibleBehavior<CollectibleBehaviorPower>(true);

            ItemStack stack = slot.Itemstack;

            if (powerPiece == null) return;


            // Only consume while active
            if (!powerPiece.PowerState(slot.Itemstack))
            {
                //Console.WriteLine("Power Off");
                return;
            }

            double fuel = GetPower(stack);

            SetPower(stack, fuel - amount);

            fuel = GetPower(stack);

            var fuelSyncPacket =  new FuelSyncPacket() { FuelHours = fuel, IdSlot = slot.Inventory.GetSlotId(slot), InvClass = slot.Inventory.InventoryID };

            NetWorkHandler?.ServerFuelChannel?.SendPacket(fuelSyncPacket, player as IServerPlayer);

            if (fuel <= 0)
            {
                powerPiece.SetPowerActive(
                    slot,
                    false,
                    player
                );


                var lightBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorLight>();
                //For all the extra passive features that would use power else.
                if (lightBehavior.RequiresPower)
                {
                    stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorLight>(true).SetLightActive(
                    slot,
                    false,
                    player
                );
                }
            }
        }

        //Single time fuel consumption for Feature Actions.
        public virtual void ActionConsumePower(ItemStack stack, EntityPlayer entityPlayer, double amount)
        {
            if (stack == null) return;
            var powerPiece = stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorPower>(true);

            if (powerPiece == null) return;

            double fuel = GetPower(stack);

            //Can't jump if not enough fuel.
            if (GetPower(stack) > amount)
            {
                return;
            }

            SetPower(stack, fuel - amount);

            fuel = GetPower(stack);
        }

        public virtual void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount)
        {
            if (sourceStack == null) return;

            SetPower(sinkStack, GetPower(sinkStack) + amount);
        }

        public virtual bool HasPower(ItemStack sourceStack)
        {
            return GetPower(sourceStack) > 0;
        }

        public virtual bool IsFull(ItemStack sourceStack)
        {
            var stackBehavior = sourceStack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
            return GetPower(sourceStack) >= (stackBehavior?.fuelCapacity ?? 0f);
        }
    }
}