using AForge.Imaging;
using AForge.Imaging.Filters;
using KwisStandalone.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;




namespace KwisStandalone
{
    class GesturePipeline
    {


        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        extern static IntPtr GetDesktopWindow();


        private const int sleep = 1300;

        private static int APPCOMMAND_MEDIA_REWIND = 0x320000;
        private const int WM_APPCOMMAND = 0x319;


        ExhaustiveTemplateMatching templateMatching;



        public GesturePipeline(Form1 form)
            : base()
        {

            UtilMPipeline pp;
            
            PXCMGesture.Blob bdata;
            PXCMImage bimage;
            PXCMImage.ImageData data;
            PXCMImage.ImageInfo info;
            int color;
            
        

            Coord[] locations;
            pp = new UtilMPipeline();
            pp.EnableGesture();
            pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);

            IntPtr handle = GetDesktopWindow();

            Bitmap pause = convertBitmap(KwisStandalone.Properties.Resources.STOP);
            Bitmap fastForward = convertBitmap(KwisStandalone.Properties.Resources.FAST_FORWARD);
            Bitmap rewind = convertBitmap(KwisStandalone.Properties.Resources.REWIND);
            Bitmap play = convertBitmap(KwisStandalone.Properties.Resources.PLAY);
            Bitmap menu = convertBitmap(KwisStandalone.Properties.Resources.MENU);
            Bitmap volUp = convertBitmap(KwisStandalone.Properties.Resources.VOL_UP);
            Bitmap volDown = convertBitmap(KwisStandalone.Properties.Resources.VOL_DOWN);


            templateMatching = new ExhaustiveTemplateMatching(0.75f);

            if (pp.Init())
            {


                while (!form.closing)
                {
                    if (!pp.AcquireFrame(true))
                    {
                        MessageBox.Show("Failed to aquire a frame.", "Kwi-S", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                        break;
                    }
                    
                    using (PXCMGesture gesture = pp.QueryGesture())
                    {
                        

                        gesture.QueryBlobData(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bdata);
                        
                        gesture.QueryBlobImage(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bimage);

                        bimage.AcquireAccess(PXCMImage.Access.ACCESS_READ, out  data);

                       

                       
                        
                        info = bimage.imageInfo;


                        color = (int)bdata.labelLeftHand;

                        locations = new Coord[info.width * info.height];



                        handLocation(ref locations, data, (int)info.width);
                        Bitmap bitmap = createBitmap(ref locations, (int)info.width, (int)info.height);


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

                        bitmap.Dispose();
                       
                        


                        bimage.ReleaseAccess(ref data);
                        bimage.Dispose();
                        gesture.Dispose();
                    }


                    
                    
                    pp.ReleaseFrame();
                }
            }

        }

        private bool compare(ref Bitmap reference, ref Bitmap target)
        {

            if (templateMatching.ProcessImage(reference, resizeBitmap(target, reference.Width, reference.Height)).Length > 0) return true;
            else return false;


        }

        public void clickAndSendKey(String key)
        {
            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("ehshell");
            if (p.Length > 0) //found
            {
                SetForegroundWindow(p[0].MainWindowHandle);
            }

            SendKeys.SendWait(key);

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


        public void handLocation( ref Coord[] locations,PXCMImage.ImageData data, int width)
        {


            IntPtr ptr = data.buffer.planes[0];
            byte[] rgbValues = new byte[locations.Length];


            Marshal.Copy(ptr, rgbValues, 0, locations.Length);

            int counter=0;
            int i;



            for (i = 0; i < locations.Length; i++)
            {
                if (rgbValues[i] == 0 )
                {
                    locations[counter++] = new Coord(i,width);

                }
            }
           
            
            if(counter >1) Array.Resize(ref locations, counter - 1);

            return;
           
        }

        private Bitmap createBitmap(ref Coord[] locations, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);


            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * bitmap.Height;

            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = 0; i < locations.Length; i++)
            {

                try
                {

                    rgbValues[locations[i].getLoc()] = 255;
                }
                catch (NullReferenceException nre)
                {
                    break;
                }

            }
            
            Marshal.Copy(rgbValues, 0, ptr, bytes);

            bitmap.UnlockBits(bmpData);

            //return bitmap;
            
            

            return cropToHand(bitmap, ref locations);
        }

        private Bitmap convertBitmap(Bitmap bmp)
        {


            Rectangle cloneRect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            return bmp.Clone(cloneRect, PixelFormat.Format8bppIndexed);

        }
      

        private Bitmap cropToHand(Bitmap bitmap, ref Coord[] locations)
        { 
            Bitmap cropped;
            Rectangle boundingBox;


            try
            {
                boundingBox = findBounds(locations);
            }
            catch (NullReferenceException nre)
            {
                boundingBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            }

            if( ((boundingBox.X+boundingBox.Width) > bitmap.Width) || ((boundingBox.Y+boundingBox.Height) > bitmap.Height) ){
                boundingBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            }

            cropped = bitmap.Clone(boundingBox,bitmap.PixelFormat);

            bitmap.Dispose();

            return cropped;
        }

        private Rectangle findBounds(Coord[] locations)
        {
            Coord lastCoord = locations[locations.Length-1];
            Coord firstCoord = locations[0];

            int xHigh = 0;
            int xLow = 1000;
            int yHigh = 0;
            int yLow = 1000;
            int minWidth = 10;
            int minHeight = 10;
            int maxWidth = 250;
            int maxHeight = 250;

            Coord start;


            foreach (Coord c in locations)
            {
                if (c.getX() > xHigh) xHigh = c.getX();
                if (c.getX() < xLow) xLow = c.getX();

                if (c.getY() > yHigh) yHigh = c.getY();
                if (c.getY() < yLow) yLow = c.getY();

            }

            start = new Coord(-1, xLow, yLow);


            int width = xHigh-xLow;
            int height = yHigh-yLow;



           

            if (height < 0 || width < 0)
            {
                
               
                width = minWidth;
                height = minHeight;

               

                return new Rectangle(0, 0, width, height);
            }

            if (start.getX() < 0 || start.getY() < 0)
            {
               
                width = minWidth;
                height = minHeight;

               // Debug.WriteLine("X/Y <0");
                return new Rectangle(0, 0, width, height);
            }

            if ((width < minWidth) || (height < minHeight))
            {

                width = minWidth;
                height = minHeight;

               
                return new Rectangle(0, 0, width, height);

            }

            if ((width > maxWidth) || (height > maxHeight))
            {
                width = maxWidth;
                height = maxHeight;

              
                return new Rectangle(0, 0, width, height);
            }

            if ((start.getX() + width > maxWidth) || (start.getY() + height > maxHeight))
            {
                width = maxWidth;
                height = maxHeight;

            }

            if ((start.getX() + width < minWidth) || (start.getY() + height < minHeight))
            {
                width = maxWidth;
                height = maxHeight;

            }



            return new Rectangle(start.getX(), start.getY(), width, height);
        }

        private static Bitmap resizeBitmap(Bitmap bitmap, int width, int height)
        {
            ResizeBilinear  resizer = new ResizeBilinear(width, height);
      


            Bitmap result = resizer.Apply(bitmap);

            
           

            return result;

        }

    }
    
}
