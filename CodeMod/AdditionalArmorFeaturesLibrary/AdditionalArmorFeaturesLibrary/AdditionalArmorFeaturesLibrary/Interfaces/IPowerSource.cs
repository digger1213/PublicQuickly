using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace AdditionalArmorFeaturesLibrary.Interfaces
{
    public interface IPowerSource
    {
        bool HasPower(ItemStack sourceStack);

        bool IsFull(ItemStack sourceStack);

        double GetPower(ItemStack sourceStack);

        float GetStackFuel(ItemStack sourceStack, ItemStack sinkStack);

        void SetPower(ItemStack sourceStack, double amount);

        void AddPower(ItemStack sourceStack, ItemStack sinkStack, double amount);

        void ConsumePower(ItemSlot sourceslot, EntityPlayer entityPlayer, double amount);
    }
}
