using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoTimingDjaus.Enums
{
    public enum TimeFromMode
    {
        FromVideoStart, //From start of video capture
        FromGunSound, //From gun sound
        FromGunFlash,  //From observed flash of gun on video
        ManuallySelect, //Manually selected start time
        WallClockSelect,
    }

    public enum VideoDetectMode
    {
        FromFlash, //Detect flash in video
        FromFrameChange, //Detect motion in video
        FromMotionDetector //Detect frame change in video.
    }

}
