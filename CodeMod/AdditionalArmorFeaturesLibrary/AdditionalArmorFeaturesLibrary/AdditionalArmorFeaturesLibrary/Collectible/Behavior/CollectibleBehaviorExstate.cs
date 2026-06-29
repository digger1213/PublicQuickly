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

    class CollectibleBehaviorExstate : CollectibleBehavior
    {

        private ICoreAPI? api { get; set; }

        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);

        [JsonProperty]
        public string? exstateSoundPath { get; set; }

        public CollectibleBehaviorExstate(CollectibleObject collObj) : base(collObj)
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

        public bool ExstateState(ItemStack stack)
        {
            Console.WriteLine("State trigger");
            return stack.Attributes.GetBool("toggleexstate");
        }

        public virtual void SwitchExstatestate(ItemSlot slot, bool active, EntityPlayer player)
        {
            Console.WriteLine("Switching state");
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;

            // Play toggle sound
            if (player != null)
            {
                var exstateBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorExstate>();
                string soundPath = exstateBehavior.exstateSoundPath ?? string.Empty;

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
            stack.Attributes.SetBool("toggleexstate", active);

            string currentCode = stack.Collectible.Code.ToString();

            string newCode = active
                ? currentCode.Replace("-exstateone", "-exstatetwo")
                : currentCode.Replace("-exstatetwo", "-exstateone");

            Item? newItem = api.World.GetItem(new AssetLocation(newCode));

            if (newItem != null)
            {
                ITreeAttribute clonedAttributes = stack.Attributes.Clone();

                slot.Itemstack = new ItemStack(newItem)
                {
                    Attributes = clonedAttributes
                };
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