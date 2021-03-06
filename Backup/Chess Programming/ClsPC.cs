using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

namespace Chess_Programming
{
    public class ClsPC
    {
        
        public ClsPC(int _port)
        {
            this.Port = _port;
            this.hostName = Dns.GetHostName();
            IPHostEntry _ipHostEntry = Dns.GetHostEntry(this.hostName);
            foreach (IPAddress ip in _ipHostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.ipAddress = ip;
                    break;
                }
            }
            this.ConnectState = 0;
            this.Function = 3;
            this.ParnerIP = null;
        }
        public void DisposeUDP()
        {
            this.ConnectState = 0;
            this.ParnerIP = null;
            this.ParnerName = null;
            this.Function = 3;
        }
        

        UdpClient uClient;
        #region Property
        private string _hostname;
        public string hostName
        {
            get { return _hostname; }
            set { _hostname = value; }
        }

        private IPAddress _ipaddress;
        public IPAddress ipAddress
        {
            get { return _ipaddress; }
            set { _ipaddress = value; }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// 0: None 1: UDP 2: TCp
        /// </summary>
        private int _connectState;
        public int ConnectState
        {
            get { return _connectState; }
            set { _connectState = value; }
        }

        private string _ParnerIP;
        public string ParnerIP
        {
            get { return _ParnerIP; }
            set { _ParnerIP = value; }
        }

        private string _ParnerName;
        public string ParnerName
        {
            get { return _ParnerName; }
            set { _ParnerName = value; }
        }

        //1. Server     //2. Client       //3. None
        private int _function;
        public int Function
        {
            get { return _function; }
            set { _function = value; }
        }

        private string _profile;

        public string Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        #endregion
        #region UDP

        public string ReceiveUDPDataBroadCast()
        {            
            try
            {
                uClient = new UdpClient(frmMain.localpc.Port);
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                
                Byte[] data = uClient.Receive(ref RemoteIpEndPoint);
                uClient.Close();
                string  message = Encoding.UTF8.GetString(data);                
                return message;
            }
            catch
            { return ""; }
            
        }
        public string ReceiveUDPData(string ip)
        {
            try
            {
                uClient = new UdpClient(frmMain.localpc.Port);                
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ip), 0);
                
                Byte[] data = uClient.Receive(ref RemoteIpEndPoint);
                uClient.Close();
                string message = Encoding.UTF8.GetString(data);
                return message;
            }
            catch
            {
                return "";
            }
        }
        public void SendUDPData(string ipTo, string type, string item)
        {
            UdpClient udp = new UdpClient();
            int _port = this.Port;
            udp.Connect(ipTo,_port);
            Byte[] data = Encoding.UTF8.GetBytes(type + "#" + this.ipAddress.ToString() + "#" + this.Profile + "#" + item);
            udp.Send(data, data.Length);
            udp.Close();
        }
        public void DisconnectUDP()
        {
            if (uClient != null)
            {
                uClient.Close();
            }

        }

        #endregion
        #region TCP
        private TcpClient client;
        private TcpListener server;
        private NetworkStream ns;
        private StreamReader sr = null;
        private StreamWriter sw = null;
        private IPEndPoint iep;

        Thread tActiveListenTCP;
        public void InitialTCP()
        {
            if (this.Function == 1)
            {
                iep = new IPEndPoint(IPAddress.Any, this.Port);
                server = new TcpListener(iep);
                server.Start();
                frmMain.localpc.SendUDPData(frmMain.localpc.ParnerIP, "SERVERREADY", "");
            }
            else
            {
                iep = new IPEndPoint(IPAddress.Parse(this.ParnerIP), this.Port);
            }
            frmMain.localpc.ConnectState = 2;
            client = new TcpClient();
            tActiveListenTCP = new Thread(new ThreadStart(ActiveListenTCP));
            tActiveListenTCP.IsBackground = true;
            tActiveListenTCP.Start();
        }

        void ActiveListenTCP()
        {
            while (true)
            {
                if (client.Connected)
                {
                    ns = client.GetStream();
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);
                    return;
                }                
                if (this.Function == 1)
                {
                    client = server.AcceptTcpClient();                     
                }
                else
                {
                    client.Connect(iep);
                    frmMain.localpc.SendUDPData(frmMain.localpc.ParnerIP, "CLIENTREADY", "");            
                }
            }
        }

        public void SendTCPData(string type, string item)
        {             
            sw = new StreamWriter(client.GetStream());
            if (this.sw != null)
            {
                this.sw.WriteLine(type + "#" + item);
                this.sw.Flush();
            }
        }
        public string ReceiveTCPData()
        {
            string strdata = "";
            sr = new StreamReader(client.GetStream());
            
            if (sr != null)
            {
                strdata = this.sr.ReadLine();
            }
            return strdata;         
        }

        public void SendFileTCP()
        {
            FileStream fs = new FileStream(Application.StartupPath + "\\Profiles\\" + clsEncoding.Encode(frmMain.localpc.Profile) + ".xml", FileMode.Open, FileAccess.Read);
            int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(fs.Length) / Convert.ToDouble(1024)));
            int TotalLength = (int)fs.Length, CurrentPacketLength, counter = 0;
            for (int i = 0; i < NoOfPackets; i++)
            {
                if (TotalLength > 1024)
                {
                    CurrentPacketLength = 1024;
                    TotalLength = TotalLength - CurrentPacketLength;
                }
                else
                {
                    CurrentPacketLength = TotalLength;
                }
                byte[] SendingBuffer = new byte[CurrentPacketLength];
                fs.Read(SendingBuffer, 0, CurrentPacketLength);
                ns.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                
            }
            ns.Close();
            fs.Close();                             
        }
       
        public void ReceiveFileTCP()
        {
            byte[] RecData = new byte[1024];
            int RecBytes;

            ns = client.GetStream();
            int totalrecbytes = 0;
            FileStream Fs = new FileStream(Application.StartupPath + "\\Profiles\\" + clsEncoding.Encode(frmMain.localpc.ParnerName) + ".xml", FileMode.OpenOrCreate, FileAccess.Write);
            while ((RecBytes = ns.Read(RecData, 0, RecData.Length)) > 0)
            {
                Fs.Write(RecData, 0, RecBytes);
                totalrecbytes += RecBytes;
            }           
            Fs.Close();
            frmMain.localpc.SendTCPData("SENDED", "");          
        }
        
        public void DisposeTCPConnection()
        {
            if (frmMain.localpc.ConnectState == 2)
            {
                tActiveListenTCP.Abort();
                if (frmMain.localpc.Function == 1)
                {
                    server.Stop();              
                }                
                client.Close();
                iep = null;

                if (ns != null)
                {
                    ns.Close();
                    ns = null;
                }
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
            }
        }

        #endregion
      
        public void AnalysisReceiveUDPString(string message, out string strT, out string strPI, out string strPN, out string strI)
        {
            string[] arrss = new string[0];
            int iType = message.IndexOf('#');
            int iParnerIp = message.IndexOf('#', iType + 1);
            int iParnerName = message.IndexOf('#', iParnerIp + 1);
            int iItem = message.IndexOf('#', iParnerName + 1);

            strT = message.Substring(0, iType);
            strPI = message.Substring(iType + 1, iParnerIp - iType - 1);
            strPN = message.Substring(iParnerIp + 1, iParnerName - iParnerIp - 1);
            strI = message.Substring(iParnerName + 1, message.Length - iParnerName - 1);
        }
        public void AnalysisReceiveTCPString(string message, out string strT, out string strI)
        {
            int iType = message.IndexOf('#');
            int iItem = message.IndexOf('#', iType + 1);
            strT = message.Substring(0, iType);
            strI = message.Substring(iType + 1, message.Length - iType - 1);            
        }
    }
}