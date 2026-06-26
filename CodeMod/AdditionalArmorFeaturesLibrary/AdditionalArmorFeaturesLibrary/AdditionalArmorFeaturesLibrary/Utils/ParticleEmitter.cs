using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using static AdditionalArmorFeaturesLibrary.Utils.ArmorFeaturesProp;

namespace AdditionalArmorFeaturesLibrary.Utils
{
    internal class ParticleEmitter
    {
        //Handles the particle emission for the items.
        public void EmitParticles(ICoreAPI api, EntityPlayer player, ItemStack stack)
        {
            //Get all the particles.
            for (int i = 0; i < (ArmorFeaturesProp.ReadFrom(stack).particlesList.Length); i++)
            {
                Console.WriteLine(ArmorFeaturesProp.ReadFrom(stack).particlesList[i].attachmentPointName);

                //May god help us.
                var AttachPointPose = player?.AnimManager?.Animator?.GetAttachmentPointPose(ArmorFeaturesProp.ReadFrom(stack).particlesList[i].attachmentPointName); //"RightHand"
                var AttachPoint = AttachPointPose?.AttachPoint;

                if (player is null || AttachPointPose is null || AttachPoint is null) return;

                //To rotate the particles with the body proper.
                float bodyYaw = player.BodyYaw + MathF.PI / 2;

                //May god help us again.
                float[] ModelMat = Mat4f.Create();
                Matrixf particleMatrix = new Matrixf().Set(ModelMat)
                    .Rotate(0f, bodyYaw, 0f)
                    .Translate(-0.5f, 0f, -0.5f) //wtf magic offset???
                    .Mul(AttachPointPose.AnimModelMatrix);

                Vec3d particleOffset = particleMatrix.TransformVector(new Vec4f(0.0f, 0f, 0.0f, 1f)).XYZ.ToVec3d();
                Vec3d localPoint = particleOffset + player.Pos.XYZ;

                Argb color = ArmorFeaturesProp.ReadFrom(stack).particlesList[i].color;

                if (localPoint != null)
                {

                    api.World.SpawnParticles(
                        4,                      // quantity
                        ColorUtil.ToRgba(color.alpha, color.red, color.green, color.blue),    // color
                        localPoint,
                        localPoint,
                        ArmorFeaturesProp.ReadFrom(stack).particlesList[i].minVelocity,  // minmotion
                        ArmorFeaturesProp.ReadFrom(stack).particlesList[i].maxVelocity, // maxmotion
                        ArmorFeaturesProp.ReadFrom(stack).particlesList[i].lifeLength,  // life length
                        ArmorFeaturesProp.ReadFrom(stack).particlesList[i].gravity, // gravity
                        ArmorFeaturesProp.ReadFrom(stack).particlesList[i].particleSize  // size 
                        );
                }

            }
        }
    }
}
