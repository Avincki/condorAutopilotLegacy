using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Win32;

namespace CondorAutoPilot
{
    class Listener
    {
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "CondorGimball";
        const string keyName = userRoot + "\\" + subkey;
        private bool m_bRun = true;

        public CondorUDPData condorUDPData { get; set; }

        public Listener()
        {
            condorUDPData = new CondorUDPData();
        }

        public async void StartListener()
        {
            m_bRun = true;
            string strMessage = ".";
            const int listenPort = 55278;

            // IPAddress IPCondor = System.Net.IPAddress.Parse(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            IPAddress IPCondorAny = System.Net.IPAddress.Any;

            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPCondorAny, listenPort);

            try
            {
                while (m_bRun)
                {
                    var result = await listener.ReceiveAsync();
                    byte[] bytes = result.Buffer;
                    // byte[] bytes = listener.ReceiveAsync().Buffer;

                    // strMessage += $"Received broadcast from {groupEP} :";
                    string str = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    strMessage = $" {str}";
                    processMessageData(ref str);
                    Registry.SetValue(keyName, "Message", strMessage);
                    var regVal = Registry.GetValue(keyName, "Run", true);
                    if (regVal == null)
                        m_bRun = true;
                    else
                        m_bRun = Convert.ToBoolean(regVal);
                }
            }
            catch (SocketException e)
            {
                strMessage += e.ToString();
                Registry.SetValue(keyName, "Message", strMessage);
            }
            finally
            {
                listener.Close();
            }
        }

        private static bool read(string strMessage, string strToken, out float dValue)
        {
            dValue = 0;
            try
            {
                int iPos;
                string strSearch = strToken.ToLower() + "=";
                if ((iPos = strMessage.IndexOf(strSearch)) >= 0)
                {
                    string str = strMessage.Substring(iPos + strSearch.Length);
                    str = str.Substring(0, str.IndexOf("\n"));
                    double value = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
                    Registry.SetValue(keyName, strToken, value);
                    dValue = Convert.ToSingle(value);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        private static bool read(string strMessage, string strToken, out string strValue)
        {
            strValue = "";
            try
            {
                int iPos;
                string strSearch = strToken.ToLower() + "=";
                if ((iPos = strMessage.IndexOf(strSearch)) >= 0)
                {
                    string str = strMessage.Substring(iPos + strSearch.Length);
                    strValue = str.Substring(0, str.IndexOf("\n"));
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        private void processMessageData(ref string strMessage)
        {
            condorUDPData.message = strMessage;
            condorUDPData.timeStamp = DateTime.Now;
            float dValue;
            if (read(strMessage, "YawStringAngle", out dValue)) condorUDPData.yawstring = dValue;
            if (read(strMessage, "Yaw", out dValue)) condorUDPData.yaw = dValue;
            if (read(strMessage, "Pitch", out dValue)) condorUDPData.pitch = dValue;
            if (read(strMessage, "Bank", out dValue)) condorUDPData.bank = dValue;
            if (read(strMessage, "YawRate", out dValue)) condorUDPData.yawrate = dValue;
            if (read(strMessage, "PitchRate", out dValue)) condorUDPData.pitchrate = dValue;
            if (read(strMessage, "RollRate", out dValue)) condorUDPData.bankrate = dValue;
            if (read(strMessage, "TurnRate", out dValue)) condorUDPData.turnrate = dValue;
            if (read(strMessage, "evario", out dValue)) condorUDPData.vario = dValue;
            if (read(strMessage, "Altitude", out dValue)) condorUDPData.altitude = dValue;
            if (read(strMessage, "Time", out dValue)) condorUDPData.time = dValue;
            if (read(strMessage, "Ax", out dValue)) condorUDPData.ax = dValue;
            if (read(strMessage, "Ay", out dValue)) condorUDPData.ay = dValue;
            if (read(strMessage, "Az", out dValue)) condorUDPData.az = dValue;
            if (read(strMessage, "Vx", out dValue)) condorUDPData.vx = dValue;
            if (read(strMessage, "Vy", out dValue)) condorUDPData.vy = dValue;
            if (read(strMessage, "Vz", out dValue)) condorUDPData.vz = dValue;
            if (read(strMessage, "Airspeed", out dValue)) condorUDPData.ias = dValue;
            if (read(strMessage, "wheelheight", out dValue)) condorUDPData.wheelheight = dValue;
            if (read(strMessage, "flaps", out dValue)) condorUDPData.flaps = Convert.ToInt32(dValue);
            if (read(strMessage, "water", out dValue)) condorUDPData.water = dValue;
            float dIAS2CAS = NaviCon.IAS2TAS(1, condorUDPData.altitude);
            condorUDPData.tas = dIAS2CAS * condorUDPData.ias;
        }
    }


    class BroadCaster
    {
        private bool m_bRun = true;

        public CondorUDPData condorUDPData { get; set; }

        public BroadCaster()
        {
            condorUDPData = new CondorUDPData();
        }

        public async void StartListener()
        {
            m_bRun = true;
            const int listenPort = 55279;

            IPAddress IPCondorAny = System.Net.IPAddress.Any;

            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPCondorAny, listenPort);

            try
            {
                while (m_bRun)
                {
                    var result = await listener.ReceiveAsync();
                    byte[] bytes = result.Buffer;

                    string str = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    condorUDPData.FromBroadCastString(str);
                }
            }
            catch (SocketException e)
            {
            }
            finally
            {
                listener.Close();
            }
        }
        public void Send(CondorUDPData condorUDPData)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 55279);
            string str = condorUDPData.ToBroadCastString();
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
        }
    }

}



