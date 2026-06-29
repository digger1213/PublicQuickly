using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior
{
    internal class CollectibleBehaviorPower : CollectibleBehavior
    {
        private ICoreAPI? api { get; set; }

        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);
        [JsonProperty]
        public string? powerSoundPath { get; set; }

        public CollectibleBehaviorPower(CollectibleObject collObj) : base(collObj)
        {
        }
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

            base.OnLoaded(api);
        }

        public bool PowerState(ItemStack stack)
        {
            //Console.WriteLine("PowerState trigger");
            return stack.Attributes.GetBool("togglepower");
        }

        public virtual void SetPowerActive(ItemSlot slot, bool active, EntityPlayer player)
        {
            Console.WriteLine("SetPowerActive");
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;

            bool hasFuel = collObj.GetCollectibleInterface<IPowerSource>()?.HasPower(stack) ?? false;

            // Only block activation if no fuel
            var fuelBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
            if (active && !hasFuel && (fuelBehavior?.UseFuel ?? false))
            {
                if (api?.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(
                        this,
                        "itemnofuel",
                        Lang.Get(
                            "additionalarmorefeatureslibrary:ingameerror-item-nofuel",
                            stack.Collectible.GetHeldItemName(stack)
                        )
                    );
                }

                return;
            }

            // Play toggle sound

            if (player != null)
            {
                var powerBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorPower>();
                string soundPath = powerBehavior.powerSoundPath ?? string.Empty;

                if (!string.IsNullOrEmpty(soundPath))
                {
                    player.World.PlaySoundAt(
                        new AssetLocation(soundPath),
                        player.Pos.X + 0.5,
                        player.Pos.Y + 0.75,
                        player.Pos.Z + 0.5,
                        null,
                        randomizePitch: false,
                        volume: 1f
                    );
                }
            }

            // Update state
            stack.Attributes.SetBool("togglepower", active);

            string currentCode = stack.Collectible.Code.ToString();

            string newCode = active
                ? currentCode.Replace("-powerisoff", "-powerison")
                : currentCode.Replace("-powerison", "-powerisoff");

            Item? newItem = api.World.GetItem(new AssetLocation(newCode));

            if (newItem != null)
            {
                ITreeAttribute clonedAttributes = stack.Attributes.Clone();

                slot.Itemstack = new ItemStack(newItem)
                {
                    Attributes = clonedAttributes
                };
            }

            var lightBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorLight>();
            //Turn off all toggleables if FeaturesUsePower
            if (!active && lightBehavior.RequiresPower)
            {
                lightBehavior?.SetLightActive(slot, false, player);

            }


            slot.MarkDirty();
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[2]
            {
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-toggle-activeslot"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "togglePower"
                },
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-toggle-gearslot"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "toggleHoveredGearPower"

                }
            }.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }
    }
}
