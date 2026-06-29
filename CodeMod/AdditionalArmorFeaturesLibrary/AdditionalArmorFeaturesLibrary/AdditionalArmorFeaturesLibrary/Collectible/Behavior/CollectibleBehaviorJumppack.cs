using AdditionalArmorFeaturesLibrary.Collectible.Behavior;
using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Essentials;

namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior
{

#nullable enable

    class CollectibleBehaviorJumppack : CollectibleBehavior
    {

        private ICoreAPI? api { get; set; }
        public ArmorFeaturesProp? armorFeaturesProp => ArmorFeaturesProp.ReadFrom(this.collObj);
        public ParticleEmitter particleEmitter = new ParticleEmitter();

        //Jumppack Var.
        [JsonProperty]
        public bool RequiresPower { get; set; } = true;
        [JsonProperty]
        public double jumpForwardVel = 0;
        [JsonProperty]
        public double jumpUpwardVel = 0;
        [JsonProperty]
        public double jumpDelay = 0;
        [JsonProperty]
        public double jumpConsumption = 0;
        [JsonProperty]
        public string? jumppackSoundPath { get; set; }
        [JsonProperty]
        public ParticleEntry[] particlesList { get; set; } = Array.Empty<ParticleEntry>();

        public CollectibleBehaviorJumppack(CollectibleObject collObj) : base(collObj)
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

        public bool JumppackState(ItemStack stack)
        {
            Console.WriteLine("jumppack trigger");
            return stack.Attributes.GetBool("togglejumppack");
        }

        public virtual void SetJumppackActive(ItemSlot slot, bool active, EntityPlayer player)
        {
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;

            // Update state
            stack.Attributes.SetBool("togglejumppack", active);

            slot.MarkDirty();
        }

        public virtual void JumpJumppack(ItemSlot slot, EntityPlayer player)
        {
            if (slot == null || slot.Empty || api == null) return;

            ItemStack stack = slot.Itemstack;
            var stackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorJumppack>();

            //Only continue if jumppack is active.
            if (!JumppackState(stack)) return;

            //NEED TO ADD DELAY STILL
            long lastActivation = slot.Itemstack.TempAttributes.GetLong("jumppackLastUse");
            long now = api.World.ElapsedMilliseconds;

            if (now - lastActivation < (stackBehavior.jumpDelay * 1000))
            {
                return;
            }
            slot.Itemstack.TempAttributes.SetLong("jumppackLastUse", now);

            //TILL HERE

            // Play toggle sound
            if (player != null)
            {
                string soundPath = stackBehavior.jumppackSoundPath ?? string.Empty;

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


            Vec3f eyes = player.Pos.GetViewVector();
            Console.WriteLine(eyes);
            player.Pos.Motion.Y = (stackBehavior.jumpUpwardVel) * 0.1;
            player.Pos.Motion.X = (stackBehavior.jumpForwardVel) * 0.1 * eyes.X;
            player.Pos.Motion.Z = (stackBehavior.jumpForwardVel) * 0.1 * eyes.Z;

            //Consumes fuel, only when feature is enabled.
            var fuelBehavior = stack.Collectible.GetCollectibleBehavior<CollectibleBehaviorFuel>(true);
            var jumppackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorJumppack>();
            if (jumppackBehavior.RequiresPower)
            {
                fuelBehavior.ActionConsumePower(stack, player, stackBehavior.jumpConsumption);
            }
            

            //Any particles set?
            if (stackBehavior.particlesList.Length > 0)
            {
                particleEmitter.EmitParticles(api, player, stack, stackBehavior.particlesList);
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