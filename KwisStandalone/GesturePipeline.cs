using AForge.Imaging;
using AForge.Imaging.Filters;
using KwisStandalone.Properties;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/* This is the meat of the application.
 * This initializes the gesture pipeline and sets
 * a loop in mottion watching the frames coming in from
 * the camera. Data on the location (or lack thereof) of the user's
 * hand is extracted on each frame and compared with reference bitmaps.
 * */


namespace KwisStandalone
{
    class GesturePipeline
    {


        // DLLs to activate the eshell application and send keystrokes
        // and appcommands (remote control messages).
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        extern static IntPtr GetDesktopWindow();


        //The amount of time to wait for a next gesutre, once one is recognized.
        private const int sleep = 1300;

        /* For whatever reason, WMC has key combinatinos for everything except
         * rewind!  These two constants contains the hex code for the special
         * media key for rewind.
         * */
        private static int APPCOMMAND_MEDIA_REWIND = 0x320000;
        private const int WM_APPCOMMAND = 0x319;

        //Invoke an instance of AForge's templating matching algorithm.
        ExhaustiveTemplateMatching templateMatching;



        public GesturePipeline(Form1 form)
            : base()
        {

            //------------------------------
            // Creates datastructures for 
            // the PCSDK
            //------------------------------
            UtilMPipeline pp; //The pipeline
            
            PXCMGesture.Blob bdata; //The intrepred blobs
            PXCMImage bimage; //The frame from the camera
            PXCMImage.ImageData data; //The data contained in the frame.
            PXCMImage.ImageInfo info; //Metadata about the frame.
            int color; //The "color" for the hand in question. 
            //------------------------------
        

            //------------------------------
            // Initilize the datastructures
            // defined above.
            //------------------------------
            Coord[] locations;
            pp = new UtilMPipeline();
            pp.EnableGesture();
            pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);
            //-----------------------------

            //initilize the pointer for the WMC window.
            IntPtr handle = GetDesktopWindow();


            //----------------------------------
            // Create and initilize the bitmaps
            // that the frames will be comapred 
            // against.
            //---------------------------------
            Bitmap pause = convertBitmap(KwisStandalone.Properties.Resources.STOP);
            Bitmap fastForward = convertBitmap(KwisStandalone.Properties.Resources.FAST_FORWARD);
            Bitmap rewind = convertBitmap(KwisStandalone.Properties.Resources.REWIND);
            Bitmap play = convertBitmap(KwisStandalone.Properties.Resources.PLAY);
            Bitmap menu = convertBitmap(KwisStandalone.Properties.Resources.MENU);
            Bitmap volUp = convertBitmap(KwisStandalone.Properties.Resources.VOL_UP);
            Bitmap volDown = convertBitmap(KwisStandalone.Properties.Resources.VOL_DOWN);
            //--------------------------------

            // initial the template matching algorithm.
            templateMatching = new ExhaustiveTemplateMatching(0.75f);


            /* If we were able to aquire a frame from the camera,
             * start the main application logic.
             * */
            if (pp.Init())
            {

                //Loop forever as long as the form is still open.
                while (!form.closing)
                {
                    if (!pp.AcquireFrame(true))
                    {
                        MessageBox.Show("Failed to aquire a frame.", "Kwi-S", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                        break;
                    }
                    
                    //Aquire the gesture data for the frame.
                    using (PXCMGesture gesture = pp.QueryGesture())
                    {
                        

                        //Load gesture data into respective data structures.
                        gesture.QueryBlobData(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bdata);
                        gesture.QueryBlobImage(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bimage);
                        bimage.AcquireAccess(PXCMImage.Access.ACCESS_READ, out  data);
                        info = bimage.imageInfo;



                        /* Although this says left hand,
                         * this actually aquires the right hand.
                         * This is from trial and error and contradicts
                         * both the documentation and common sense.  A bug 
                         * in the SDK?
                         * */
                        color = (int)bdata.labelLeftHand;

                        //Store the pixels that include the hand specified above.
                        locations = new Coord[info.width * info.height];
                        handLocation(ref locations, data, (int)info.width);

                        //create a new bitmap from this data.
                        Bitmap bitmap = createBitmap(ref locations, (int)info.width, (int)info.height);


                        /* Zero is the left hand (actuall right).  So, if we detected this,
                         * then lets see if it matches any of the stored reference images.
                         * */
                        if (color == 0)
                        {



                            try
                            {
                                if (compare(ref bitmap, ref  pause))
                                {
                                    clickAndSendKey(RemoteKey.PLAY_PAUSE);

                                }
                                else if (compare(ref bitmap, ref play))
                                {

                                    clickAndSendKey(RemoteKey.PLAY);
                                }
                                else if (compare(ref bitmap, ref fastForward))
                                {

                                    clickAndSendKey(RemoteKey.FAST_FORWARD);
                                }

                                else if (compare(ref bitmap, ref rewind))
                                {
                                    clickAndSendKey(APPCOMMAND_MEDIA_REWIND, handle);
                                }
                                else if (compare(ref bitmap, ref menu))
                                {
                                   
                                    clickAndSendKey(RemoteKey.STOP);
                                }
                                else if (compare(ref bitmap, ref volUp))
                                {
                                   clickAndSendKey(RemoteKey.VOL_UP);
                                }
                                else if (compare(ref bitmap, ref volDown))
                                {

                                    clickAndSendKey(RemoteKey.VOL_DOWN);
                                }



                            }
                            catch (InvalidImagePropertiesException iipe)
                            {

                            }

                        }

                        //Clean up the datastructures in prep for a new frame.
                        bitmap.Dispose();
                        bimage.ReleaseAccess(ref data);
                        bimage.Dispose();
                        gesture.Dispose();
                    }

                    pp.ReleaseFrame();
                }
            }

        }

