using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KwisCapture
{
    class CaptureHand
    {
        private UtilMPipeline pp;
        private int color;
        PXCMGesture gesture;
        PXCMGesture.Blob bdata;
        PXCMImage bimage;
        PXCMImage.ImageData data;
        PXCMImage.ImageInfo info;
        Bitmap bitmap;

        public CaptureHand(CaptureForm form): base(){
            pp = new UtilMPipeline();
            pp.EnableGesture();
            pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);
 
           

            if (pp.Init())
            {


                pp.QueryCapture().SetFilter(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, 100);
                do
                {
                    if (!pp.AcquireFrame(true))
                    {
                        MessageBox.Show("Failed to aquire a frame.", "Kwi-S", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        break;
                    }
                   
                    gesture =  pp.QueryGesture();

                    
                    gesture.QueryBlobData(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bdata);
                    
                    gesture.QueryBlobImage(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out bimage);

                    
                    

                    bimage.AcquireAccess(PXCMImage.Access.ACCESS_READ,out  data);
                    info = bimage.imageInfo;


                    color =(int) bdata.labelLeftHand;


                    form.setTextBox(color.ToString());
                    

                    pp.ReleaseFrame();
                    
                  

                } while (color == -1);

                Coord[] locations = handLocation(data, (int)(info.width * info.height),(int) info.width);
                bitmap = createBitmap(locations, (int)info.width, (int)info.height);
                form.setImage(bitmap);
                form.bitmap = bitmap;

               
            }
        }

        private Bitmap processBitmap(Bitmap bitmap)
        {
          

            
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite,bitmap.PixelFormat);


            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * bitmap.Height;

            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i=0; i < bytes; i++)
            {
               //if (rgbValues[i] != 0) rgbValues[i] = Convert.ToByte(255);
                
            }

            Marshal.Copy(rgbValues, 0, ptr, bytes);

            bitmap.UnlockBits(bmpData);
            
            return bitmap;
            
        }

        public Coord[] handLocation(PXCMImage.ImageData data,int size, int width)
        {

            Coord[] locations = new Coord[size];

            
            IntPtr ptr = data.buffer.planes[0];
            byte[] rgbValues = new byte[size];


            Marshal.Copy(ptr, rgbValues, 0, size);

            int counter=0;
            int i;
            for (i = 0; i < size; i++)
            {
                if (rgbValues[i] == 0)
                {
                    locations[counter++] = new Coord(i,width);
                }
            }
            //locations[counter] = -1;
            
            Array.Resize(ref locations, counter - 1);
            
            return locations;
           
        }

        private Bitmap createBitmap(Coord[] locations, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);


            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * bitmap.Height;

            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);


            for (int i = 0; i<locations.Length; i++)
            {
               
                rgbValues[locations[i].getLoc()] = 255;

            }

            Marshal.Copy(rgbValues, 0, ptr, bytes);

            bitmap.UnlockBits(bmpData);

            //return bitmap;
            return cropToHand(bitmap,locations);
        }

        private Bitmap cropToHand(Bitmap bitmap,Coord[] locations)
        {
           
            Rectangle boundingBox = findBounds(locations);

            Bitmap cropped = bitmap.Clone(boundingBox,bitmap.PixelFormat);

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

            return new Rectangle(start.getX(), start.getY(), width, height);
        }

    }



}
