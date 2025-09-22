# PhotoTimingDjaus

**WPF app has been renamed AthStitcher** 

>> NB With Phone Video Capture: You need to enter the stitched image name and accept it, like a return. On my phone I accept by pressing the tick bottom right of popup keyboard

## About
A simple phototiming app for Athletics etc where a finish line is filmed with say, a phone, and a stitched image is created by taking the middle vertical line of pixels from each video frame and stiching together the phototiming image. Previously had a similar app that used AForge.  Note that video FramesPerSecond are typically 30 so each line represents 0.033 of second and hence this is the resolution of any timing. Commercial equipment would be to thousandths or tens of thousandths of a second. _Can now set to 60 Fps if phone supports that._

## Recent
### Status
> All good, except uploads only work from phone from  app in **SendVideo**  in
[TransferVideoOverTcp](https://github.com/djaus2/AthsVideoRecording) solution.  
SendVideo is being updated for the  recent updates here and in [TransferVideoOverTcp/SendVideo](https://github.com/djaus2/TransferVideoOverTcp/tree/master/SendVideo).


### Latest
- AthStitcher: V3.2.0
  - Can download ExifTool, set location, unzip and even change its name(not advised).
  - Fixed an issue wrt GUNSOUND mode where if no audio for part of track, audio processing is errant.
    - Come as "inf", which is tagged as NaN and is finally set as zero.
- AthStitcher: V3.1.2 Fixed issue at start of StitchVideo() where incorrect _embellished_ filename appendages were searched for, when VideoInfo json file is not being used.
  - Should be one of: ```_VIDEOSTART_, _GUNSOUND_, _GUNFLASH_, _MANUAL_, _WALLCLOCK_```
  - Not being processed later though (eg getting video sound)
> Embellished filanmes functionality will be removed once VideoInfo json file mechanism is complete for all modes.
- AthStitcher: V3.1.1 Works properly for Manual,FromVideoStart and WallClock modes, when using VideoInfo to pass meta-info rather than using embellished filename.
  - Flash and GunSound 2Do. 
  - Added some sample videos with their json files.  See <solution>\vid
  - Copy contents to c:\temp\vid
- AthStitcher: Improved Json Editor for VideoInfo, VideoIfo databound and conditionals.
- Athstitcher: Once a Video is downloaded along with its json file, the corresponding Json file can be directly edited: 
  - File-Edit Video File's Meta-Info.
- AthStitcher: Updated to .NET 9.0
- AthStitcher: Download _(version2)_ video and meta info over TCP from phone app.
  - Meta-info as json and uses that for filename, checksum etc.
  - Filename is now un-embellished with meta-info.
  - Embellished filename should still work though
  - VideoEnums _(VideoEnums.Windows here)_ is now a Nuget Package. 
    - There is a Windows version and an Android/Maui version.
- AthStitcher: Improving UI for Video Frame Popups - Max popup Frame vertival  size, with vertical scrollbars when video frame image is bigger vertically.
- AthStitcher: Download now menu item. Menu File-Done to return.
  - Meta info _(as embellished filename)_ has video start time and gun time (if set)
  - If no Gun time then Video start time is used as Gun time.
  - _(Embellished)_ Video filename is _(such as)_: ```{originalfilename}_GUN_{guntime:yyyy-MM-dd HH--mm--ss.fff}_.mp4```
  - Stitched image filename is set to: ```{originalfilename}_GUN_{guntime:yyyy-MM-dd HH--mm--ss.fff}_.png```
  - If no Gun time then _VIDEOSTART_ is used instead of _GUN_ in filenames.
  - WallClock time can be used as well if that is set in meta info.
- AthStitcher: Improved Nudged video frame popup, Place Left, Middle or Right on app, or use previous (red line) video frame.
- AthStitcher: Fixed where unable to click to right of previous click on image and be processed.
- AthStitcher: App: Now has download video over local TCP.
- Sample app [djaus2/MauiMediaRecorderVideoAndroidApp](https://github.com/djaus2/MauiMediaRecorderVideoAndroidApp) has been updated to append TimeFromMode to Video filename.
- WPF File-Open is Open is now "Open Video File and Stitch".
  - Looks at filename and determines type of video, stitches and determines start time.
    - If no match then opens according to menu selection.
    - Filename patterns:
```cs
  videoStart =  @"_VIDEOSTART_\.mp4$";  // Default
  wallClockPattern = @"_WALLCLOCK_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$"; <-- A DateTime string (sort of)
  gunPattern = @"_GUNSOUND_\.mp4$";
  flashPattern = @"_GUNFLASH_\.mp4$";
  manualPattern = @"_MANUAL_\.mp4$";
```
- WPF app now recognizes those patterns and embedds the types as video title and for WallClock, the GunWallClcok time as teh Comment.
- If use [Stitch] button this meta info is ignored and the selected TimeFromMode is used.
  - Programmically the TimeFromMode names have changed:
```cs
    public enum TimeFromMode
    {
        FromVideoStart, //From start of video capture
        FromGunSound, //From gun sound
        FromGunFlash,  //From observed flash of gun on video
        ManuallySelect, //Manually selected start time
        WallClockSelect,
    }
```
  - ~~2Do Match this with the phone video capture app.~~ _Not possible with the package used to embedded metainfo in the video file on Android._

- WPF App HAS been renamed as **AthStitcher**  <-- And the project folder has now been renamed to that.
- Added simple App **SplashScreen** (Image in root and SplashScreen property)
  - And App icon. Used Gimpy to create as 256x256 in root of project and set as Content _(no Copy property)_. Then add as App Icon in project properties.
- Popup image of frame is centered for mouse click on image (red) line, if start has been determined.
  - Nudge line is green and image frame for it can be left, center or right wrt to Stitched Image when start time has been determined/set.
  - For **Manual mode**, the Gun line (selected color) is nudged until accepted.
  - Double click on image frame hides it, single click enlarges frame x1.5, shift single click reduces frame by 1.5. If too small (about 50) is hidden
- Zoom controls now work. Pan sliders don't though. Simplest: ***Just set the Auto Width and Height.***
- Default TimeFrom mode is Manual. If Video Filename has DateTime string on end then that is interpreted as the Gun (race start) DateTime and set to WallClock mode.
  - eg ```qwerty1_GUN_2025-06-19 11--34--08.591_.mp4```     Pattern searched for with Regex is  
```string pattern = @"_GUN_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$"```
  - Matching change now in Video Capture Phone app: [djaus2/MauiMediaRecorderVideoAndroidApp](https://github.com/djaus2/MauiMediaRecorderVideoAndroidApp)
    - If Gun icon is tapped before or after video start then that WallClock time is used as race start and is appended as a DateTime string to video filename as above. WallClock mode here uses that as default instead of VideoStart time and calculates times wrt to that.
  - Video Capture NuGet Package has been updated (V2.2.2): [djaus2/djaus2_MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib/) adds this Gun functionality.
  - _Could also check if audio in video and set that mode or even check video frames for flash mnde._
- **Added WallClock Start Time**: Just enter the start time of the event (Calendar Day (Select), Time of Day to ms). Initaially set to Video Start. Start on StitchedImage is then calculated wrt Video start DateTime. _(Currently assumes same day)_
-  **Added ability to popup corresponding video frame for selected time centred on StitchedImage timing line with aligned line thru frame.**

Major rework so that all/most info in XAML page is bound to ViewModel properties. Get Set for many in separate page which handles the DtataContext.  
Still a workk in progress

The WPF app has been updated to calculate time from gun.  The guntime is taken from audio (microsphone).  So the video is recording before the gun. Continued development of teh WPF app.  
> Latest: Big changes to the WPF UI and functionality.

## Manual Mode:
- Can select **Manual Mode**
  - Stitch image then
  - Select "Start Time" from stitched image with mouse using right click and drag
    - Can nudge as well. (It's green line)
  - Then [Accept Gun Line] button to set this as GunTime and write its line
    - That button is only visible when in Manual mode, and image is stitched in that mode
    - But gun line not currently drawn but timings are relevant to where it shoul show. (2Do)

## 2Do
- Rewrite this README.
- When loading stitched image get start time from meta info.
  - Stitching embeds Video start WallClock time in Title and Gun time (WallClock) in Comment in stitched image.
- Remove dead code _(commented out code)_
- Take all image width ratios from horizonatal zoom slider.

## Features to add
- Truncate video option at start(gun) time.
  - Code is writen.


## Comment

Have posted a blog post wrt GitHub Pilot strengths and weakness in creating this code. [GitHub Copilot v Documentation: How far can you go with Copilot](https://davidjones.sportronics.com.au/coding/GitHub_Copilot_v_Documentation-How_far_can_you_go_with_Copiot-coding.html)

## Info:
- Input a .MP4 video
- Output a .png file

## Library
- **PhotoTimingDjausLib**
  - Uses NuGet Packages:
    - OpenCvSharp4
    - OpenCvSharp4.runtime.win
    - FFMpegCore
    - NAudio
  - Does video stitching
- **PhotoTimingDjausLibAndroid** Works now
  - Uses **Emgu.CV.runtime.maui.mini.android** instead of OpenCvSharp4
  - _Not yet as functional as *PhotoTimingDjausLib 2Do._


## Apps
- **PhotoTimingDjaus**
  - A Console app that does the image stiching.
    - Originally called by VideoSticher apps to do such but that functionality is separate library.
    - Currently unloaded as code hasn't been updated for code changes in library.
- **AthStitcher** .. Use this
  - **This is a WPF app so runs on a Windows desktop.**
  - Uses PhotoTimingDjausLib _as above._
  - Can measure time for events using mouse click and drag on image.
  - Set video file and press [Stitch Video], or load and stitch from File menu.
    - Generates stitched image file
    - Option to choose timing mode from:
      1. Video Start
      2. Manual Mode (Set start time wonce stitched)
      3. WallClock Mode Set Wallclock time of race start and then work out times wrt this and video start.
      4. Detect Start Gun audio
      5.  Detect video flash _(Not yet imlemented)._
    - _(For 2.)_ Also extracts audio max volume (per audio fame) in dB v time text file from video, generates gun time
       - **Assumes video  recording is started before gun.**
      - Nb: Audio frames are not video frames.
        - Max vol for each frame = max vol for each frame in dB - (the min value in dB for all audio frames)
      - Volume for each audio frame = 10^ (Max vol for frame in  /10);
        - Graph of Volume added below ticks for WPF app.
      - Guntime is first time at which Volume >= (Max Volume of all frames) /1000  <-- This 1000 value is in-app settable.
  - Can load previous stitched file but timing doesn't work for that (2Do)
  - Uses Image viewer with zoom and pan from this GitHub project [djaus2/ShowImageWPF](https://github.com/djaus2/ShowImageWPF)
    - Zoom etc currently not working
- **StitchInTimeMaui**  Maui version of PhotoTimingDjaus Console app
  - Uses PhotoTimingDjausLibAndroid _as above._
  - Tested on Google Pixel 6 phone
  - Now performs stitch of limited video:
    - Limited to 1000 frames = 33 seconds
  - Now displays stitched image
    - On phone scroll up from bottom
    - 2D: Display on separate page
  - Uses **Emgu.CV.runtime.maui.mini.android** via PhotoTimingDjausLibAndroid  library
  - Now runs in Android Device
  - Ticks added at bottom by overwriting image
    - 1, 5, 10 sec and minute different colors and sizes
    - No labels yet
  - Cancel button added
    - Some times there is a buffer issue.

## 2Dos
A few issues remain but getting there. 
- Times are not quite accurste.
- Panning and scrolling need improvement. <- _Improved in V3_
- When starting from a time not at start of video, time at bottom scale is not chaned (is from 0). But determined time is correct.

## Usage
- You need a video, as .MP4.
    - Film with a phone and transfer to desktop.
- In WPF app, Once stictched, use mouse to select a time, for which a red line appears vertically and time is shown.
  - Once the mouse releases, the time shows in a box towards the top as a time span string.
  - It is also copied to the clipboard.
 
## Footnote
Found my phone has a few options to try
- Image stabilisation (Locked)
- 60 FPS which would halve time resolution to 17 mSec  ... about 2 100ths of a sec

Enjoy.

> Nb: This was authoured up to last 10% using GitHub Copilot.
> Also the porting of PhotoTimingDjausLibAndroid from OpenCvSharp4 to Emgu.CV was done with help of GitHub Copilot!
