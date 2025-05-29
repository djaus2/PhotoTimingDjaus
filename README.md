# PhotoTimingDjaus

>> NB With Phone Video Capture: You need to enter the stitched image name and accept it, like a return. Om my phone I accept by pressing the tick bottom right of popup keyboard

## Recent

The WPF app has been updated to calculate time from gun.  The guntime is taken from audio (microsphone).  So the video is recording before the gun.

A simple phototiming app for Athletics etc where a finish line is filmed with say, a phone, and a stitched image is created by taking the middle vertical line of pixels from each video frame and stiching together the phototiming image. Previously has a similar app that used AForge.  Note that video FramesPerSecond are typically 30 so each line represents 0.033 of s second and hence this is the resolution of any timing. Commercial equipment would be to thousandths or tens of thousandths of a second.

> Currently looking at adding the video capture to the app like what was done in the previous app. Can only get the launching of the inbuilt phone video app to take video; not to orchestarte it, thus far. 
One attempt at this is [djaus2/MauiMediaRecorderVideoSample-Android](https://github.com/djaus2/MauiMediaRecorderVideoSample-Android) which uses the MediaRecorder control.

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
  - Uses **Emgu.CV.runtime.maui.mini.android** instaed of OpenCvSharp4
  - 2Do: Add timining marks

## Apps
- **PhotoTimingDjaus**
  - A Console app that does the image stiching.
    - Originally called by VideoSticher apps to do such but that functionality is separate library.
- **VideoStitcherWPFAppV3** .. Use this
  - **This is a WPF app so runs on a Windows desktop.**
  - Uses PhotoTimingDjausLib _as above._
  - Set vudeo file and press [Stitch Video]
    - Geerates stitched image file
    - Also extracts audio max volume (per audio fame) in dB v time text filefrom video, generates gun time
      - Nb: Audio frames are not video frames.
        - Max vol for each frame = max vol for each frame in dB - (the min value in dB for all audio frames)
      - Volume for each audio frame = 10^ (Max vol for frame in  /10);
        - Graph of Volume added below ticks for WPF app.
      - Guntime is first time at which Volume > (Max Volume of all frames) /1000 
  - Can load previous stitched file but timing doesn't work for that (2Do)
  - Uses Image viwer with zoom and pan from this GitHub project [djaus2/ShowImageWPF](https://github.com/djaus2/ShowImageWPF)
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
