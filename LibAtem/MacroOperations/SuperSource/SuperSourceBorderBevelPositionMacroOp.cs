using LibAtem.Commands;
using LibAtem.Commands.SuperSource;
using LibAtem.Common;
using LibAtem.Serialization;

namespace LibAtem.MacroOperations.SuperSource
{
    [MacroOperation(MacroOperationType.SuperSourceBorderBevelPosition, 8)]
    public class SuperSourceBorderBevelPositionMacroOp : MacroOpBase
    {
        [Serialize(4), UInt8Range(0, 100)]
        [MacroField("BevelPosition")]
        public uint BevelPosition { get; set; }

        public override ICommand ToCommand()
        {
            return new SuperSourcePropertiesSetCommand()
            {
                Mask = SuperSourcePropertiesSetCommand.MaskFlags.BorderBevelPosition,
                BorderBevelPosition = BevelPosition,
            };
        }
    }
}