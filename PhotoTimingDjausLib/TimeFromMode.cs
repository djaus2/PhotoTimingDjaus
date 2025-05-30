using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoTimingDjaus
{
        public enum TimeFromMode
        {
            FromButtonPress, //From start of video capture
            FromGunviaAudio, //From gun sound
            FromGunViaVideo  //From observed flash of gun on video
        }

}
