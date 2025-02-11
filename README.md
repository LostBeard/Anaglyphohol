# Anaglyphohol

Anaglyphohol is a web browser extension that let's you view images on the web in anaglyph 3D. It supports green magenta, and red cyan glasses. View image search results in 3D on google.com, bing.com, and yahoo.com. Use Anaglyphohol on on almost any website. 

Anaglyphohol is developed using Blazor WebAssembly. The 2D to 3D magic is thanks to the amazing monocular depth estimation machine learning model [Depth Anything](https://huggingface.co/depth-anything/Depth-Anything-V2-Small).

## Installing from Chrome Web Store
Anaglyphohol on the Chrome Web Store: [Anaglyphohol](https://chromewebstore.google.com/detail/anaglyphohol/fjbffnhfchidmfcbecccnmdedjahankc)  
  
  It is recommended that you pin the Anaglyphohol extension button to the Chrome toolbar.  Anaglyphohol will create a transparent clickable icon at the top center of the webpage it loads on. Clicking this icon will toggle the UI which allows switching anaglyph modes, and toggling anaglyph mode on and off.

## Installing in development mode (bypass Chrome Store)
If you want to install your own build of Anaglyphohol or simply want to run the latest version before it is available on the Chrome Web Store, you can install it using Chrome in development mode.

- Unpack the Anaglyphohol [release](https://github.com/LostBeard/Anaglyphohol/releases) zip.
- Navigate to "chrome://extensions" in your browser
- Enable "Developer mode" at the top right
- Click "Load unpacked" and select the folder where you unpacked Anaglyphohol.

## Notes
Images are added to the conversion queue in the order they are found. Moving your mouse over an image will move the image to the front of the queue.

Anaglyphohol adds a border to images it identifies for conversion when enabled. 
- Orange - queued for conversion
- Green - already converted and showing the anaglyph image
- Blue - the image currently being converted
- Red - conversion failed (may requeue)
- None - Not supported (too small... less than 100x100), or not an `<img>` element.

## Known issues
- Currently only Google Chrome on Windows has been tested. Firefox desktop support is planned. 
- Some websites and some images on websites do not work. Ex. Google Photos does not work.
- Almost not user configurable settings. User settings are planned.

## Screenshots
Bing image search in red cyan  
![Screenshot Bing Red Cyan](https://raw.githubusercontent.com/LostBeard/Anaglyphohol/main/Anaglyphohol/wwwroot/screenshots/BingRedCyan.jpg)  
Google image search in green magenta  
![Screenshot Google Green Magenta](https://raw.githubusercontent.com/LostBeard/Anaglyphohol/main/Anaglyphohol/wwwroot/screenshots/GoogleGreenMagenta1.jpg)   

## Building
You can download Anaglyphohol, make changes, and build it yourself. The Blazor WebAssembly library [SpawnDev.BlazorJS.BrowserExtension](https://github.com/LostBeard/SpawnDev.BlazorJS.BrowserExtension) is used to interact with the extension APIs. If you have any questions or issues, don't hesitate to open an issue.

### Manifest
The extension `manifest.json` file is located in `Anaglyphohol\wwwroot` and is merged with `manifest.chrome.json` for the Chrome build and `manifest.firefox.json` for the Firefox build. This allows the use of common and browser dependent configurations.

### Debug Build
To create a `Debug` build of Anaglyphohol run `_buildDebug.bat` in the project folder. Builds for both Firefox and Chrome will be created in the `Anaglyphohol\bin\PublishDebug\` folder. The build can be loaded into Firefox and Chrome using development mode.

### Release Build
To create a `Release` build of Anaglyphohol run `_buildRelease.bat` in the project folder. Builds for both Firefox and Chrome will be created in the `Anaglyphohol\bin\PublishRelease\` folder. The build can be loaded into Firefox and Chrome using development mode. Zip files containing the extension will also be built for Firefox and Chrome.

# Get Support
Issues and feature requests can be submitted [here](https://github.com/LostBeard/Anaglyphohol/issues) on GitHub. We are always here to help.

# Support Us
Sponsor us via Github Sponsors to give us more time to work on Anaglyphohol and other open source projects. Or buy us a cup of coffee via Paypal. All support is greatly appreciated! ♥

[![GitHub Sponsor](https://img.shields.io/github/sponsors/LostBeard?label=Sponsor&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/LostBeard)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=2F6VANCK2EMEY)

# Thanks
Thank you to everyone who has helped!
