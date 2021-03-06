using System.Collections.Generic;
using LibAtem.Common;
using LibAtem.MacroOperations;
using LibAtem.MacroOperations.Settings;
using LibAtem.Serialization;

namespace LibAtem.Commands.Settings
{
    [CommandName("CDcO", 4), NoCommandId]
    public class DownConvertModeSetCommand : SerializableCommandBase
    {
        [Serialize(0), Enum8]
        public DownConvertMode DownConvertMode { get; set; }

        public override IEnumerable<MacroOpBase> ToMacroOps()
        {
            yield return new DownConvertModeMacroOp {DownConvertMode = DownConvertMode};
        }
    }
}