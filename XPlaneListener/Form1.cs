using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XPlaneListener
{
    public partial class Form1 : Form
    {
        private Listener listener;
        public Form1()
        {
            InitializeComponent();
            SetupListener();
        }

        private void SetupListener()
        {
            byte byte1 = Convert.ToByte(maskedTextBox1.Text);
            byte byte2 = Convert.ToByte(maskedTextBox2.Text);
            byte byte3 = Convert.ToByte(maskedTextBox3.Text);
            byte byte4 = Convert.ToByte(maskedTextBox4.Text);
            int port = int.Parse(maskedTextBox5.Text);
            byte [] address = new byte []{byte1, byte2, byte3, byte4};
            IPAddress ipAddress = new IPAddress(address);
            listener = new Listener(ipAddress, port, fileNameTextBox.Text);
            listener.DataReceived += listener_DataReceived;
            listener.LogReceived += listener_LogReceived;

            if (listener == null)
            {
                button1.Enabled = false;
            }
        }

        private void listener_LogReceived(object sender, LogEventArgs e)
        {
            if (e.NewLine)
            {
                System.Threading.Thread.Sleep(100);
                logTextBox.Text = e.Message;
            }
            else
            {
                logTextBox.AppendText(e.Message);
            }
            Application.DoEvents();
        }

        private void listener_DataReceived(object sender, DataReceivedEventArgs e)
        {
            byte[] data = e.Data;
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:000},", b);
            }
            rawTextBox.Text = sb.ToString();
            listener.AppendToFile(fileNameTextBox.Text, rawTextBox.Text);
            Application.DoEvents();
            InterpretData(data, xPlaneRadioButton.Checked);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            this.listener.Listen();            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.listener.Cancel();
            button1.Enabled = true;
        }
        //  http://www.jefflewis.net/XPlaneUDP_9.html
//        Breaking Down an X-Plane UDP Packet
// Now, let's take a look at what a UDP packet being sent from X-Plane looks like. 
        //This is another place where I got a little lost looking at the information on x-plane.info. But once I figured out everything was in bytes, it made a lot more sense. A UDP packet contains a header with some network information, but Visual Basic does not import that into the program when we use the .GetData command. So, we only see the body part of the packet. When I talk about packets in Visual Basic, that is what I'm referring to. A typical DATA packet being sent out from X-Plane may look something like: 

//       68 65 84 65 38 0 0 0 37 68 151 111 166 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0  

        //So what does all this mean? The first five bytes are what X-Plane uses for its header. 
        //Each of these bytes are actually ASCII codes, so we convert each of them into a symbol. 
        //The first 4 bytes of the header tell us what type of packet it is. In this example, they're 68,65,84,65, which correspond to D,A,T,A, 
        //respectively, so we know this is a DATA packet. The fifth byte in the header, 38 in this example, is an index used internally by X-Plane 
        //that we don't really need to worry about (I'm not exactly sure what it does, to tell the truth). When creating a data packet to send back to X-Plane, 
        //I just set this value to 48, the ASCII code for "0." 

//Now comes a group of 36 bytes. This is the data segment. The first 4 bytes are the index, and the next 32 bytes are the data for that index. 
        //The best explanation of what each index is, and what data are sent on that index, is to simply look at the Data Input & Output screen in X-Plane. 
        //The first 4 bytes are the index, as an integer. Remember that PCs and Macs reverse the order of the bytes. 
        //So, on a PC, look at the first of the 4 bytes, and on a Mac, look at the fourth of the 4 bytes. Whatever the byte is is the index number. 
        //In our example above the byte is 37, which means index 37, which is engine rpm. 

//Now there are 32 bytes left in this data segment. This is 8 groups of 4, or 8 single precision floating point numbers. 
        //You convert them in the manner as described above. The first number in our example is the four bytes 68, 151, 111, 166, or 1211.489. 
        //The remaining 7 data points in this example are all zero. 