        /* Invokes the AForge image matching algorithm
         * to compare the reference image with the one passed
         * in.
         * */
        private bool compare(ref Bitmap reference, ref Bitmap target)
        {

            if (templateMatching.ProcessImage(reference, resizeBitmap(target, reference.Width, reference.Height)).Length > 0) return true;
            else return false;


        }

        /* These two methods just take either a string in 
         * the case of a simple key combo, or a key and pointer
         * for more complicated appcommands.  This is then sent 
         * to the eshell window.
         * */
        public void clickAndSendKey(String key)
        {
            //Find eshel app.
            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("ehshell");
            if (p.Length > 0) //found
            {
                //Set it as the foreground app.
                SetForegroundWindow(p[0].MainWindowHandle);
            }

            //Send the keystroke.
            SendKeys.SendWait(key);

            //Sleep so we don't send too many commands at once.
            System.Threading.Thread.Sleep(sleep);

        }

        public void clickAndSendKey(int key, IntPtr handle)
        {
            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("ehshell");
            IntPtr ptr = p[0].MainWindowHandle;
            if (p.Length > 0) //found
            {
                SetForegroundWindow(ptr);
            }

            
            SendMessageW(ptr, WM_APPCOMMAND, ptr, (IntPtr)key);
            System.Threading.Thread.Sleep(sleep);
        }


        /* Given a full frame, this isolates the pixels that actually
         * contain the hand in question.  It then returns the pixel locations
         * for this hand.
         * */
        public void handLocation( ref Coord[] locations,PXCMImage.ImageData data, int width)
        {

            //Load the depth data from the frame.
            IntPtr ptr = data.buffer.planes[0];

            //Initilize the byte array for the bitmap.
            byte[] rgbValues = new byte[locations.Length];

            //Copy the passed in image data into byte array.
            Marshal.Copy(ptr, rgbValues, 0, locations.Length);


            //Loop through the values indetifying the hand values.
            int counter=0;
            int i;

            for (i = 0; i < locations.Length; i++)
            {
                if (rgbValues[i] == 0 )
                {
                    locations[counter++] = new Coord(i,width);

                }
            }
           
            //Resize the array to the smaller size.
            if(counter >1) Array.Resize(ref locations, counter - 1);

            return;
           
        }

        /* Given an array of hand locations, this method creates
         * a new bit map which ONLY contains the hand.  It also 
         * crops the image to a rectangle bounding the hand.
         * */
        private Bitmap createBitmap(ref Coord[] locations, int width, int height)
        {

            //initilize the bitmap we will be writing into.  initially this will be
            // the full frame.
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            //Begin copying data into the new bitmap.  We start at the first pointer value.
            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * bitmap.Height;

            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);

            /* Loop through the locations, lighting up the pixels 
             * stored in the location array.
             * */
            for (int i = 0; i < locations.Length; i++)
            {

                try
                {

                    rgbValues[locations[i].getLoc()] = 255; //make the pixels white where the hand is.
                }
                catch (NullReferenceException nre)
                {
                    break; //If for some reason the frame was corrupt, the value might be bad, so bail.
                }

            }
            
