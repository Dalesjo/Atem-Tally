using LibAtem.Common;

namespace LibAtem.Commands.DeviceProfile
{
    [CommandName("_pin"), NoCommandId]
    public class ProductIdentifierCommand : ICommand
    {
        public string Name { get; set; }
        public ModelId Model { get; set; }

        public void Serialize(ByteArrayBuilder cmd)
        {
            cmd.AddString(32, Name);
            // TODO figure out what this is. It might mean something, or nothing. By blanking out after the name seems to cause the client to lose input names on the buttons
            cmd.AddByte(0x28, 0x36, 0x9B, 0x60, 0x4C, 0x08, 0x11, 0x60);
            cmd.AddUInt8((uint) Model);
            cmd.AddByte(0x3D, 0xA4, 0x60);
        }

        public void Deserialize(ParsedByteArray cmd)
        {
            Name = cmd.GetString(32);
            cmd.Skip(8);
            Model = (ModelId) cmd.GetUInt8();
            cmd.Skip(3);
        }
    }
}