using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

#nullable enable

namespace AdditionalArmorFeaturesLibrary.Collectible.Behavior
{
    class CollectibleBehaviorCustomStats : CollectibleBehavior
    {
        [JsonProperty]
        public bool RequiresPower { get; set; } = true;

        [JsonProperty]
        public float FallDamageModifier { get; set; } = 0;

        [JsonProperty]
        public Dictionary<string, float> Stats { get; set; } = new();

        public CollectibleBehaviorCustomStats(CollectibleObject collObj) : base(collObj)
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

        public virtual Dictionary<string, float>? GetStats(ItemStack item)
        {
            if (!RequiresPower) return Stats;

            //check for power
            var behaviorPower = item.Collectible.GetCollectibleBehavior<CollectibleBehaviorPower>(true);
            if (behaviorPower == null || !behaviorPower.PowerState(item)) return null;

            return Stats;
        }

        public virtual float GetFallDamageModifier(ItemStack item)
        {
            if (!RequiresPower) return FallDamageModifier;

            //check for power
            var behaviorPower = item.Collectible.GetCollectibleBehavior<CollectibleBehaviorPower>(true);
            if (behaviorPower == null || !behaviorPower.PowerState(item)) return 0;

            return FallDamageModifier;
        }
    }
}