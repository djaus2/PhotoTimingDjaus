using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AthStitcher.Data
{
    public partial class Scheduling : ObservableObject
    {
        [ObservableProperty]
        private int meetCutoff=0;

        [ObservableProperty]
        private int eventCutoff = 0;

        [ObservableProperty]
        private bool canAddHeatsOnDayOfMeet = false;

        [ObservableProperty]
        private bool useTabbedPrinting = true;

        [ObservableProperty]
        private string appIcon  = "djcolor.jpg";


        [ObservableProperty]
        private string infoLink  = "https://davidjones.sportronics.com.au/tags/athstitcher/";

        [ObservableProperty]
        private string infoLinkText  = "Blogs about AthStitcher app";


        [ObservableProperty]
        private string gitHubLink  = "https://github.com/djaus2/PhotoTimingDjaus";

        [ObservableProperty]
        private string gitHubLinkText  = "App Repository ... See AthStitcher project";
    }
}
