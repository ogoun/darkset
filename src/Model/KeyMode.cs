using System;

namespace Darknet.Dataset.Merger.Model
{
    [Flags]
    public enum KeyMode
    {
        None = 0x0,
        Alt = 0x1,
        Shift = 0x2,
        AltShift = Alt | Shift, // 3
        Ctrl = 0x4,
        CtrlAlt = Alt | Ctrl, // 5
        CtrlShift = Alt | Shift, //
        CtrlAltShift = Alt | Shift | Ctrl
    }
}
