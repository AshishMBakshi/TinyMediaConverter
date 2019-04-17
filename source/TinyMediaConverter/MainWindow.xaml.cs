using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.IO.Compression;
using System.Windows.Threading;
using TinyMediaConverter;

namespace ImageConverterWPF
{
    // Tiny Media Converter
    //   by Ashish M. Bakshi
    //   August 2016
    //   Converts media (photos/videos/Living Images) for display on TinyCircuits' TinyScreen+ platform

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<string> sourceImagePaths = new ObservableCollection<string>();

        string outputDir; // output folder
        int outputWidth = 96; // output image width
        int outputHeight = 64; // output image height
        int outputRotationAngle = 0; // output rotation angle (CW)
        string outputExtension = "TSV"; // output extension, without dot
        bool outputBigEndian = true; // is the output for a big Endian platform?
        int outputBitMode = 16; // 8 or 16-bit output

        // byte[] TempByteArray = new byte[1]; // temp byte array

        public MainWindow()
        {
            InitializeComponent();
            lbSourceImages.ItemsSource = sourceImagePaths; // bind listbox to source paths list
        }

        #region UI Stuff

        private void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            // add files to list

            // show file picker menu
            //
            // info at https://msdn.microsoft.com/en-us/library/windows/apps/windows.storage.pickers.fileopenpicker.aspx?cs-save-lang=1&cs-lang=csharp#code-snippet-1
            //

            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //dlg.FileName = "Image"; // Default file name
            //dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "Images (jpg/jpeg/bmp/png/gif)|*.jpg;*.jpeg;*.bmp;*.png;*.gif|Videos (mp4)|*.mp4"; // Filter files by extension
            dlg.CheckFileExists = true;
            dlg.Multiselect = true;

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                foreach (string p in dlg.FileNames)
                {
                    // go through each selected file
                    // UpdateStatus("Adding image to list: " + p);
                    sourceImagePaths.Add(p);
                }
            }

        }

        private void btnRemoveFiles_Click(object sender, RoutedEventArgs e)
        {
            foreach (string rem in GetSelectedFiles())
            {
                // remove each file on the selection list
                // UpdateStatus("Removing image from list: " + rem);
                sourceImagePaths.Remove(rem);
            }

        }

        private void btnBrowseDestFolder_Click(object sender, RoutedEventArgs e)
        {
            // Open folder dialog box

            // oddly, MS doesn't expose a native folder browser in WPF, so we have to use the Windows API Code Pack

            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select Output Directory";
            dlg.IsFolderPicker = true;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtOutputDir.Text = dlg.FileName;
                outputDir = dlg.FileName;
            }

        }

        private List<string> GetSelectedFiles()
        {
            // gets list of selected files

            // using this rather than just going through each selected
            /// item in the listbox directly because, e.g., deleting an item from
            // bound list causes listbox to update and clear selection

            List<string> selFiles = new List<string>();

            if (lbSourceImages.SelectedItems.Count > 0)  // if a file is selected
            {
                foreach (string sel in lbSourceImages.SelectedItems)
                {
                    // add selected files to selection list
                    selFiles.Add(sel);
                }
            }

            return selFiles;
        }


        private void txtOutputWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            // make sure we have numbers only

            char[] originalText = txtOutputWidth.Text.ToCharArray();
            foreach (char c in originalText)
            {
                if (!(Char.IsNumber(c)))
                {
                    // non-number input
                    txtOutputWidth.Text = txtOutputWidth.Text.Remove(txtOutputWidth.Text.IndexOf(c));
                }
            }
            txtOutputWidth.Select(txtOutputWidth.Text.Length, 0);

            // update the var
            if (int.TryParse(txtOutputWidth.Text, out outputWidth))
            {
                // we're good
            }
            else
            {
                // it's not an int - set to 96
                outputWidth = 96;
                txtOutputWidth.Text = outputWidth.ToString();
            }
        }

        private void txtOutputHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            // make sure we have numbers only

            char[] originalText = txtOutputHeight.Text.ToCharArray();
            foreach (char c in originalText)
            {
                if (!(Char.IsNumber(c)))
                {
                    // non-number input
                    txtOutputHeight.Text = txtOutputHeight.Text.Remove(txtOutputHeight.Text.IndexOf(c));
                }
            }
            txtOutputHeight.Select(txtOutputHeight.Text.Length, 0);

            // update the var
            if (int.TryParse(txtOutputHeight.Text, out outputHeight))
            {
                // we're good
            }
            else
            {
                // it's not an int - set to 64
                outputHeight = 64;
                txtOutputHeight.Text = outputHeight.ToString();
            }
        }

        private void txtOutputExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            // extension can have letters or numbers, so no need to filter input

            outputExtension = txtOutputExtension.Text;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if ((outputExtension != "") && (outputDir != "") && (outputHeight > 0) && (outputWidth > 0) && (sourceImagePaths.Count > 0))
            {
                // if we have an output extension, directory, and dimensions, and images to convert, then go
                ConvertImagesFromPathList();
            }
        }

        private void cbRotationAngle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // update rotation angle

            int selAngle = 0;

            ComboBoxItem selItem = (ComboBoxItem)(cbRotationAngle.SelectedItem);
           
            if (Int32.TryParse(selItem.Content.ToString(), out selAngle))
            {
                // if we successfully got angle
                outputRotationAngle = selAngle;
            }

            // if degree signs in combobox items:

            //switch (cbRotationAngle.SelectedValue.ToString())
            //{
            //    case "0°":
            //        outputRotationAngle = 0;
            //        break;
            //    case "90°":
            //        outputRotationAngle = 90;
            //        break;
            //    case "180°":
            //        outputRotationAngle = 180;
            //        break;
            //    case "270°":
            //        outputRotationAngle = 270;
            //        break;
            //    default:
            //        outputRotationAngle = 0;
            //        break;
            //}
        }

        private void txtStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            // scroll to end whenever updated
            txtStatus.ScrollToEnd();
        }

        private void btnExtractLIvideos_Click(object sender, RoutedEventArgs e)
        {
            if ((outputDir != "") && (sourceImagePaths.Count > 0))
            {
                // extract living image videos
                ExtractLivingImageVideosFromPathList();
            }
        }

        private void btnMakeVideo_Click(object sender, RoutedEventArgs e)
        {
            if ((outputDir != "") && (sourceImagePaths.Count > 0))
            {
                // make video from photos
                CreateConvertedVideoFromPathList();
            }
        }

        private void btnProcessVideo_Click(object sender, RoutedEventArgs e)
        {
            ExtractVideoFramesFromPathList();
        }

        private void chkBigEndian_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // checkbox value is bool?, meaning true/false/null, so need to cast to bool
                outputBigEndian = (bool)chkBigEndian.IsChecked;
            }
            catch
            {
                // should only get error if checkbox value is somehow null
                outputBigEndian = true;
                chkBigEndian.IsChecked = true;
            }
        }

        #endregion

        #region Prior Conversion Methods

        //private void ConvertImage(string filePath)
        //{
        //    // convert image
        //    // Load up the original bitmap.
        //    Bitmap source = (Bitmap)Bitmap.FromFile(filePath);

        //    //Creat a 16 bit copy of it using the graphics class
        //    Bitmap dest = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
        //    using (Graphics g = Graphics.FromImage(dest))
        //    {
        //        g.DrawImageUnscaled(source, 0, 0);
        //    }

        //    //Get a copy of the byte array you need to send to your tft
        //    Rectangle r = new Rectangle(0, 0, dest.Width, dest.Height);
        //    BitmapData bd = dest.LockBits(r, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
        //    TempByteArray = new byte[dest.Width * dest.Height * 2];
        //    Marshal.Copy(bd.Scan0, TempByteArray, 0, TempByteArray.Length);
        //    dest.UnlockBits(bd);

        //    //A 16 bit copy of the data you need to send to your tft is now sitting in the TftData array.

        //    UpdateStatus("Converted file to new format:");
        //}

        //private void ConvertImage2(string filePath)
        //{

        //    // different approach

        //    BitmapImage source = new BitmapImage(new Uri(filePath));

        //    FormatConvertedBitmap FmtCnvBmp = new FormatConvertedBitmap(source, PixelFormats.Bgr565, null, 1.0);
        //    TempByteArray = new byte[96 * 64 * 2];
        //    FmtCnvBmp.CopyPixels(TempByteArray, 96 * 2, 0);
        //}

        //private void ConvertImage3(string filePath)
        //{

        //    // approach 3 - 2 + reversing array

        //    BitmapImage source = new BitmapImage(new Uri(filePath));

        //    FormatConvertedBitmap FmtCnvBmp = new FormatConvertedBitmap(source, PixelFormats.Bgr565, null, 1.0);
        //    TempByteArray = new byte[96 * 64 * 2];
        //    FmtCnvBmp.CopyPixels(TempByteArray, 96 * 2, 0);

        //    Array.Reverse(TempByteArray);
        //}

        //private void ConvertImage4(string filePath)
        //{

        //    // approach 4 - 2 + converting each

        //    BitmapImage source = new BitmapImage(new Uri(filePath));

        //    FormatConvertedBitmap FmtCnvBmp = new FormatConvertedBitmap(source, PixelFormats.Bgr565, null, 1.0);
        //    TempByteArray = new byte[96 * 64 * 2];
        //    FmtCnvBmp.CopyPixels(TempByteArray, 96 * 2, 0);

        //    //for (int i = 1; i <= TempByteArray.Length; i++)
        //    //{
        //    //}

        //    int x = BitConverter.ToInt32(TempByteArray, 0);
        //    int y = Endian.SwapInt32(x);
        //    TempByteArray = BitConverter.GetBytes(y);
        //}

        //private void ConvertImage5(string filePath)
        //{
        //    Bitmap bmp = (Bitmap)Bitmap.FromFile(filePath);

        //    // this gets one line's output

        //    var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
        //            ImageLockMode.ReadOnly,
        //            System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
        //        try
        //        {
        //            // var ptr = (IntPtr)((long)data.Scan0 + data.Stride * (bmp.Height - line - 1));  // commented out to avoid errors
        //            // var ret = new int[bmp.Width];
        //            // System.Runtime.InteropServices.Marshal.Copy(ptr, ret, 0, ret.Length * 4);
        //            // TempByteArray = ret; // commented out to avoid errors
        //        }
        //        finally
        //        {
        //            bmp.UnlockBits(data);
        //        }


        //}

        //private void ConvertImage6(string filePath)
        //{
        //    // manual conversion to RGB 565
        //    // from Java code at http://stackoverflow.com/questions/8319770/java-image-conversion-to-rgb565

        //    // load original bitmap
        //    Bitmap bufImg = (Bitmap)Bitmap.FromFile(filePath);

        //    // create 16 bit copy using graphics class
        //    Bitmap sendImg = new Bitmap(bufImg.Width, bufImg.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
        //    using (Graphics g = Graphics.FromImage(sendImg))
        //    {
        //        g.DrawImageUnscaled(bufImg, 0, 0);
        //    }

        //    int numByte = 0;
        //    byte[] OutputImageArray = new byte[outputWidth * outputHeight * 2];

        //    int i = 0;
        //    int j = 0;
        //    int len = OutputImageArray.Length;

        //    for (i = 0; i < outputWidth; i++)
        //    {
        //        for (j = 0; j < outputHeight; j++)
        //        {

        //            System.Drawing.Color c = sendImg.GetPixel(i, j);
        //            int aRGBpix = sendImg.GetPixel(i, j).ToArgb();
        //            int alpha;
        //            int red = c.R;
        //            int green = c.G;
        //            int blue = c.B;

        //            //RGB888
        //            red = (aRGBpix >> 16) & 0x0FF;
        //            green = (aRGBpix >> 8) & 0x0FF;
        //            blue = (aRGBpix >> 0) & 0x0FF;
        //            alpha = (aRGBpix >> 24) & 0x0FF;

        //            //RGB565
        //            red = red >> 3;
        //            green = green >> 2;
        //            blue = blue >> 3;

        //            //A pixel is represented by a 4-byte (32 bit) integer, like so:
        //            //00000000 00000000 00000000 11111111
        //            //^ Alpha  ^Red     ^Green   ^Blue
        //            //Converting to RGB565

        //            short pixel_to_send = 0;
        //            int pixel_to_send_int = 0;
        //            pixel_to_send_int = (red << 11) | (green << 5) | (blue);
        //            pixel_to_send = (short)pixel_to_send_int;

        //            //dividing into bytes
        //            byte byteH = (byte)((pixel_to_send >> 8) & 0x0FF);
        //            byte byteL = (byte)(pixel_to_send & 0x0FF);

        //            //Writing it to array - High-byte is second
        //            OutputImageArray[numByte] = byteH;
        //            OutputImageArray[numByte + 1] = byteL;

        //            numByte += 2;
        //        }
        //    }

        //    // done - now return val
        //    TempByteArray = new byte[outputWidth * outputHeight * 2];
        //    TempByteArray = OutputImageArray;
        //}

        //public static void getRGB(Bitmap image, int startX, int startY, int w, int h, int[] rgbArray, int offset, int scansize)
        //{
        //    // function from http://stackoverflow.com/questions/4747428/getting-rgb-array-from-image-in-c-sharp
        //    //
        //    // for ConvertImage6 (Java-adapted function)
        //    //
        //    const int PixelWidth = 3;
        //    const System.Drawing.Imaging.PixelFormat PixFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

        //    // En garde!
        //    if (image == null) throw new ArgumentNullException("image");
        //    if (rgbArray == null) throw new ArgumentNullException("rgbArray");
        //    if (startX < 0 || startX + w > image.Width) throw new ArgumentOutOfRangeException("startX");
        //    if (startY < 0 || startY + h > image.Height) throw new ArgumentOutOfRangeException("startY");
        //    if (w < 0 || w > scansize || w > image.Width) throw new ArgumentOutOfRangeException("w");
        //    if (h < 0 || (rgbArray.Length < offset + h * scansize) || h > image.Height) throw new ArgumentOutOfRangeException("h");

        //    BitmapData data = image.LockBits(new Rectangle(startX, startY, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, PixFormat);
        //    try
        //    {
        //        byte[] pixelData = new Byte[data.Stride];
        //        for (int scanline = 0; scanline < data.Height; scanline++)
        //        {
        //            Marshal.Copy(data.Scan0 + (scanline * data.Stride), pixelData, 0, data.Stride);
        //            for (int pixeloffset = 0; pixeloffset < data.Width; pixeloffset++)
        //            {
        //                // PixelFormat.Format32bppRgb means the data is stored
        //                // in memory as BGR. We want RGB, so we must do some 
        //                // bit-shuffling.
        //                rgbArray[offset + (scanline * scansize) + pixeloffset] =
        //                    (pixelData[pixeloffset * PixelWidth + 2] << 16) +   // R 
        //                    (pixelData[pixeloffset * PixelWidth + 1] << 8) +    // G
        //                    pixelData[pixeloffset * PixelWidth];                // B
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        image.UnlockBits(data);
        //    }
        //}

        #endregion

        #region Image Processing

        private byte[] ConvertImage(BitmapSource source)
        {
            // approach #7 - #3 + 180 rotation
            //
            // converts to RGB565, then flips array at end (for big Endian)
            //   so in between, rotates 180 deg to compensate

            FormatConvertedBitmap FmtCnvBmp;

            if (outputBigEndian)
            {
                // big Endian, so let's first rotate 180

                TransformedBitmap transformBmp = new TransformedBitmap();
                transformBmp.BeginInit();
                transformBmp.Source = source;
                RotateTransform transform = new RotateTransform(180);
                transformBmp.Transform = transform;
                transformBmp.EndInit();

                FmtCnvBmp = new FormatConvertedBitmap(transformBmp, PixelFormats.Bgr565, null, 1.0);
            }
            else
            {
                // little Endian- nothing to do
                FmtCnvBmp = new FormatConvertedBitmap(source, PixelFormats.Bgr565, null, 1.0);
            }

            byte[] TempByteArray = new byte[96 * 64 * 2];
            FmtCnvBmp.CopyPixels(TempByteArray, 96 * 2, 0);

            if (outputBigEndian) {
                // if big Endian, time to reverse the array
                Array.Reverse(TempByteArray);
            }

            return TempByteArray;
        }

        private byte[] ConvertResizeImageFromPath(string filePath, int newWidth, int newHeight)
        {
            // resizes and converts image to RGB565 from path

            //return ConvertImage(RotateImageIfVertical(ResizeImageFromPath(filePath, newWidth, newHeight)));
            // NOT WORKING YET

            // manual switch for rotation
            if (outputRotationAngle == 0)
            {
                return ConvertImage(ResizeImageFromPath(filePath, newWidth, newHeight));
            }
            else
            {
                return ConvertImage(RotateImage(ResizeImageFromPath(filePath, newWidth, newHeight), outputRotationAngle));
            }
        }

        private byte[] ConvertResizeImage(BitmapImage inputImage, int newWidth, int newHeight)
        {
            // resizes and converts image to RGB565 from image

            //return ConvertImage(RotateImageIfVertical(ResizeImageFromPath(filePath, newWidth, newHeight)));
            // NOT WORKING YET

            // manual switch for rotation
            if (outputRotationAngle == 0)
            {
                return ConvertImage(ResizeImage(inputImage, newWidth, newHeight));
            }
            else
            {
                return ConvertImage(RotateImage(ResizeImage(inputImage, newWidth, newHeight), outputRotationAngle));
            }
        }

        private BitmapSource RotateImageIfVertical(BitmapImage inputImage, bool rotateRight = true)
        {
            // if image is vertical, rotate to the right (90 deg), or left (270 deg)

            // NOT WORKING - (1) we lose any idea of verticality in the resizing process
            //  (2) image orientation via EXIF is a fairly complex issue: http://www.daveperrett.com/articles/2012/07/28/exif-orientation-handling-is-a-ghetto/
            //  So handling manually via user switch for now

            if (inputImage.Width > inputImage.Height)
            {
                // horizontal image - no work to do here
                return inputImage;
            }
            else
            {
                // vertical image
                if (rotateRight)
                {
                    // rotate 90
                    return RotateImage(inputImage, 90);
                }
                else
                {
                    // rotate 270
                    return RotateImage(inputImage, 270);
                }
            }
        }

        private TransformedBitmap RotateImage(BitmapImage inputImage, int angle)
        {
            TransformedBitmap TempImage = new TransformedBitmap();

            TempImage.BeginInit();
            TempImage.Source = inputImage;

            RotateTransform transform = new RotateTransform(angle += 90);
            TempImage.Transform = transform;
            TempImage.EndInit();

            return TempImage;
        }

        private BitmapImage ResizeImageDNU(BitmapImage inputImage, int newWidth, int newHeight)
        {
            // basic resize routine
            // DOESN'T WORK - DO NOT USE

            BitmapImage bi = inputImage.Clone(); // prob won't be able to manipulate after this...
            bi.BeginInit();
            // bi.UriSource = photoPath;
            bi.DecodePixelWidth = newWidth;
            bi.DecodePixelHeight = newHeight;
            bi.EndInit();

            return bi;
        }

        private BitmapImage ResizeImage(BitmapSource imgSource, int width, int height, BitmapScalingMode scaleMode = BitmapScalingMode.HighQuality)
        {
            // resize image at high quality
            // from https://xiu.shoeke.com/2010/07/15/resizing-images-with-wpf-4-0/
            ///
            //      TO DO - clean up: this is very similar to ResizeImage3
            //
            int margin = 0; // no marging
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, scaleMode);
            group.Children.Add(new ImageDrawing(imgSource, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            ////TESTING BY SAVING FILES TO FOLDER
            //JpegBitmapEncoder jpg = new JpegBitmapEncoder();
            //jpg.Frames.Add(BitmapFrame.Create(imgSource));
            //using (Stream stm = File.Create(Path.Combine(outputDir, "testimage" + (new Random().Next(1, 10000)) + ".jpg")))
            //{
            //    jpg.Save(stm);
            //}

            return ConvertRenderTargetBitmapToBitmapImage(resizedImage);
            //return BitmapFrame.Create(resizedImage); // switched to BitmapImage instead of BitmapFrame
        }

        private BitmapImage ResizeImageFromPath(string inputPath, int newWidth, int newHeight)
        {
            // basic resize routine - should avoid double rendering from ResizeImage

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(inputPath);
            bi.DecodePixelWidth = newWidth;
            bi.DecodePixelHeight = newHeight;
            bi.EndInit();

            return bi;
        }

        private TransformedBitmap ResizeImage2(BitmapImage origImg, int newWidth, int newHeight)
        {
            // mid-complexity resize routine using ScaleTransform
            TransformedBitmap tBitmap = new TransformedBitmap();
            tBitmap.BeginInit();
            tBitmap.Source = origImg;
            ScaleTransform scale = new ScaleTransform(newWidth / origImg.PixelWidth, newHeight / origImg.PixelHeight);
            tBitmap.Transform = scale;
            tBitmap.EndInit();

            return tBitmap;
        }

        public static BitmapFrame ResizeImage3(BitmapFrame photo, int width, int height, BitmapScalingMode scalingMode = BitmapScalingMode.Fant)
        {
            // more detailed resize routine
            //   from http://weblogs.asp.net/bleroy/resizing-images-from-the-server-using-wpf-wic-instead-of-gdi

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, scalingMode);
            group.Children.Add(new ImageDrawing(photo, new Rect(0, 0, width, height)));
            var targetVisual = new DrawingVisual();
            var targetContext = targetVisual.RenderOpen();
            targetContext.DrawDrawing(group);
            var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
            targetContext.Close();
            target.Render(targetVisual);
            var targetFrame = BitmapFrame.Create(target);
            return targetFrame;
        }

        public bool SaveByteArrayToFile(byte[] _ByteArray, string _FileName)
        {
            try
            {
                // Open file for reading

                using (System.IO.FileStream _FileStream = new System.IO.FileStream(_FileName,
                    System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    // Writes a block of bytes to this stream using data from a byte array.
                    _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                    // close file stream
                    _FileStream.Close();

                    // UpdateStatus("Saved new file as: " + _FileName);

                    return true;
                }
            }
            catch // (Exception _Exception)
            {
                // Error
                // MessageBox.Show("Error: " + _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

        #endregion

        private void ConvertImagesFromPathList()
        {
            // convert all images in list
            foreach (string p in sourceImagePaths)
            {
                // convert image
                UpdateStatus("Converting image: " + p);
                byte[] imageOutput = ConvertResizeImageFromPath(p, outputWidth, outputHeight); // resize + convert

                // then save it
                System.IO.FileInfo sourceFileInfo = new System.IO.FileInfo(p);
                System.IO.DirectoryInfo destDirInfo = new System.IO.DirectoryInfo(outputDir);
                string destPath = System.IO.Path.Combine(destDirInfo.FullName, sourceFileInfo.Name.Replace(sourceFileInfo.Extension, "." + outputExtension));

                UpdateStatus("Saving converted image as: " + destPath);
                SaveByteArrayToFile(imageOutput, destPath);
            }
        }

        private void ExtractLivingImageVideosFromPathList()
        {
            // extract videos from any Living Images in list
            foreach (string p in sourceImagePaths)
            {
                if (p.Contains("_LI"))
                {
                    // filename contains _LI
                    UpdateStatus("Processing Living Image: " + p);

                    // make copy of file as a ZIP in the output directory
                    FileInfo sourceFileInfo = new FileInfo(p);
                    DirectoryInfo destDirInfo = new DirectoryInfo(outputDir);
                    string zipPath = Path.Combine(destDirInfo.FullName, sourceFileInfo.Name.Replace(sourceFileInfo.Extension, ".zip"));
                    File.Copy(p, zipPath);

                    try
                    {
                        // see if ZIP contains an MP4
                        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                                {
                                    // we have an MP4 - save it
                                    string MP4saveName = sourceFileInfo.Name.Replace(sourceFileInfo.Extension, "_Ext.mp4");
                                    string MP4fullPath = Path.Combine(destDirInfo.FullName, MP4saveName);
                                    entry.ExtractToFile(MP4fullPath);
                                    UpdateStatus("Extracted video from Living Image: " + MP4fullPath);
                                }
                            }
                        }

                        // got the MP4, now move the image into a folder for successfully extracted LIs
                        // File.Move(p, Path.Combine(destDirInfo.FullName, "LI", sourceFileInfo.Name));
                    }
                    catch
                    {
                        // error - must not be an LI, so move it to the non-LI folder
                        // File.Move(p, Path.Combine(destDirInfo.FullName, "NonLI", sourceFileInfo.Name));
                    }

                    // done - now delete the ZIP
                    File.Delete(zipPath);
                }
            }
        }

        private void CreateConvertedVideoFromPathList()
        {
            // create converted video from path list of frames
            // convert all images in list

            byte[] videoOutput = new byte[0];
            foreach (string p in sourceImagePaths)
            {
                // convert image and add to video output byte
                UpdateStatus("Converting frame: " + p);
                videoOutput = CombineByteArrays(videoOutput, ConvertResizeImageFromPath(p, outputWidth, outputHeight)); // resize + convert

            }

            // then save it

            DirectoryInfo destDirInfo = new DirectoryInfo(outputDir);
            string destPath = Path.Combine(destDirInfo.FullName, "OutputVid." + outputExtension);

            UpdateStatus("Saving converted video as: " + destPath);
            SaveByteArrayToFile(videoOutput, destPath);
        }

        private byte[] CombineByteArrays(params byte[][] arrays)
        {
            // from http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp

            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private void UpdateStatus(string updText)
        {
            // update status box
            //txtStatus.Text += updText + Environment.NewLine;
            txtStatus.AppendText(updText + Environment.NewLine); // faster, and auto-scrolls to bottom

            // AppendText is not scrolling, so let's try manually
            //txtStatus.SelectionStart = txtStatus.Text.Length;
            //txtStatus.ScrollToCaret();
        }

        #region Video Frame Capture

        // adapted from http://www.thomasclaudiushuber.com/blog/2008/04/09/take-snapshots-part-ii-save-as-animated-gif/

        List<BitmapImage> videoFrames = new List<BitmapImage>(); // collection of video frames
        List<string> videoProcessingQueue = new List<string>(); // queue of paths of videos to process
        string currentVideoPath; // path of currently-processing video
        bool processingVideo = false; // currently saving video?

        private void meMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            int fps = 30; // frames per seconds for capture

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000 / fps); // at 30 fps, 33.3 ms per frame
            _timer.Tick += _timer_OnTick;
            _timer.Start();
        }

        DispatcherTimer _timer;

        void _timer_OnTick(object sender, EventArgs e)
        {
            // initial approach, not working - produces black images

            //System.Drawing.Size dpi = new System.Drawing.Size(96, 96);
            //RenderTargetBitmap bmp = new RenderTargetBitmap(96, 64, dpi.Width, dpi.Height, PixelFormats.Pbgra32); // changed from Pbgra32
            //bmp.Render(meMedia);
            // // videoFrames.Add(BitmapFrame.Create(bmp));  // switched to BitmapImage list instead of BitmapFrame
            // videoFrames.Add(ConvertRenderTargetBitmapToBitmapImage(bmp));  // add output to frame collection

            // approach 2
            //videoFrames.Add(GetVideoPlayerScreenshotResized(outputWidth, outputHeight));
            UpdateStatus("Capturing frame");
            videoFrames.Add(GetVideoPlayerScreenshot());
        }

        private void meMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Tick -= _timer_OnTick;
            //_timer = null;

            // output the file
            UpdateStatus("Extracted " + videoFrames.Count.ToString() + " frames from video " + currentVideoPath);
            CreateConvertedVideoFromExtractedFrames();

            // done processing this video, so remove it from queue
            videoProcessingQueue.Remove(currentVideoPath);
            // now process any others remaining in queue
            ProcessVideoConversionQueue();
        }

        private void ExtractVideoFramesFromPathList()
        {
            // called from UI to load video into MediaElement, play, and capture
            //
            
            if (sourceImagePaths.Count > 0)
            {
                foreach (string p in sourceImagePaths)
                {
                    // for each file
                    if (p.EndsWith("mp4",StringComparison.CurrentCultureIgnoreCase))
                    {
                        // if we have a video, add it to the queue
                        videoProcessingQueue.Add(p);
                    }
                }

                // done adding files to queue, now process them
                ProcessVideoConversionQueue();
            }
        }

        private void ProcessVideoConversionQueue()
        {
            while (processingVideo)
            {
                // if processing video, wait till done
            }

            // processes next item in video conversion queue
            if (videoProcessingQueue.Count > 0)
            {
                // we have work to do
                currentVideoPath = videoProcessingQueue[0];
                UpdateStatus("Processing video " + currentVideoPath);
                processingVideo = true; // make sure we don't start another yet
                meMedia.Source = new Uri(currentVideoPath);
                meMedia.Play();
                // when the MediaElement starts playing, frame extraction will kick in
            }
        }

        private void CreateConvertedVideoFromExtractedFrames()
        {
            // create converted video from frames residing in videoFrames var
            // convert all images in list

            byte[] videoOutput = new byte[0];

            for (int i = 0; i < videoFrames.Count; i++)
            {
                // for each frame, convert image and add to video output byte
                // UpdateStatus("Converting frame: " + (i + 1) + " of " + videoFrames.Count);
                videoOutput = CombineByteArrays(videoOutput, ConvertResizeImage(videoFrames[i], outputWidth, outputHeight)); // resize + convert
            }

            // then save it

            FileInfo sourceFileInfo = new FileInfo(currentVideoPath);
            DirectoryInfo destDirInfo = new DirectoryInfo(outputDir);
            string destPath = Path.Combine(destDirInfo.FullName, sourceFileInfo.Name.Replace(sourceFileInfo.Extension, "_NA." + outputExtension));
            // saving as same file name as source, but in dest folder, with _NA.TSV at end
            //   TO DO: make naming a user option

            UpdateStatus("Saving converted video as: " + destPath);
            
            SaveByteArrayToFile(videoOutput, destPath);

            // clear the video buffer
            videoFrames.Clear();
            videoFrames.TrimExcess();
            
            processingVideo = false; // we're done
        }

        private static BitmapImage ConvertRenderTargetBitmapToBitmapImage(RenderTargetBitmap inputRTB)
        {
            // converts a RenderTargetBitmap to BitmapImage
            //   from http://stackoverflow.com/questions/13987408/convert-rendertargetbitmap-to-bitmapimage
            //   see also http://stackoverflow.com/questions/20083210/convert-rendertargetbitmap-to-bitmap?rq=1

            var bitmapImage = new BitmapImage();
            //var bitmapEncoder = new PngBitmapEncoder();
            var bitmapEncoder = new BmpBitmapEncoder(); // BMP instead of PNG - no need for transparency
            bitmapEncoder.Frames.Add(BitmapFrame.Create(inputRTB));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }

        #endregion

        private void btnSaveVideoSnapshot_Click(object sender, RoutedEventArgs e)
        {
            SaveTestImageToFile(GetVideoPlayerScreenshotResized(outputWidth, outputHeight));
        }

        public BitmapImage GetVideoPlayerScreenshot()
        {
            // adapted from http://www.c-sharpcorner.com/UploadFile/dpatra/take-screen-shot-from-media-element-in-wpf/

            // rather than taking source as as a UIElement parameter, we'll hard-code it
            UIElement source = meMedia;

            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)actualWidth, (int)actualHeight, 96, 96, PixelFormats.Pbgra32);

            renderTarget.Render(meMedia);

            VisualBrush sourceBrush = new VisualBrush(source);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                // drawingContext.PushTransform(new ScaleTransform(scaleWidth, scaleHeight)); // no scaling
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(actualWidth, actualHeight)));
            }
            renderTarget.Render(drawingVisual);

            // convert to BitmapImage
            return ConvertRenderTargetBitmapToBitmapImage(renderTarget);
        }

        public BitmapImage GetVideoPlayerScreenshotResized(int renderWidth, int renderHeight)
        {
            // adapted from http://www.c-sharpcorner.com/UploadFile/dpatra/take-screen-shot-from-media-element-in-wpf/

            // rather than taking source as as a UIElement parameter, we'll hard-code it
            UIElement source = meMedia;

            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;
            double scaleHeight = (renderHeight / actualHeight);
            double scaleWidth = (renderWidth / renderHeight);

            RenderTargetBitmap renderTarget = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);

            renderTarget.Render(meMedia);

            VisualBrush sourceBrush = new VisualBrush(source);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.PushTransform(new ScaleTransform(scaleWidth, scaleHeight));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(actualWidth, actualHeight)));
            }
            renderTarget.Render(drawingVisual);

            // convert to BitmapImage
            return ConvertRenderTargetBitmapToBitmapImage(renderTarget);
        }

        private void SaveTestImageToFile(BitmapSource src)
        {
            // saves a test file to disk
            JpegBitmapEncoder jpg = new JpegBitmapEncoder();
            jpg.Frames.Add(BitmapFrame.Create(src));
            using (Stream stm = File.Create(Path.Combine(outputDir, "testimage" + (new Random().Next(1, 10000)) + ".jpg")))
            {
                jpg.Save(stm);
            }

        }

        private void BtnAboutHelp_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
    }
}
