using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace AdditionalArmorFeaturesLibrary
{
    public class ArmorStats
    {
        public bool NeedsFuel { get; set; } = true;

        public float FuelCapacityHours { get; set; } = 30f;

        public float FuelEfficiency { get; set; } = 1f;

        public string FuelAttribute { get; set; } = "fuelHours";

        public string[] RefuelBagWildcard { get; set; } = new string[1] { "armor*" };

        public Dictionary<string, float> FlatReduction { get; set; } = new Dictionary<string, float>();

        public Dictionary<string, float> StatsWhenTurnedOn { get; set; } = new Dictionary<string, float>();

        public Dictionary<string, float> StatsWhenTurnedOff { get; set; } = new Dictionary<string, float>();


    }
}
