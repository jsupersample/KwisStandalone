using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KwisStandalone
{
    class Coord
    {
        private int x;
        private int y;
        private int loc;

        public Coord(int loc, int width)
            : base()
        {
            this.loc = loc;
            x = loc % width;
            y = loc / width;
        }

        public Coord(int loc, int x, int y)
        {
            this.loc = loc;
            this.x = x;
            this.y = y;

        }

        public int getLoc()
        {
            return loc;
        }

        public int getX()
        {
            return x;
        }

        public int getY()
        {
            return y;
        }


       
    }
}
