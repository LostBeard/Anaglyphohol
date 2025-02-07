# Anaglyphohol

Anaglyphohol is a web browser extension that let's you view images on the web in anaglyph 3D. It supports green magenta, and red cyan glasses. View image search results in 3D on google.com, bing.com, and yahoo.com. Use Anaglyphohol on on almost any website. 

## Development
Anaglyphohol is developed using Blazor WebAssembly. The 2D to 3D magic is thanks to the amazing monocular depth estimation machine learning model [Depth Anything](https://huggingface.co/depth-anything/Depth-Anything-V2-Small).

Currently only Google Chrome on Windows ahs been tested. Firefox desktop support is planned.

## Screenshots
![Screenshot 4](https://raw.githubusercontent.com/LostBeard/Anaglyphohol/main/Anaglyphohol/wwwroot/screenshots/BingRedCyan.jpg)  
![Screenshot 4](https://raw.githubusercontent.com/LostBeard/Anaglyphohol/main/Anaglyphohol/wwwroot/screenshots/GoogleGreenMagenta1.jpg)   

## Installing in development mode (bypass Chrome Store)
Anaglyphohol has been submitted to the Chrome but has not yet been approved. If you would like to install the extension now or simply want to run the latest version, you can install it in Chrome in development mode.

### To load Anaglyphohol using Chrome development mode
- Unpack the Anaglyphohol [release](https://github.com/LostBeard/Anaglyphohol/releases) zip.
- Navigate to "chrome://extensions" in your browser
- Enable "Developer mode" at the top right
- Click "Load unpacked" and select the folder where you unpacked Anaglyphohol.

It is recommended that you pin the Anaglyphohol extension button to the Chrome toolbar.  Anaglyphohol will create a transparent clickable icon at the top center of the webpage it loads on. Clicking this icon will toggle the UI which allows switching anaglyph modes, and toggling anaglyph mode on and off.

# Get Support
Issues and feature requests can be submitted [here](https://github.com/LostBeard/Anaglyphohol/issues) on GitHub. We are always here to help.

# Support Us
Sponsor us via Github Sponsors to give us more time to work on Anaglyphohol and other open source projects. Or buy us a cup of coffee via Paypal. All support is greatly appreciated! ♥

[![GitHub Sponsor](https://img.shields.io/github/sponsors/LostBeard?label=Sponsor&logo=GitHub&color=%23fe8e86)](https://github.com/sponsors/LostBeard)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=2F6VANCK2EMEY)

# Thanks
Thank you to everyone who has helped!
