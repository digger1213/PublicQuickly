using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior
{

#nullable enable

    class CollectibleBehaviorJetpack : CollectibleBehavior
    {
        private ICoreAPI? api { get; set; }
        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);
        public ParticleEmitter particleEmitter = new ParticleEmitter();

        [JsonProperty]
        public bool RequiresPower { get; set; } = true;
        [JsonProperty]
        public string? jetpackSoundPath { get; set; }
        [JsonProperty]
        public double jetMaxUpwardVel = 0.25;
        [JsonProperty]
        public double jetUpwardVel = 0.03;
        [JsonProperty]
        public double jetConsumption = 0;

        [JsonProperty]
        public ParticleEntry[] particlesList { get; set; } = Array.Empty<ParticleEntry>();

        public CollectibleBehaviorJetpack(CollectibleObject collObj) : base(collObj)
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

        public bool JetpackState(ItemStack stack)
        {
            return stack.Attributes.GetBool("togglejetpack");
        }

        public virtual void SetJetpackActive(ItemSlot slot, bool active, EntityPlayer player)
        {
            if (slot == null || slot.Empty) return;

            ItemStack stack = slot.Itemstack;

            // Update state
            stack.Attributes.SetBool("togglejetpack", active);

            slot.MarkDirty();
        }


        public virtual void FlyJetpack(ItemSlot slot, EntityPlayer player)
        {
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;
            //Toggle required
            if (!JetpackState(stack)) return;

            //Only if player is holding jump
            if (!player.Controls.Jump) return;

            var source = stack.Collectible.GetCollectibleInterface<IPowerSource>();
            if (source == null || !source.HasPower(stack))
            {
                return;
            }

            var stackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorJetpack>();

            //Propels person, also limits speed.
            player.Pos.Motion.Y = Math.Min(player.Pos.Motion.Y + (stackBehavior.jetUpwardVel), stackBehavior.jetMaxUpwardVel);

            //Any particles set?
            if (stackBehavior.particlesList.Length > 0)
            {
                particleEmitter.EmitParticles(api, player, stack, stackBehavior.particlesList);
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
    }
}