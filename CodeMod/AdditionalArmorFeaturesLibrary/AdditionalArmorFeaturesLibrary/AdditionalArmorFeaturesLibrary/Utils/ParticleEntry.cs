using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.MathTools;

namespace AdditionalArmorFeaturesLibrary.Utils
{
    public class Argb
    {
        public int alpha { get; set; } = 255;
        public int red { get; set; } = 0;
        public int green { get; set; } = 0;
        public int blue { get; set; } = 0;
    }
    public class ParticleEntry
    {
        public string attachmentPointName { get; set; } = string.Empty;
        public int particleCount { get; set; } = 0;
        public Argb color { get; set; }
        public Vec3f minVelocity { get; set; } = new Vec3f(0.0f, 0.0f, 0.0f);
        public Vec3f maxVelocity { get; set; } = new Vec3f(0.0f, 0.0f, 0.0f);
        public float lifeLength { get; set; } = 1;
        public float gravity { get; set; } = 0;
        public float particleSize { get; set; } = 1;

    }
}
