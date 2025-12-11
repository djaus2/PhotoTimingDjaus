# PhotoTimingDjaus  ... AthStitcher App
[![License: CC0-1.0](https://img.shields.io/badge/License-CC0_1.0-lightgrey.svg)](http://creativecommons.org/publicdomain/zero/1.0/) 
[Issues](https://github.com/djaus2/PhotoTimingDjaus/issues)
[![GitHub stars](https://img.shields.io/github/stars/djaus2/PhotoTimingDjaus.svg?style=social&label=Star)

## About
AthStitcher is a simple ***Photo Timing/Photo Finish*** app for Athletics etc where a finish line is filmed with say, a phone, and a stitched image is created by taking the middle 
vertical line of pixels from each video frame and stiching together the phototiming image. Previously had a similar app that used AForge.  Note that video FramesPerSecond are typically 30 so each line represents 0.033 of second and hence this is the resolution of any timing. Commercial equipment would be to thousandths or tens of thousandths of a second. _Can now set to 60 Fps if phone supports that._    
There is a Maui app, [djaus2/AthsVideoRecording](https://github.com/djaus2/AthsVideoRecording), that handles the recording of the finish line video along with relevant meta-data. 
It handles sending of that video over TCP, which AthStitcher is able to receive.  
A DBMS has been added using Entity Framework Core to record Meets, Events, Heats and Lane Results, and export results as a PDF.


- A Competition Series has Rounds of Meets. 
- Each Meet has Events. 
- Each Event has Heats. 
- Each Heat has Lane Results.

> **Latest Blog Post:** [AthStitcher -Functionality](https://davidjones.sportronics.com.au/appdev/AthStitcher-Functionality-appdev.html)

## Note
As things stand, athletes are manually entered into heat results before or after a heat has been run. 

---

## Feedback

> **What features would you like to see implemented?**

`Ps:` Stopping this development for now. Awaiting any feedback before going further.  
- Email davidjones AT sportronics DOT com DOT au  
- Or leave a comment on the repository [GitHub Discussions](https://github.com/djaus2/PhotoTimingDjaus/discussions).
- Or leave a comment on my blog site : [SporTronics AppDev Blog](https://davidjones.sportronics.com.au/cats/appdev/) _(Requires DISQUS login)_

> Eg Athletes Table, Location Table Draw up Heats etc ?? Leave a note thx.

---

## QuestPDF
- Added QuestPDF NuGet package to AthStitcher WPF app to enable export of Heat, Event results as Pdf file.
- Requires licensing acceptance on first use.
  - See [QuestPDF Licensing](https://www.questpdf.com/license/community.html)
  - If accepting license for Community Non Profit use then uncomment line #46 in Data/ToPdf.cs

---

## AthStitcher Updates History

`In Brief:` This software package has been created in two parts. 
Initially a simple Photo Timing app using stitched images from a video was created. 
Of late a Database Management System was added for recording the timing results in the context of Meets, Events, Heats, and Lane results.
An ability to export results as Text PDF files has been added to the DBMS system 
as well as an ability to import events for a Meet from a Csv or Tabbed text file.
The software has undergone progress updates to enhance functionality and user experience.

>  **Version Update Details:** [See](AthStitcherUpdatesHistory.md)

## 2Do
- Reverse the horizontal direction of the stitched image such that athlete on right is fastest which matches the direction that athletes compete.

## Features to add
- Send Meets and Events to AthsVideoRecording app over TCP each with a Guid.
  - So that when video is sent over TCP, the Meet and Event can be selected.
  - Sending Events and Heats at this end is done.
    - _AthsVideoRecoding app is being updated to receive these.__
  - 2Do: _Need to align received video with correct Event and Heat.
- Athletes Table, Location Table Draw up Heats 
- Truncate video option at start(gun) time.
  - Code is written.


## Comment

Have posted a blog post wrt GitHub Pilot strengths and weakness in creating this code. [GitHub Copilot v Documentation: How far can you go with Copilot](https://davidjones.sportronics.com.au/coding/GitHub_Copilot_v_Documentation-How_far_can_you_go_with_Copiot-coding.html)

## Informatioon:
- Input a .MP4 video
  - Competitors _(Manual entry in lane results).__
- Outputs:
  - A .png file (stitched image)
  - Result in SqLite database file
  - Results a text or Pdf file

## App
- **AthStitcher**
  - **This is a WPF app so runs on a Windows desktop.**
  - Uses PhotoTimingDjausLib.
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
  - DBMS functionality added
    - Using Entity Framework Core with SqLite database
    - Record Meets, Events, Heats, Lane Results
    - Export results as Text or Pdf file using QuestPDF NuGet package
    - Import Events from Csv or Tabbed text file

## AthStitcher Libraries
- **PhotoTimingDjausLib**
  - Does video stitching
  - Uses NuGet Packages:
    - OpenCvSharp4
    - OpenCvSharp4.runtime.win
    - FFMpegCore
    - NAudio
- **DetectVideoFlash**
  -  Uses NuGet Packages:
    - OpenCvSharp4
    - OpenCvSharp4.runtime.win
    - And Sportronics.VideoEnums
- **GetVideoWPFLib**
  - Receives video over TCP from AthsVideoRecording Maui app
  - Uses DownloadVideoOverTcpLib
  - And Sportronics.VideoEnums
- **Sportronics.VideoEnums** _From NuGet_
  - Various enums used throughout
  - VideoInfo class
    - Meta-Info created on phone for video
      - Passed to AthStitcher over TCP before video and used there.

## 2Dos
A few issues remain but getting there. 
- Times are not quite accurste.
- Panning and scrolling need improvement. <- _Improved in V3_
- When starting from a time not at start of video, time at bottom scale is not changed (is from 0). But determined time is correct.

## Usage
- You need a video, as .MP4.
    - Film with a phone and transfer to desktop.
- In WPF app, Once stitched, use mouse to select a time, for which a red line appears vertically and time is shown.
  - Once the mouse releases, the time shows in a box towards the top as a time span string.
  - It is also copied to the clipboard.
 
## Footnote
Found my phone has a few options to try
- Image stabilisation (Locked)
- 60 FPS which would halve time resolution to 17 mSec  ... about 2 100ths of a sec

Enjoy.

> Nb: This was authored up to last 10% using GitHub Copilot.
> Also the porting of PhotoTimingDjausLibAndroid from OpenCvSharp4 to Emgu.CV was done with help of GitHub Copilot!
