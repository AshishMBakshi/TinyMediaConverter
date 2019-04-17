# Tiny Media Converter

This is a Windows desktop (WPF) app that converts photos, videos, and Windows Living Images to a 16-bit RGB-565 (TSV) format viewable on [TinyCircuits'](https://tinycircuits.com/) TinyDuino / TinyScreen+ platform, with some extras.

## Getting Started

To process any media, first add the file(s) to the queue using the "Add" button at the top left. Next, select an output folder by clicking the "Browse" button on the right. Then press the respective button to perform whichever conversion you'd like. Any status updates will be shown in the textbox at the bottom.

### 1. Convert Images to TSV

Select your desired options (output size, extension, whether your target platform is Big Endian, desired rotation angle), then press the "Convert Images" button.

### 2. Convert Videos to TSV

Select your desired options (same as above), then press the "Convert Videos" button. The app automatically converts videos to ~30 fps. Currently the file extension must be MP4 for a video to be processed.

### 3. Convert Images to TSV Video

This function allows you to input a series of still images and combine them into a single output TSV video. Press the "Frames -> Video" button to start.

### 4. Extract Living Images MP4s

This feature allows you to extract the video portion of a [Microsoft Living Images](https://www.windowscentral.com/video-living-images-nokia-camera) photo (same as Apple Live Photos, but earlier), which is normally embedded within the JPG image, as an MP4. Each MP4 will be saved with the original file name, plus "_Ext", in the selected output folder. Press the "Extract Living Image MP4s" to start. To then convert these output videos to TSV, clear the input queue, add the output MP4s to the queue, and press the "Convert Videos" button.

## Authors

* **Ashish Bakshi** - [GitHub](https://github.com/AshishMBakshi) / [Site](http://www.ashishbakshi.com)

## License

This project is licensed under the GNU GPL v3 License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Several snippets within the code are derived from blog posts, StackOverflow answers, and the like -- all acknowledged in comments next to the respective code.
