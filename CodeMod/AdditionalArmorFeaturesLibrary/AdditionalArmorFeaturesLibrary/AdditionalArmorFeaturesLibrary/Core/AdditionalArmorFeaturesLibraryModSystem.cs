using AdditionalArmorFeaturesLibrary;
using AdditionalArmorFeaturesLibrary.Collectible.Behavior;
using AdditionalArmorFeaturesLibrary.Interfaces;
using AdditionalArmorFeaturesLibrary.Util;
using AdditionalArmorFeaturesLibrary.Utils;
using Cairo;
using HarmonyLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods.WorldEdit;

namespace AdditionalArmorFeaturesLibrary;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class TogglePacket
{
    public string HotKeyCode { get; set; } = "";
}

internal class LogStuff : ModSystem
{
    public override void Start(ICoreAPI api)
    {

        api.RegisterCollectibleBehaviorClass("AdditionalArmorFeaturesLibrary:ArmorFeatures", typeof(CollectibleBehaviorArmorFeatures));
        api.RegisterCollectibleBehaviorClass("AdditionalArmorFeaturesLibrary:Fuel", typeof(CollectibleBehaviorFuel));

    }

}

public partial class AdditionalArmorFeaturesLibrrarySystem : ModSystem
{
    public long OnLongRefreshTick { get; set; }
    public long OnLongServerFuelTick { get; set; }
    public long OnLongServerTick { get; set; }

    public event Action? OnDispose;
    ICoreClientAPI capi;
    ICoreServerAPI sapi;
    EntityBehaviorPlayerInventory bh;


    public bool Disposed { get; private set; } = false;
    public static event Action<ICoreAPI>? OnSettingsChange;

    public override void Start(ICoreAPI api)
    {
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;


        if (!api.IsSinglePlayer)
        {
            api.Event.EnqueueMainThreadTask(() => OnSettingsChange?.Invoke(api), "game");
        }

        api.Input.RegisterHotKey("toggleLight", Lang.Get("AdditionalArmorFeaturesLibrary:keybind-activeslot-description"), GlKeys.L);
        api.Input.SetHotKeyHandler("toggleLight", _ => OnToggleLightHotkey(api.World.Player));

        api.Input.RegisterHotKey("toggleHoveredGearLight", Lang.Get("AdditionalArmorFeaturesLibrary:keybind-gearslot-description"), GlKeys.L, HotkeyType.GUIOrOtherControls, false, true, false);
        api.Input.SetHotKeyHandler("toggleHoveredGearLight", _ => OnToggleHoveredGearLightHotkey(api.World.Player));

    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        sapi = api;

        OnLongServerFuelTick = api.Event.RegisterGameTickListener(OnServerFuelTick, 2000);
        OnLongServerTick = api.Event.RegisterGameTickListener(OnServerTick, 100);
    }

    private void OnServerTick(float obj)
    {
        if (sapi == null) return;

        foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
        {
            if (player?.Entity == null) continue;

            var invGear =
                player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

            if (invGear == null) continue;

            foreach (ItemSlot slot in invGear)
            {
                if (slot == null || slot.Empty) continue;

                var item = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorArmorFeatures>(true);

                if (item == null) continue;

                if (!ArmorFeaturesProp.ReadFrom(slot.Itemstack).UseFuel)
                {
                    continue;
                }

                var dresstype = slot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorWearable>(true)?.GetDressType(slot);

            }
        }
    }

    double lastCheckTotalHours;
    private void OnServerFuelTick(float dt)
    {
        if (sapi == null) return;

        double totalHours = sapi.World.Calendar.TotalHours;
        double hoursPassed = totalHours - lastCheckTotalHours;

        hoursPassed = Math.Min(hoursPassed, 0.5);

        if (hoursPassed <= 0)
        {
            lastCheckTotalHours = totalHours;
            return;
        }

        foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
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

                    if (!ArmorFeaturesProp.ReadFrom(slot.Itemstack).UseFuel)
                    {
                        continue;
                    }

                    source.ConsumePower(
                        slot,
                        player.Entity,
                        hoursPassed
                    );

                    slot.MarkDirty();
                }
            }
        }

        lastCheckTotalHours = totalHours;
    }

    private bool ToggleWearableItem(IPlayer player, int itemslot = -1)
    {
        player = capi.World.Player;
        if (player == null) return false;

        var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

        ItemSlot? currentSlot = player.InventoryManager.ActiveHotbarSlot;

        var logger = player.Entity.Api.Logger;

        // Gear slot override
        if (itemslot != -1 && invGear != null)
        {
            currentSlot = invGear[itemslot];
        }

        if (currentSlot == null || currentSlot.Empty) return false;

        var attachmentableLight = currentSlot.Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorArmorFeatures>(true);

        if (attachmentableLight == null) return false;

        var newState = !attachmentableLight.LightState(currentSlot.Itemstack);

        attachmentableLight.SetLightActive(currentSlot, newState, player.Entity);

        currentSlot.MarkDirty();
       
        return true;
    }

    private bool OnToggleLightHotkey(IPlayer player)
    {
        return ToggleWearableItem(player);
    }

    private bool OnToggleHoveredGearLightHotkey(IPlayer player)
    {
        player = capi.World.Player;
        var invGear = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

        ItemSlot? hoveredSlot = player.InventoryManager.CurrentHoveredSlot;
        int itemslot = invGear?.GetSlotId(hoveredSlot) ?? -1;

        if (itemslot == -1)
        {
            return false;
        }

        LoggerExt.SendLogger(capi, [$"The current slot that {player.PlayerName} is hovered over: {itemslot}"]);

        return ToggleWearableItem(player, itemslot);
    }

}
