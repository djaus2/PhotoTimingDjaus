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
    }
}
