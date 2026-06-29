using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior
{

#nullable enable

    class CollectibleBehaviorLight : CollectibleBehavior
    {
        private ICoreAPI? api { get; set; }

        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);


        [JsonProperty]
        public bool RequiresPower { get; set; } = true;
        [JsonProperty]
        public string? lightSoundPath { get; set; }
        [JsonProperty]
        public byte[] lightHSV { get; set; } = new byte[] { 0, 0, 0 };

        public CollectibleBehaviorLight(CollectibleObject collObj) : base(collObj)
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

        public bool LightState(ItemStack stack)
        {
            Console.WriteLine("Lightstate trigger");
            return stack.Attributes.GetBool("togglelight");
        }

        public virtual void SetLightActive(ItemSlot slot, bool active, EntityPlayer player)
        {
            Console.WriteLine("SetLightActive");
            if (slot == null || slot.Empty) return;

            ItemStack stack = slot.Itemstack;
            var stackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorLight>();

            bool hasFuel = collObj.GetCollectibleInterface<IPowerSource>()?.HasPower(stack) ?? false;

            var fuelBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorFuel>();
            // Only block activation if no fuel
            if (active && !hasFuel && (fuelBehavior.UseFuel ?? false) && stackBehavior.RequiresPower)
            {
                if (api?.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(
                        this,
                        "lightnofuel",
                        Lang.Get(
                            "additionalarmorefeatureslibrary:ingameerror-item-nofuel",
                            stack.Collectible.GetHeldItemName(stack)
                        )
                    );
                }

                return;
            }

            //Check if it needs to be on to even be toggled.
            //If light is on, have ability to turn it off still (for auto turn off)
            if (!stack.Attributes.GetBool("togglelight")){
                //When armor is off and features require power, it doesn't turn on.
                if (stackBehavior.RequiresPower && !stack.Attributes.GetBool("togglepower")){ 
                    return;
                }
            }
            

            // Play toggle sound
            if (player != null)
            {
                string soundPath = stackBehavior.lightSoundPath ?? string.Empty;

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
            stack.Attributes.SetBool("togglelight", active);

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
                    HotKeyCode = "toggleLight"
                },
                new WorldInteraction
                {
                    ActionLangCode = Lang.GetMatching("awearablelight:heldhelp-toggle-gearslot"),
                    MouseButton = EnumMouseButton.None,
                    HotKeyCode = "toggleHoveredGearLight"

                }
            }.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
        }

    }
}