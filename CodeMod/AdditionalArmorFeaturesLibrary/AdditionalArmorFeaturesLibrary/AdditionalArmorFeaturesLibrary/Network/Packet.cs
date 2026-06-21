using ProtoBuf;

namespace AdditionalArmorFeaturesLibrary.Network
{
    public enum ToggleType
    {
        Power,
        Light
    }


    [ProtoContract]
    public sealed class AdditionalArmorFeaturesLibraryPacket
    {
        [ProtoMember(1)]
        public int ItemSlot { get; set; }

        [ProtoMember(2)]
        public ToggleType Toggle { get; set; }

    }
}