using AdditionalArmorFeaturesLibrary.Utils;
using AdditionalArmorFeaturesLibrary.Config;
using ProperVersion;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AdditionalArmorFeaturesLibrary.Util
{
    public static class AdditionalArmorFeaturesLibraryConfigHelper
    {

    }

    public class LoggerExt
    {
        public static void SendLogger(ICoreClientAPI capi, string[] logs)
        {
            if (capi == null) return;

            var match = capi.Settings.Bool.Get("developerMode");

            if (match)
            {
                foreach (var log in logs)
                {
                    if (string.IsNullOrEmpty(log)) continue;

                    capi.Logger.Debug(log);

                }
            }
        }
    }

}