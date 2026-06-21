using AdditionalArmorFeaturesLibrary.Collectible.Behavior;
using AdditionalArmorFeaturesLibrary.Config;
using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Items;
using AdditionalArmorFeaturesLibrary.Network;
using AdditionalArmorFeaturesLibrary.Util;
using AdditionalArmorFeaturesLibrary.Utils;
using AdditionalArmorFeaturesLibrary.HarmonyPatches;
using HarmonyLib;
using ProtoBuf;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AdditionalArmorFeaturesLibrary;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class TogglePacket
{
    public string HotKeyCode { get; set; } = "";
}

public partial class AdditionalArmorFeaturesLibrarySystem : ModSystem
{

    //To override bug in vanilla -Cry emoji-
    private Harmony harmonyInstance => new(Mod.Info.ModID);

    private static readonly string ConfigServerName = "additionalarmorfeatureslibrary-server.json";

    private static readonly string ConfigClientName = "additionalarmorfeatureslibrary-client.json";

    public static Server? ServerConfig { get; set; }

    public static Client? ClientConfig { get; set; }

    public long OnLongRefreshTick { get; set; }
    public long OnLongServerFuelTick { get; set; }
    public long OnLongServerTick { get; set; }

    private ICoreServerAPI? Sapi { get; set; }
    private ICoreClientAPI? Capi { get; set; }

    private IClientNetworkChannel? ClientToggleChannel { get; set; }

    private IServerNetworkChannel? ServerToggleChannel { get; set; }

    public ConfigSyncSystem? ConfigSync { get; set; }


