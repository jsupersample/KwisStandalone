using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KwisCapture
{
    public partial class CaptureForm : Form
    {
        public volatile bool closing = false;
        public volatile Bitmap bitmap;

        public CaptureForm()
        {
            InitializeComponent();
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);
        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            closing = true;
        }

        private void DoRecognition()
        {
            CaptureHand capture = new CaptureHand(this);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Threading.Thread thread = new System.Threading.Thread(DoRecognition);
            thread.Start();
            System.Threading.Thread.Sleep(5);
        }

        private void captureButton_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        public void  setImage(Image image){
            pictureBox1.Image = image;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            bitmap.Save("out.bmp");
   
        }

        public void setTextBox(string text)
        {
            textBox1.Text = text;
        }

    }
}
