﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KwisStandalone
{
    public partial class Form1 : Form
    {

        public volatile bool closing = false;

        public Form1()
        {
            InitializeComponent();
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);
            this.WindowState = FormWindowState.Minimized;
            

            System.Threading.Thread thread = new System.Threading.Thread(DoRecognition);

            thread.Start();
            System.Threading.Thread.Sleep(5);
            
        }
       
        

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            closing = true;
        }

        private void DoRecognition()
        {
            GesturePipeline gr = new GesturePipeline(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

   
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            closing = true;
            Application.Exit();
        }



    }
}
