# Record-Rssd-By-Monocle
Monocle Capture App For full-body scan 
=======================================
Version: 
Date: 19-Nov-2015
Author: Original developer of Monocle is unknown to us. Sheindy Hirshman (sheindyx.hirshman@intel.com) did the updates that allow Record and Playback of rssdk files. 



Overview
========
A simple data capture app using the RealSense DS4, based on [Smithers][https://github.com/bodylabs/smithers]. 

The original code was modofied / augmented by Sheindy Hirshman (PerC validation) to support also Record (saving color and depth stream in an rssdk files) and playback. 

Very little teststing was done on the modified tool, so it crashes sometimes. But it's usable. 

We do not intend to use this utility for Playback since we already have a tool (ScanBack) that does playback using the new ds4-api that was defined with Bodylabs. 

To get ScanBack talk to Michael Stahl (Israel) or to Joseph Olivas (USA).  

Installation
------------

Note: Friendly suggestion: just follow the below EXACTLY. Don't try shortcuts. Boot when we tell you to. 

0. Uninstall every previous version of RealSense DCM and SDK. Turn off sleep when idle for your tablet/desktop. Boot. 
1. Plug in the DS4 R200 sensor, which is needed for the DCM and SDK installation.
2. Install the following SDK and DCM (find them in the "Pre-requisites installers" folder):

SDK: version 5.0.3.187777
DCM for R200: 2.0.3.2548

Experinece shows that it's OK to have also the DCM for F200 installed on the same platform - it does not interfere with the R200 DCM. 

You can also get the rssdk installer from: https://wiki.ith.intel.com/pages/viewpage.action?pageId=479691390 

Theoretically you can also get it somehow from here: https://software.intel.com/en-us/intel-realsense-sdk/download but I did not stufy too much how to get old rssdk installer via the public Intel website.



3. Install the Kinect V2 SDK (find it in the "Pre-requisites installers" folder, or if you feel like it get it from: https://www.microsoft.com/en-us/download/details.aspx?id=44561) 

Boot. Open Programs and Features and verify the correct versions are installed. 

4. Unzip the Monocle app 7z file to a folder of your choice. The executable for the app is "Monocle.exe" in the "Monocle\bin\x64\Release" folder. 

5. Start the app - it should show a warning that the upload credentials have not been set. Note that sometimes this warning pop-up hides behind some other windows, so it seems like nothing happened. Look for this popup.

Edit the credential file (C:\Users\[USERNAME]\Documents\Body Labs\Monocle\credentials) to fill in the API accessKey and secret from Body Labs. 
  
The possible values are listed at the end of this file.



Using Monocle
--------------
1. Connect an R200 camera. Monocle will crash if there is no camera. 

2. Fill in the Name field - this will be the name of your scan. 

3. Make sure to select the correct gender. This has a significant impact on the accuracy of the final results from Bodylabs. 

4. Height and Weight are optional, and currently not used by Bodylabs. We do recommend to fill in the correct values, since eventually maybe they will use this data, so why not. 

5. Click New Session. At this time you should see the camera streaming to the screen (Color or Depth - depending on your selection at the top-left buttons). We recommend you use the Depth stream, since this will ensure the subject is centered in the depth stream. Centering the subject in the Color stream means it's a bit to the side in the depth stream. 

6. Start a sweep by clicking "Start Recording". Note that it takes the app a few seconds to start the recording, so it seems frozen. Just wait for the frame-count to start counting up. Once this starts, scan the subject. It should take 5-7 seconds to sweep and the frame counter should show about 30-40 frames. Click STOP to finish the sweep. 

Note that Monocle has a frame limit of 80. If you hit this value you need to restart the scan (all sweeps) since it's gonna crash anyway. 

If you are not happy with a sweep, you can redo it by selecting "Redo last sweep". This worked well in the original Monocle. We did not test it with the one you have here (yet). 

Remember that the order of sweeps is: Front, Left (side of the subject!), Back, Right.  This means the subject needs to turn 90 degrees clockwise for each new sweep. 

7. After scanning the body 4 times from 4 directions, 
   the app store the scans at C:\Users\[USERNAME]\Documents\Body Labs\Monocle, 
   and store also the rssdk files with some metadata (calibration file) at C:\Users\[USERNAME]\Documents\Body Labs\Monocle\
   the folder name is "input session name-rssdk"
   "input session name" is the value you entered in the "Name" field.




DEBUG
-----------
There is a log.txt in the same folder where we store the scans (e.g. C:\Users\[USERNAME]\Documents\Body Labs\Monocle).

Know Issue & Workaround
------------------------

1. The sensor will take a while to be live when start streaming. It only starts after you click "New Session".  
2. Since we are using the Scene Perception API which seems to block the DCM services at times, please restart the DCM services under Windows's Local services management panel if the app failed to start or the camwera is not streaming. In particular, if the app crashes and the log file shows 
   Unable to enable scene perception:PXCM_STATUS_ITEM_UNAVAILABLE, it could be fixed by restarting the INTEL R200 DCM service.
3. Also orient the camera to some valid scene helps the Scene Perception API to initialize. 
4. If DCM installation failed, please follow the DCM release notes to resolve possible conflicts.




Credentials
--------------
Use one of these credentials:

Account Created: intel_test_1@intel.com
Access key: AK72e97e614aeea033e831b34a709834e5 Secret: 813abd601cd070e6723c8dc80409add7

Account Created: intel_test_2@intel.com
Access key: AK0d44ffcd6ba679302fd1a532ac993067 Secret: 9172c91ec72deee337ae0a1ab17796d0

Account Created: intel_test_3@intel.com
Access key: AK02c9c20e4af12c7c8cca933213d5c25f Secret: 006ced81009ba42644aea0d3298ebea6
