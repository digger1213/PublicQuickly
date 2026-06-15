using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdditionalArmorFeaturesLibrary.Utils
{
    public class ArmorFeaturesProp
    {

        //public string jetSoundPath { get; set; } = string.Empty;

        //public float FuelCapacityHours { get; set; } = 24f;
        //public float FuelEfficiency { get; set; } = 1f;
        //public string FuelAttribute { get; set; } = "nightVisionFuelHours";
        //public string ToggleAttribute { get; set; } = "turnedOn";
        //public bool ConsumeFuelWhileSleeping { get; set; } = false;
        //public byte[] LightHsv { get; set; } = [0, 0, 0];
        //public byte[] TurnedOffLightHsv { get; set; } = [0, 0, 0];
        //public float MaxFuelWasteFraction { get; set; } = 0.5f;
        //public string LightHsvVariantCode { get; set; } = "lining";
        //public bool CanBeTurnedOff { get; set; } = true;

        //Current Stats
        public string lightSoundPath { get; set; } = string.Empty;

        public float fuelCapacity = 24f;
        public string PowerType { get; set; } = "fuel";
        public bool UseFuel { get; set; } = true;
        public bool OnCraftedFueled { get; set; } = false;
        public Dictionary<string, float> FuelList { get; set; } = new();


        public static ArmorFeaturesProp? ReadFrom(ItemStack itemStack)
        {
            if (itemStack == null) return null;

            ArmorFeaturesProp? armorFeaturesProp = itemStack.Collectible.Attributes?["armorFeaturesProp"]?.AsObject<ArmorFeaturesProp>();

            if (armorFeaturesProp == null)
            {
                return null;
            }

            return armorFeaturesProp;
        }

        public static ArmorFeaturesProp? ReadFrom(CollectibleObject colObj)
        {
            if (colObj == null)
            {
                return null;
            }

            ArmorFeaturesProp? armorFeaturesProp = colObj.Attributes?["armorFeaturesProp"]?.AsObject<ArmorFeaturesProp>();

            if (armorFeaturesProp == null)
            {
                return null;
            }

            return armorFeaturesProp;
        }

        public class ParticleProp
        {
            public Vec3d? BasePos { get; set; }

            public AdvancedParticleProperties[] Particle { get; set; } = Array.Empty<AdvancedParticleProperties>();

        }
    }
}
