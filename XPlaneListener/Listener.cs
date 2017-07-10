using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XPlaneListener
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data;
    }

    public class LogEventArgs : EventArgs
    {
        public string Message;
        public bool NewLine;
    }

    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
    public delegate void LogReceivedEventHandler(object sender, LogEventArgs e);

    public class Listener
    {
        private readonly IPAddress ipAddress;
        private readonly int port;
        private bool stopped = false;
        public event DataReceivedEventHandler DataReceived;
        public event LogReceivedEventHandler LogReceived;

        public Listener(IPAddress IPAddresss, int Port, string FileName)
        {
            this.ipAddress = IPAddresss;
            this.port = Port;
            if (System.IO.File.Exists(FileName))
            {
                System.IO.File.Delete(FileName);
                CreateLogEvent("Deleted log file", true);
            }
        }

        public void Listen()
        {
            stopped = false;
            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(ipAddress, port);
            UdpClient newsock = new UdpClient(ipep);
            // Polling the socket
            while (!stopped && newsock.Available == 0)
            {
                CreateLogEvent("Waiting for a client...", true);
                System.Threading.Thread.Sleep(100);
            }
            // Got one packet, should be more coming along soon
            //int counter1 = 0;
            while (!stopped)
            {
                CreateLogEvent("X-Plane Data Read:", true);
                data = newsock.Receive(ref ipep);
                // Analyse every 10th message packet
                //if (counter1++ % 10 == 0)
                //{
                    CreateDataReceivedEvent(data);
                //}
            }
            newsock.Close();
            newsock = null;
            CreateLogEvent("Closed", true);
        }

        private void CreateDataReceivedEvent(byte[] data)
        {
            if (DataReceived != null)
            { 
                DataReceivedEventArgs e = new DataReceivedEventArgs();
                e.Data = data;
                DataReceived(this, e);
            }
        }

        private void CreateLogEvent(string message, bool newLine)
        {
            if (LogReceived != null)
            {
                LogEventArgs e 
                    = new LogEventArgs() { Message = message, NewLine = newLine };
                LogReceived(this, e);
            }
        }

        public void Cancel()
        {
            stopped = true;
        }

        public void AppendToFile(string FileName, string LineText)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                return;
            }
            System.IO.File.AppendAllLines(FileName, new string [] {string.Format("{0}\r\n",LineText)});
        }
    }
}
