using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using AdditionalArmorFeaturesLibrary.Collectible.Behavior;

namespace AdditionalArmorFeaturesLibrary.Utils
{
    internal class JetpackSoundHelper
    {
        protected ILoadedSound jetSound;
        public void ToggleJetpackSounds(ICoreAPI api, EntityPlayer player, ItemStack stack, bool on)
        {
            if (api.Side != EnumAppSide.Client) return;

            if (on)
            {
                if (jetSound == null || !jetSound.IsPlaying)
                {
                    var jetpackBehavior = stack.Collectible.GetBehavior<CollectibleBehaviorJetpack>();
                    jetSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation(jetpackBehavior.jetpackSoundPath),
                        ShouldLoop = true,
                        Position = new Vec3f((float)player.Pos.X + 0.5f, (float)player.Pos.Y + 0.75f, (float)player.Pos.Z + 0.5f),
                        DisposeOnFinish = false,
                        Volume = 1
                    });

                    jetSound.Start();
                }
            }
            else
            {
                if (jetSound != null) 
                {
                    jetSound.Stop();
                    jetSound.Dispose();
                    jetSound = null;
                }
            }
        }
    }
}
