﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MetroFramework;
using MetroFramework.Forms;
using System.Threading;

using OpenCvSharp;

namespace Assignment5
{
    public partial class MotionDetectionForm : MetroForm
    {
        
        private Thread frameRunner;
        private VideoCapture camera;

        private Mat currentFrame;
        private Mat posterizedFrame;
        private Mat previousFrame;
        private Mat resultFrame;

        private int userSensitivity = 960;     //min: 640 - default: 960 - max: 1280

        private bool stopFrameFlag = false;

        private int gcInterval = 30;

        public MotionDetectionForm()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            lbl_motiondetection.Hide();
            lbl_sensitivity.Text = "50";               
        }
        
        private void RunFrame()
        {
            CheckForIllegalCrossThreadCalls = false;

            var gcCounter = 0;
            camera = new VideoCapture(0);

            camera.FrameWidth = picb_motiondetection.Width;
            camera.FrameHeight = picb_motiondetection.Height;
            
            while (!stopFrameFlag)
            {
                camera.Read(currentFrame);
                Cv2.CvtColor(currentFrame, posterizedFrame, ColorConversionCodes.BGR2GRAY);
                EightLevelPosterizing();

                resultFrame = posterizedFrame - previousFrame;

                lbl_motiondetection.Visible = DetermineMotionState();
                
                picb_motiondetection.ImageIpl = resultFrame;
                posterizedFrame.CopyTo(previousFrame);

                if (gcCounter == gcInterval)
                {
                    GC.Collect();
                    gcCounter = 0;
                }

                ++gcCounter;
                Cv2.WaitKey(20);
                   
            }
        }

        private void EightLevelPosterizing()
        {
            var indexer = posterizedFrame.GetGenericIndexer<byte>();

            for (int i = 0; i < posterizedFrame.Height; ++i)
                for (int j = 0; j < posterizedFrame.Width; ++j)
                {
                    var pixelVal = indexer[i, j];
                    if (0 <= pixelVal && pixelVal < 32)
                        indexer[i, j] = 0;
                    else if (32 <= pixelVal && pixelVal < 64)
                        indexer[i, j] = 32;
                    else if (64 <= pixelVal && pixelVal < 96)
                        indexer[i, j] = 64;
                    else if (96 <= pixelVal && pixelVal < 128)
                        indexer[i, j] = 96;
                    else if (128 <= pixelVal && pixelVal < 160)
                        indexer[i, j] = 128;
                    else if (160 <= pixelVal && pixelVal < 192)
                        indexer[i, j] = 160;
                    else if (224 <= pixelVal && pixelVal <= 255)
                        indexer[i, j] = 224;             
                }            
        }

        private bool DetermineMotionState()
        {
            var indexer = resultFrame.GetGenericIndexer<byte>();
            var determiningPoint = 0;
            
            for(int i = 0; i < resultFrame.Height; ++i)
            {
                for(int j = 0; j < resultFrame.Width; ++j)
                {
                    if (indexer[i, j] > 32)
                        ++determiningPoint;
                    else
                        indexer[i, j] = 0;
                }
            }

            if (determiningPoint >= userSensitivity)
                return true;
            else
                return false;
   
        }

        private void OnShown(object sender, EventArgs e)
        {
            currentFrame = new Mat();
            previousFrame = new Mat();
            posterizedFrame = new Mat();
            resultFrame = new Mat();
            frameRunner = new Thread(new ThreadStart(delegate () { RunFrame(); }));
            frameRunner.Start();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            stopFrameFlag = true;
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            camera.Dispose();
        }

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            userSensitivity = 640 + (int)(640 * ((float)e.NewValue / 100.0f));
            lbl_sensitivity.Text = e.NewValue.ToString();
        }
    }
}
