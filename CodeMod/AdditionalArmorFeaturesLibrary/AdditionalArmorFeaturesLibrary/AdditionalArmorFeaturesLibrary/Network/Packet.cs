using ProtoBuf;

namespace AdditionalArmorFeaturesLibrary.Network
{
    public enum ToggleType
    {
        Power,
        Light,
        Exstate,
        JumppackActivation,
        Jumppack,
        Jetpack,
        Nightvision
    }


    [ProtoContract]
    public sealed class AdditionalArmorFeaturesLibraryPacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }

        [ProtoMember(2)]
        public ToggleType Toggle { get; set; }

    }

    [ProtoContract]
    public sealed class FuelSyncPacket
    {
        [ProtoMember(1)]
        public double FuelHours;

        [ProtoMember(2)]
        public string InvClass;

        [ProtoMember(3)]
        public int IdSlot;
    }
}