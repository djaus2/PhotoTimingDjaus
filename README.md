# PhotoTimingDjaus

A simple phototiming app for Athletics etc where a finish line is filmed with say, a phone, and a stitched image is created by taking the middle vertical line of pixels from each video frame and stiching together the phototiming image.  Note that video FramesPerSecond are typically 30 so each line represents 0.033 of s second and hence this is the resolution of any timing. Commercial equipment would be to thousandths or tens of thousandths of a second.

## Apps
- PhotoTimingDjaus
  - A Console app that does the image stiching. Also called by VideoSticher apps to do such
- VideoStitcherWPFApp
  - Earlier version of WPF app. _Don't use._
- VideoStitcherWPFAppV2
  - **This is a WPF app so runs on a Windows desktop.**  _A Phone version may follow._
- VideoStitcherWPFAppV3 .. Use this
  - Can load previous stitched file but timing doesn't work for that (2Do)

## 2Dos
A few issues remain but getting there. 
- Times are not quite accurste.
- Panning and scrolling need improvement. <- _Improved in V3_
- When starting from a time not at start of video, time at bottom scale is not chaned (is from 0). But determined time is correct.

## Usage
- You need a video, as .MP4.
    - Film with a phone and transfer to desktop.
- Once stictched, use mouse to select a time, for which a red line appears vertically and time is shown.
- Once the mouse releases, the time shows in a box towards the top as a time span string.
- It is also copied to the clipboard.

Enjoy.

> Nb: This was authoured up to last 10% using GitHub Copilot.