            //Write values to the bitarray.
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);
            
            
            //Send this on to tightly crop to the hand, then return.
            return cropToHand(bitmap, ref locations);
        }

        /* For some reason, When c# loads a bitmap, it changes the pixel format.
         * This is annoying because it turnes it into an 32-bit rgb image instead of the
         * 8-buit grey scale stored.  This converts it back.
         * */
        private Bitmap convertBitmap(Bitmap bmp)
        {


            Rectangle cloneRect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            return bmp.Clone(cloneRect, PixelFormat.Format8bppIndexed);

        }
      

        /* Given a bitmap now that only has a single blob (the hand)
         * crop the bitmap tightly to this.
         * */
        private Bitmap cropToHand(Bitmap bitmap, ref Coord[] locations)
        { 
            Bitmap cropped;
            Rectangle boundingBox;


            try
            {
                //Try to find the bounds of the hand.
                boundingBox = findBounds(locations);
            }
            catch (NullReferenceException nre)
            {
                //If this errors, use the whole from.  Essentially a 100% crop.
                boundingBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            }

            /* It's possible that the hand is too close to a frame edge.
             * Check for this and set  a 100% crop if so.
             * */
            if( ((boundingBox.X+boundingBox.Width) > bitmap.Width) || ((boundingBox.Y+boundingBox.Height) > bitmap.Height) ){
                boundingBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            }

            //Do the crop.
            cropped = bitmap.Clone(boundingBox,bitmap.PixelFormat);

            //clean up.
            bitmap.Dispose();

            return cropped;
        }

        /* Because we know that the locations were scaned in from the uppermost
         * pixel to the lower most pixel, we can use these locations alone to 
         * determine the locations of the hand in a full frame.
         * */
        private Rectangle findBounds(Coord[] locations)
        {

            Coord lastCoord = locations[locations.Length-1];
            Coord firstCoord = locations[0];

            //initilize the values such that they don't initially fail the comparisons.
            int xHigh = 0;
            int xLow = 1000;
            int yHigh = 0;
            int yLow = 1000;
            int minWidth = 10;
            int minHeight = 10;
            int maxWidth = 250;
            int maxHeight = 250;

            Coord start;


            /* Iterate through all the locations finding the high and low vaules for X and Y.
             * We are loooking for the extrememes of the hand.
             * */
            foreach (Coord c in locations)
            {
                if (c.getX() > xHigh) xHigh = c.getX();
                if (c.getX() < xLow) xLow = c.getX();

                if (c.getY() > yHigh) yHigh = c.getY();
                if (c.getY() < yLow) yLow = c.getY();

            }

            //Initilize a coord at the very end.
            start = new Coord(-1, xLow, yLow);


            /* Since we now have the high and low values, we also know 
             * the size of the bounding box.
             * */
            int width = xHigh-xLow;
            int height = yHigh-yLow;



           
            //Now check various conditions to find the correct crop.
            if (height < 0 || width < 0) //Image data was bad, so return default values.
            {
   
                width = minWidth;
                height = minHeight;


                return new Rectangle(0, 0, width, height);
            }

            if (start.getX() < 0 || start.getY() < 0) //Image data was bad, so return default values.
            {
               
                width = minWidth;
                height = minHeight;

                return new Rectangle(0, 0, width, height);
            }

            if ((width < minWidth) || (height < minHeight)) //Crop would be smaller than the minimum tolerance.  Error state.
            {

                width = minWidth;
                height = minHeight;

               
                return new Rectangle(0, 0, width, height);

            }

            if ((width > maxWidth) || (height > maxHeight)) //Crop is larger than the original image, return default.
            {
                width = maxWidth;
                height = maxHeight;

              
                return new Rectangle(0, 0, width, height);
            }

            if ((start.getX() + width > maxWidth) || (start.getY() + height > maxHeight)) //Landscape crop
            {
                width = maxWidth;
                height = maxHeight;

            }

            if ((start.getX() + width < minWidth) || (start.getY() + height < minHeight)) //portrait crop.
            {
                width = maxWidth;
                height = maxHeight;

            }



            return new Rectangle(start.getX(), start.getY(), width, height);
        }


        //used to actually resize the bitmap to match the reference image.
        private static Bitmap resizeBitmap(Bitmap bitmap, int width, int height)
        {
            ResizeBilinear  resizer = new ResizeBilinear(width, height);

            Bitmap result = resizer.Apply(bitmap);
            return result;

        }

    }
    
}