    double lastCheckTotalHours;

  
    public override void StartPre(ICoreAPI api)
    {
        //All to make some lights work...........
        harmonyInstance.Patch(
        AccessTools.Method(typeof(EntityBehaviorContainer), nameof(EntityBehaviorContainer.OnTesselation)),
        postfix: AccessTools.Method(typeof(LightRenderPatch), nameof(LightRenderPatch.OnTesselationPatch)));
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        if (api.Side == EnumAppSide.Server)
        {
            ServerConfig = new LoadOrCreate().SapiConfig(api, ConfigServerName);
            AdditionalArmorFeaturesLibraryConfigHelper.Init(ServerConfig);

            if (AdditionalArmorFeaturesLibraryConfigHelper.oldVersion >= ServerConfig.Version)
            {
                AdditionalArmorFeaturesLibraryConfigHelper.MigrateConfig(api);
            }
        }

        ConfigSync = new ConfigSyncSystem(
            api,
            api.Side == EnumAppSide.Server ? ServerConfig : null
        );

        api.RegisterItemClass("additionalfeatures", typeof(ItemAdditionalFeatures));

        api.RegisterCollectibleBehaviorClass("additionalarmorfeatureslibrary:Power", typeof(CollectibleBehaviorPower));
        api.RegisterCollectibleBehaviorClass("additionalarmorfeatureslibrary:Light", typeof(CollectibleBehaviorLight));
        api.RegisterCollectibleBehaviorClass("additionalarmorfeatureslibrary:Fuel", typeof(CollectibleBehaviorFuel));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        Capi = api;

        ClientConfig = new LoadOrCreate().CapiConfig(api, ConfigClientName);

        ClientToggleChannel = api.Network
           .RegisterChannel("additionalarmorfeatureslibrarytoggle")
           .RegisterMessageType<AdditionalArmorFeaturesLibraryPacket>();

        api.Input.RegisterHotKey("togglePower", Lang.Get("additionalarmorfeatureslibrary:keybind-activeslot-description-power"), GlKeys.P);
        api.Input.SetHotKeyHandler("togglePower", _ => OnTogglePowerHotkey(api.World.Player));

        api.Input.RegisterHotKey("toggleLight", Lang.Get("additionalarmorfeatureslibrary:keybind-activeslot-description-light"), GlKeys.L);
        api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        Sapi = api;

        ServerToggleChannel = api.Network
            .RegisterChannel("additionalarmorfeatureslibrarytoggle")
            .RegisterMessageType<AdditionalArmorFeaturesLibraryPacket>()
            .SetMessageHandler<AdditionalArmorFeaturesLibraryPacket>(OnTogglePacket);


        api.Event.PlayerNowPlaying += (player) =>
        {
            ConfigSync?.SendToPlayer(player);
        };

        OnLongServerFuelTick = api.Event.RegisterGameTickListener(OnServerFuelTick, 2000);
        OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 100);
    }

    //To distinguish between what to toggle.
    private void OnTogglePacket(IServerPlayer player, AdditionalArmorFeaturesLibraryPacket packet)
    {
        switch (packet.Toggle)
        {
            case ToggleType.Power:
                TogglePowerWearableItem(player, packet.ItemSlot);
                break;

            case ToggleType.Light:
                ToggleLightWearableItem(player, packet.ItemSlot);
                break;
        }
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(Mod.Info.ModID);
    }

    private void OnServerTick(float obj)
    {
        if (Sapi == null) return;

    }
    
    private void OnServerFuelTick(float dt)
    {
        if (Sapi == null) return;

        double totalHours = Sapi.World.Calendar.TotalHours;
        double hoursPassed = totalHours - lastCheckTotalHours;
        double Consumption = hoursPassed;

        hoursPassed = Math.Min(hoursPassed, 0.5);

        if (hoursPassed <= 0)
        {
            lastCheckTotalHours = totalHours;
            return;
        }

        foreach (IServerPlayer player in Sapi.World.AllOnlinePlayers)
        {
            if (player != null)
            {
                var invGear =
                player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

                if (invGear == null) continue;

                foreach (ItemSlot slot in invGear)
                {
                    if (slot == null || slot.Empty) continue;

                    var source = slot.Itemstack.Collectible.GetCollectibleInterface<IPowerSource>();
                    if (source == null) continue;

                    if (!(ArmorFeaturesProp.ReadFrom(slot.Itemstack)?.UseFuel ?? false))
                    {
                        continue;
                    }

                    //For Each Turned on Passive, Increase Consumption by 1x
                    if (slot.Itemstack.Attributes.GetBool("togglelight")){ Consumption = hoursPassed*2; }

                    source.ConsumePower(slot, player.Entity, Consumption);

                    slot.MarkDirty();
                }
            }
        }
        lastCheckTotalHours = totalHours;
    }

    //To Toggle Power
    public void TogglePowerWearableItem(IServerPlayer player, AdditionalArmorFeaturesLibraryPacket packet) => TogglePowerWearableItem(player);
    private bool TogglePowerWearableItem(IPlayer player, int itemslot = -1)
    {
        if (player == null) return false;

        var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

        ItemSlot? currentSlot = player.InventoryManager.ActiveHotbarSlot;

        var logger = player.Entity.Api.Logger;

        //Toggle for all worn items.
        foreach (ItemSlot slot in invGear)
        {
            if (slot.Empty) continue;

            var armorPiece = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorPower>(true);
            if (armorPiece == null) continue;

            var newState = !armorPiece.PowerState(slot.Itemstack);
            armorPiece.SetPowerActive(slot, newState, player.Entity);

            slot.MarkDirty();

            ClientToggleChannel?.SendPacket(
                new AdditionalArmorFeaturesLibraryPacket
                {
                    Toggle = ToggleType.Power
                }
            );
        }
        return true;
    }

    private bool OnTogglePowerHotkey(IPlayer player)
    {
        return TogglePowerWearableItem(player);
    }

    //To Toggle Light
    public void ToggleLightWearableItem(IServerPlayer player, AdditionalArmorFeaturesLibraryPacket packet) => ToggleLightWearableItem(player);
    private bool ToggleLightWearableItem(IPlayer player, int itemslot = -1)
    {
        if (player == null) return false;

        var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

        ItemSlot? currentSlot = player.InventoryManager.ActiveHotbarSlot;

        var logger = player.Entity.Api.Logger;

        // Toggle for all worn items.
        foreach (ItemSlot slot in invGear)
        {
            if (slot.Empty) continue;

            var armorPiece = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorLight>(true);
            if (armorPiece == null) continue;

            var newState = !armorPiece.LightState(slot.Itemstack);
            armorPiece.SetLightActive(slot, newState, player.Entity);

            slot.MarkDirty();

            ClientToggleChannel?.SendPacket(
                new AdditionalArmorFeaturesLibraryPacket
                {
                    Toggle = ToggleType.Light
                }
            );
        }

        return true;
    }

    private bool OnToggleLightHotkey(IPlayer player)
    {
        return ToggleLightWearableItem(player);
    }

}
