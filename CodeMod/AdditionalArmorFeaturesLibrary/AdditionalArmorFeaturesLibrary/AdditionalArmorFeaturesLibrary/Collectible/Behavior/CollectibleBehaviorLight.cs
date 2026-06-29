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

        //public override void OnLoaded(ICoreAPI api)
        //{
        //    this.api = api;

        //    base.OnLoaded(api);
        //}

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

            // Only block activation if no fuel
            if (active && !hasFuel && (ArmorFeaturesProp.ReadFrom(stack).UseFuel ?? false) && stackBehavior.RequiresPower)
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

            ToggleLightHsv(slot.Itemstack);

            slot.MarkDirty();
        }

        public virtual void ToggleLightHsv(ItemStack itemStack)
        {
            var stackBehavior = itemStack.Collectible.GetBehavior<CollectibleBehaviorLight>();

            Console.WriteLine(stackBehavior.lightSoundPath);
            //If the Toggle light is turned on, go in.
            if (itemStack.Attributes.GetBool("togglelight"))
            { //Check if the value actually exists.
                if (stackBehavior.lightHSV.Length > 0)
                {//set LightHsv of item to the custom LightHSV variable.
                    itemStack.Collectible.LightHsv = stackBehavior.lightHSV;
                }
                else
                {
                    itemStack.Collectible.LightHsv = new byte[] { 0, 0, 0 };
                }
            }
            else
            { //If the Toggle light is turned off, turn light off.
                itemStack.Collectible.LightHsv = new byte[] { 0, 0, 0 };
            }
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
            
        public virtual byte[] GetCustomLightHsv(ItemStack item)
        {
            if (!RequiresPower) return lightHSV;

            //check for power
            var behaviorPower = item.Collectible.GetCollectibleBehavior<CollectibleBehaviorPower>(true);
            if (behaviorPower == null || !behaviorPower.PowerState(item)) return new byte[] { 0, 0, 0 };

            return lightHSV;
        }

        public virtual string GetLightSoundPath(ItemStack item)
        {
            return lightSoundPath;
        }

    }
}