using System;
using System.Collections.Generic;
using System.Text;

namespace ALARm.Core.Constants
{
    /// <summary>
    /// Единый шаг продвижения по видео.
    /// Сейчас 1 "шаг" = 200 (как в текущей логике времени/смещения).
    /// На Этапе 2 просто поменяем Milliseconds = 100.
    /// </summary>
    public static class FrameStep
    {
        public const int Milliseconds = 200;
        public const int FramesPerMeter = 5;              // ← было «/5» в коде
        public static int RowMs => Milliseconds / 4;
        public static double MetersPerFrame => 1.0 / FramesPerMeter;
    }

}
