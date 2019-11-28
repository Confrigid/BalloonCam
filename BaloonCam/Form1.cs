using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using DirectShowLib;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace BalloonCam
{
    public partial class Form1 : Form
    {
        VideoCapture capture = new VideoCapture();
        string cheerPath, subPath, donatePath;
        bool isConnected = false;
        String[] ports;
        SerialPort port;
        bool algoEnabler = false;
        List<int> inflationCollection = new List<int>();
        List<int> donationCollection = new List<int>();
        decimal cheerAccumulator;
        int selectedWebcam = 0;

        //Create's webcam list, adds webcams to list, creates webcam combobox, sets serial ports to combobox.
        public Form1()
        {
            InitializeComponent();
            //Create webcam List
            List<KeyValuePair<int, string>> ListCamerasData = new List<KeyValuePair<int, string>>();
            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            //Get com ports for arduino
            getAvailableComPorts();

            //Add all webcams to List
            int _DeviceIndex = 0;
            foreach (DirectShowLib.DsDevice _Camera in _SystemCameras)
            {
                ListCamerasData.Add(new KeyValuePair<int, string>(_DeviceIndex, _Camera.Name));
                _DeviceIndex++;
            }
            //Clear the combobox and textBox1
            textBox1.Text = null;
            ComboBoxCameraList.DataSource = null;
            ComboBoxCameraList.Items.Clear();

            //Bind the combobox
            ComboBoxCameraList.DataSource = new BindingSource(ListCamerasData, null);
            ComboBoxCameraList.DisplayMember = "Value";
            ComboBoxCameraList.ValueMember = "Key";

            //Set available ports to combobox
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
                Console.WriteLine(port);
                if (ports[0] != null)
                {
                    comboBox1.SelectedItem = ports[0];
                }
            }
            disableControls();
            //backgroundWorker1.RunWorkerAsync();
        }

        //Get Com Ports for listing.
        void getAvailableComPorts()
        {
            ports = SerialPort.GetPortNames();
        }

        //EMGU Open CV code
        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                //Circle Detection *commented out*
                #region Circle Detection
                //Mat m = new Mat();
                //capture.Retrieve(m);

                //UMat uimage = new UMat();
                //CvInvoke.CvtColor(m, uimage, ColorConversion.Bgr2Gray);

                ////use image pyr to remove noise
                //UMat pyrDown = new UMat();
                //CvInvoke.PyrDown(uimage, pyrDown);
                //CvInvoke.PyrUp(pyrDown, uimage);

                //double cannyThreshold = 180.0;
                //double circleAccumulatorThreshold = 120;
                //CircleF[] circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 20.0, cannyThreshold, circleAccumulatorThreshold, 5);

                //foreach (CircleF circle in circles)
                //    CvInvoke.Circle(m, Point.Round(circle.Center), (int)circle.Radius, new Bgr(Color.Red).MCvScalar, 2);

                //imageBox1.Image = m;
                #endregion
                //Red Detection *WIP*
                #region Red Detection
                Mat m = new Mat();
                capture.Retrieve(m);

                Mat bgr_inv = m;

                Mat hsv_inv = new Mat();

                CvInvoke.CvtColor(bgr_inv, hsv_inv, ColorConversion.Bgr2Hsv);

                Mat mask1 = new Mat();
                Mat mask2 = new Mat();
                MCvScalar scalarOne = new MCvScalar(0, 70, 50);
                MCvScalar scalarTwo = new MCvScalar(10, 255, 255);
                MCvScalar scalarThree = new MCvScalar(170, 70, 50);
                MCvScalar scalarFour = new MCvScalar(180, 255, 255);

                ScalarArray scA = new ScalarArray(scalarOne);
                ScalarArray scB = new ScalarArray(scalarTwo);
                ScalarArray scC = new ScalarArray(scalarThree);
                ScalarArray scD = new ScalarArray(scalarFour);

                CvInvoke.InRange(hsv_inv, scA, scB, mask1);
                CvInvoke.InRange(hsv_inv, scC, scD, mask2);

                Mat mask = new Mat();
                CvInvoke.BitwiseOr(mask1, mask2, mask);
                imageBox1.Image = mask;
                //If there are more than 500 non-zero pixels in the image, this fires.
                if (CvInvoke.CountNonZero(mask) > 500)
                {
                    //Code to set operation start flag.
                    algoEnabler = true;
                }
                //If the image has 0 nonzero pixels, this fires.
                else if(CvInvoke.CountNonZero(mask) == 0)
                {
                    //Code to disable operation
                    algoEnabler = false;
                }
                #endregion

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Start webcam capture
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                capture = new VideoCapture(selectedWebcam);
            }
            capture.ImageGrabbed += Capture_ImageGrabbed;
            capture.Start();
        }

        //Stop webcam capture
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(capture != null)
            {
                capture.Stop();
            }
        }

        //Pause webcam capture
        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(capture!=null)
            {
                capture.Pause();
            }
        }

        //Select StreamLabels Directory
        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChooseFolder();
            #region Stream Labels text file reader
            if (textBox1.Text != null)
            {
                fileSystemWatcher1.Path = textBox1.Text;
                fileSystemWatcher2.Path = textBox1.Text;
                fileSystemWatcher3.Path = textBox1.Text;

                fileSystemWatcher1.Filter = "session_most_recent_cheerer.txt";
                fileSystemWatcher2.Filter = "session_most_recent_subscriber.txt";
                fileSystemWatcher3.Filter = "session_most_recent_donator.txt";

                cheerPath = textBox1.Text + @"\" + fileSystemWatcher1.Filter;
                subPath = textBox1.Text + @"\" + fileSystemWatcher2.Filter;
                donatePath = textBox1.Text + @"\" + fileSystemWatcher3.Filter;

                fileSystemWatcher1.Changed += new FileSystemEventHandler(cheerChanged);
                fileSystemWatcher2.Changed += new FileSystemEventHandler(subChanged);
                fileSystemWatcher3.Changed += new FileSystemEventHandler(donateChanged);

                fileSystemWatcher1.EnableRaisingEvents = true;
                fileSystemWatcher2.EnableRaisingEvents = true;
                fileSystemWatcher3.EnableRaisingEvents = true;
            }
            #endregion
        }

        //Choose where streamlabels folder on user's PC.
        public void ChooseFolder()
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        //Events that fire when the text file changes.
        #region Donate/Sub/Cheer Changed events
        private void cheerChanged(object source, FileSystemEventArgs e)
        {
            FileStream cheererFileStream = new FileStream(textBox1.Text + @"\session_most_recent_cheerer.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader cheererFileReader = new StreamReader(cheererFileStream);

            while (!cheererFileReader.EndOfStream)
            {
                string cheerInput = cheererFileReader.ReadLine();
                if (cheerInput != null)
                {
                    decimal cheerOutput = decimal.Parse(cheerInput.Substring(cheerInput.IndexOf(':') + 1));
                    if(cheerOutput < 100)
                    {
                        cheerAccumulator += cheerOutput;
                        if (cheerAccumulator > 100)
                        {
                            inflationCollection.Add(1);
                            cheerAccumulator = 0;
                        }
                    }
                    else
                    {
                        if(Math.Ceiling(cheerOutput/100) > 15)
                        {
                            inflationCollection.Add(15);
                        }
                        else
                        {
                            inflationCollection.Add((int)Math.Ceiling(cheerOutput / 100));
                        }
                    }
                }
            }
            cheererFileStream.Close();
            cheererFileReader.Close();
        }

        private void subChanged(object source, FileSystemEventArgs e)
        {
            FileStream subscriberFileStream = new FileStream(textBox1.Text + @"\session_most_recent_subscriber.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader subscriberFileReader = new StreamReader(subscriberFileStream);

            while (!subscriberFileReader.EndOfStream)
            {
                string subInput = subscriberFileReader.ReadLine();
                if (subInput != null)
                {
                    inflationCollection.Add(5);
                }
            }
            subscriberFileStream.Close();
            subscriberFileReader.Close();
        }

        private void donateChanged(object source, FileSystemEventArgs e)
        {
            FileStream donatorFileStream = new FileStream(textBox1.Text + @"\session_most_recent_donator.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader donatorFileReader = new StreamReader(donatorFileStream);

            while (!donatorFileReader.EndOfStream)
            {
                string donateInput = donatorFileReader.ReadLine();
                if (donateInput != null)
                {
                    string donateOutput = donateInput.Substring(donateInput.IndexOf('$') + 1);
                    decimal donationAmount = decimal.Parse(donateOutput);
                    int donationFinal = (int)donationAmount;
                    latestDonator.Text = donationFinal.ToString();
                    if(donationFinal >= 15)
                    {
                        inflationCollection.Add(15);
                    }
                    else if(donationFinal < 1)
                    {
                        inflationCollection.Add(1);
                    }
                    else
                    {
                        inflationCollection.Add(donationFinal);
                    }
                }
            }
            donatorFileStream.Close();
            donatorFileReader.Close();
        }
        #endregion

        //Button to connect and disconnect from the arduino
        private void button1_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                connectToArduino();
            }
            else
            {
                disconnectFromArduino();
            }
        }

        //Method to connect to the arduino
        private void connectToArduino()
        {
            isConnected = true;
            try
            {
                string selectedPort = comboBox1.GetItemText(comboBox1.SelectedItem);
                port = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
                port.Open();
                port.Write("#STAR\n");
                button1.Text = "Disconnect";
                enableControls();
            }
            
            catch(Exception)
            {
                MessageBox.Show("Arduino must be selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Method to disconnect from arduino
        private void disconnectFromArduino()
        {
            try
            {
                isConnected = false;
                port.Write("#STOP\n");
                port.Close();
                button1.Text = "Connect";
                disableControls();
            }

            catch (Exception)
            {
                MessageBox.Show("Arduino must be selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //1, 5, and 10 second test buttons.
        #region Solenoid Test Buttons
        private void button2_Click(object sender, EventArgs e)
        {
            port.Write("#SOLDON01\n");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            port.Write("#SOLDON05\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            port.Write("#SOLDON10\n");
        }

        
        #endregion

        //Enable and disable Solenoid Test Buttons when connected/disconnected from arduino.
        #region Enable/Disable Controls
        private void enableControls()
        {
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        private void ComboBoxCameraList_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<int, string> SelectedItem = (KeyValuePair<int, string>)ComboBoxCameraList.SelectedItem;
            selectedWebcam = SelectedItem.Key;
        }

        private void disableControls()
        {
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
        }
        #endregion

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (algoEnabler == true && isConnected == true)
            {
                if (inflationCollection[0] < 10)
                {
                    port.Write("#SOLDON0" + inflationCollection[0] + "\n");
                    Thread.Sleep((inflationCollection[0] + 5) * 1000);
                    inflationCollection.RemoveAt(0);
                }
                else
                {
                    port.Write("#SOLDON" + inflationCollection[0] + "\n");
                    Thread.Sleep((inflationCollection[0] + 5) * 1000);
                    inflationCollection.RemoveAt(0);
                }

            }
        }
    }
}