//A DATA packet can end there, as in the above example, or it could be followed by any number of 32 byte data segments, 
        //which you treat the same was as described above. From version 8 on, there is no special symbol to designate the end of a packet. 
        //If you want to be able to handle an arbitrary number of data segments, you'll just have to count how long the UDP packet is,
        //and calculate the number of data segments from that (NumberChannels = (bytesTotal - 5) / 36). 

        private void InterpretData(byte[] data, bool isXPlane)
        {
            int index = 5; // the sixth byte in the array
            if (isXPlane)
            {
                // Get first FIVE bytes
                string header = string.Format("{0}{1}{2}{3}{4}"
                    , Convert.ToChar(data[0])
                    , Convert.ToChar(data[1])
                    , Convert.ToChar(data[2])
                    , Convert.ToChar(data[3])
                    , Convert.ToChar(data[4]));

                if (!header.Equals("DATA0")) // is the last byte variable?
                {
                    logTextBox.Text = "Invalid header";
                }

                while (index < data.Length)
                {
                    SetUserInterface(ref index, data);
                }
            }
            else
            {
                index = 0;
                ProcessRollPitchHeading(ref index, data, false);
                ProcessLatLon(ref index, data, false);
                ProcessLocVel(ref index, data, false);

            }
        }

        private const int PITCH_ROLL_HEADING = 18;
        private const int LAT_LON_ALTITUDE = 20;
        private const int LOC_VEL_DIST = 21;

        private void SetUserInterface(ref int index, byte[] data)
        {
            if (index > data.Length-4)
            {
                index++;
                return;
            }
            // Get the next 4 bytes
            //So, on a PC, look at the first of the 4 bytes 
            string fieldIndexString = string.Format("{3}{2}{1}{0}", data[index++], data[index++], data[index++], data[index++]);
            int fieldValue = 0;

            // Data is dodgy  
            if (!int.TryParse(fieldIndexString, out fieldValue))
            {
                return;
            }

            bool reverse = false;
            switch(fieldValue)
            { 
                case PITCH_ROLL_HEADING:
                    //
                    // Pitch in Degrees is single - 4 bytes
                    // roll in degrees
                    // Heading true
                    // Heading magnetic'
                    // Index = 9

                    ProcessRollPitchHeading(ref index, data, reverse);
                    // End it here for now
                    //index = data.Length;
                    break;

                case LAT_LON_ALTITUDE:
                    //

                    ProcessLatLon(ref index, data, reverse);
                    break;

                case LOC_VEL_DIST:
                    ProcessLocVel(ref index, data, reverse);
                    break;

                default:
                    //throw new NotImplementedException();
                    index++;
                    break;

            }
        }

        private void ProcessLocVel(ref int index, byte[] data, bool reverse)
        {
            float xm = ConvertToSingle(data, ref index, reverse);
            float ym = ConvertToSingle(data, ref index, reverse);
            float zm = ConvertToSingle(data, ref index, reverse);
            float vXms = ConvertToSingle(data, ref index, reverse);
            float vYms = ConvertToSingle(data, ref index, reverse);
            float vZms = ConvertToSingle(data, ref index, reverse);
            float distFt = ConvertToSingle(data, ref index, reverse);
            float distNm = ConvertToSingle(data, ref index, reverse);

            groundSpeedTextBox.Text = GetGroundSpeed(vXms, vYms, vZms);
       
        }

        private void ProcessLatLon(ref int index, byte[] data, bool reverse)
        {
            float latitudeDegrees = ConvertToSingle(data, ref index, reverse);
            float longitudeDegrees = ConvertToSingle(data, ref index, reverse);
            float heightMSL = ConvertToSingle(data, ref index, reverse);
            float heightAGL = ConvertToSingle(data, ref index, reverse);
            float onrunwy = ConvertToSingle(data, ref index, reverse);
            float altind = ConvertToSingle(data, ref index, reverse);
            float latsouth = ConvertToSingle(data, ref index, reverse);
            float lonwest = ConvertToSingle(data, ref index, reverse);
            latitudeTextBox.Text = string.Format("{0:f} degrees", latitudeDegrees);
            longitudeTextBox.Text = string.Format("{0:f} degrees", longitudeDegrees);
            amslTextBox.Text = string.Format("{0:f} Ft", heightMSL);
            aglTextBox.Text = string.Format("{0:f} Ft", heightAGL);

        }

        private void ProcessRollPitchHeading(ref int index, byte[] data, bool reverse)
        {
            float pitchDegrees = ConvertToSingle(data, ref index, reverse);
            float rollDegrees = ConvertToSingle(data, ref index, reverse);
            float headingTrueDegrees = ConvertToSingle(data, ref index, reverse);
            float headingMagDegrees = ConvertToSingle(data, ref index, reverse);
            float headingCompDegrees = ConvertToSingle(data, ref index, reverse);
            float magVar = ConvertToSingle(data, ref index, reverse);
            float blank1 = ConvertToSingle(data, ref index, reverse);
            float blank2 = ConvertToSingle(data, ref index, reverse);
            pitchTextBox.Text = string.Format("{0:f} degrees", pitchDegrees);
            rollTextBox.Text = string.Format("{0:f} degrees", rollDegrees);
            headingTextBox.Text = string.Format("{0:000} degrees T, {1:000} degrees M", headingTrueDegrees, headingMagDegrees);
        }

        private string GetGroundSpeed(float vXms, float vYms, float vZms)
        {
            double sumSquared = (vXms * vXms) + (vYms * vYms) + (vZms * vZms);
            double scalar = Math.Sqrt(sumSquared); // 100 Km/h= 27.777 m/s = 50.2777 Nm/h
            double knots = scalar * (50.2777f / 27.777f);
            return string.Format("{0:000.00} M/s {1:000.00} knots", scalar, knots);
        }
        /// <summary>
        /// Least significant byte appears first
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <returns></returns>
        private float ConvertToSingle(byte p1, byte p2, byte p3, byte p4)
        {
            byte [] array = new byte [] {p4,p3,p2,p1};
            //68, 151, 111, 166, ==or 1211.489. 
            // Least Significant byte (p4) appears first
            //return BitConverter.ToSingle(new byte[] { 166, 111, 151, 68 }, 0);
            //float test = BitConverter.ToSingle(new byte[] { 100, 112, 217, 191 }, 0);
            
            float result = BitConverter.ToSingle(array,0);
            string tryText = string.Format("{0:f}", result);
            return float.Parse(tryText);
        }

        private float ConvertToSingle(byte[] data, ref int index, bool reverse)
        {
            if (data.Length < index + 3)
            {
                return 0.0f;
            }
            // still need to reverse
            byte[] array;

            array = reverse ? new byte[] { 
                data[index+3],
                data[index+2], 
                data[index+1], 
                data[index]
            } :
            new byte[] { 
                data[index],
                data[index+1], 
                data[index+2], 
                data[index+3]
            };
            float result = BitConverter.ToSingle(array, 0);
            string tryText = string.Format("{0:f}", result);
            index += 4;
            return float.Parse(tryText);
        }

        private double ConvertToDouble(byte[] data, ref int index)
        {
            // still need to reverse
            byte[] array = new byte[] { 
           
                data[index+7], 
                data[index+6],
                data[index+5],
                data[index+4],
                data[index+3],
                data[index+2], 
                data[index+1], 
                data[index]
            };
            double result = BitConverter.ToDouble(array, 0);
            string tryText = string.Format("{0:f}", result);
            index += 8;
            return double.Parse(tryText);
        }
    }
}
