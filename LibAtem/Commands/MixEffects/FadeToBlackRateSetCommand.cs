using System.Collections.Generic;
using LibAtem.Common;
using LibAtem.MacroOperations;
using LibAtem.MacroOperations.MixEffects;
using LibAtem.Serialization;

namespace LibAtem.Commands.MixEffects
{
    [CommandName("FtbC", 4)]
    public class FadeToBlackRateSetCommand : SerializableCommandBase
    {
        [CommandId]
        [Serialize(1), Enum8]
        public MixEffectBlockId Index { get; set; }
        [Serialize(2), UInt8Range(0, 250)]
        public uint Rate { get; set; }

        public override void Serialize(ByteArrayBuilder cmd)
        {
            base.Serialize(cmd);
            cmd.Set(0, 0x01); // Mask Flag
        }

        public override IEnumerable<MacroOpBase> ToMacroOps()
        {
            yield return new FadeToBlackRateMacroOp
            {
                Index = Index,
                Rate = Rate,
            };
        }
    }
}