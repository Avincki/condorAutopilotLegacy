using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    public class CondorUDPData : ICloneable
    {
        const int VERSION = 2;
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "CondorGimball";
        const string keyName = userRoot + "\\" + subkey;
        public string message { get; set; }

        public DateTime timeStamp { get; set; }
        public int index { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float time { get; set; }
        public float altitude { get; set; }
        public float vario { get; set; }
        public float turnrate { get; set; }
        public float yawstring { get; set; }
        public float yaw { get; set; }
        public float pitch { get; set; }
        public float bank { get; set; }
        public float ax { get; set; }
        public float ay { get; set; }
        public float az { get; set; }
        public float vx { get; set; }
        public float vy { get; set; }
        public float vz { get; set; }
        public float bankrate { get; set; }
        public float pitchrate { get; set; }
        public float yawrate { get; set; }
        //public float height { get; set; }
        public float ias { get; set; }
        public float tas { get; set; }
        public double acceleration { get; set; }
        public int wayPoint { get; set; }
        public float  dijkstraRange { get; set; }
        public int thermal { get; set; }
        public Point3d thermalPoint { get; set; }
        public float wheelheight { get; set; }
        public int flaps { get; set; }
        public float water { get; set; }

        public bool planevisible { get; set; }
        public float plane_x { get; set; }
        public float plane_y { get; set; }
        public float plane_bank { get; set; }
        public float yawstring_x { get; set; }
        public float yawstring_y { get; set; }

        public float targetSpeed { get; set; }
        public float targetHeading { get; set; }

        public float targetRange { get; set; }
        public float range { get; set; }
        public float stickRoll { get; set; }
        public float stickPitch { get; set; }
        public float targetPitch { get; set; }
        public float targetBank { get; set; }
        public float targetTurn { get; set; }


        public CondorUDPData()
        {
            thermalPoint = new Point3d();
            targetPitch = 0;
            targetBank = 0;
            targetTurn = 0;

        }
        public CondorUDPData(CondorUDPData other)
        {
            index = other.index;
            x = other.x;
            y = other.y;
            time = other.time;
            timeStamp = other.timeStamp;
            altitude = other.altitude;
            vario = other.vario;
            turnrate = other.turnrate;
            yawstring = other.yawstring;
            yaw = other.yaw;
            pitch = other.pitch;
            bank = other.bank;
            ax = other.ax;
            ay = other.ay;
            az = other.az;
            vx = other.vx;
            vy = other.vy;
            vz = other.vz;
            bankrate = other.bankrate;
            pitchrate = other.pitchrate;
            yawrate = other.yawrate;
            ias = other.ias;
            tas = other.tas;
            wayPoint = other.wayPoint;
            thermal = other.thermal;
            thermalPoint = new Point3d(other.thermalPoint);
            wheelheight = other.wheelheight;
            flaps = other.flaps;
            water = other.water;
            dijkstraRange = other.dijkstraRange;
            planevisible = other.planevisible;
            plane_x = other.plane_x;
            plane_y = other.plane_y;
            plane_bank = other.plane_bank;
            yawstring_x = other.yawstring_x;
            yawstring_y = other.yawstring_y;
            targetSpeed = other.targetSpeed;
            targetHeading = other.targetHeading;
            targetRange = other.targetRange;
            range = other.range;
            stickRoll = other.stickRoll;
            stickPitch = other.stickPitch;
            targetPitch = other.targetPitch;
            targetBank = other.targetBank;
            targetTurn = other.targetTurn;

        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public void myClone(CondorUDPData other)
        {
            index = other.index;
            x = other.x;
            y = other.y;
            time = other.time;
            timeStamp = other.timeStamp;
            altitude = other.altitude;
            vario = other.vario;
            turnrate = other.turnrate;
            yawstring = other.yawstring;
            yaw = other.yaw;
            pitch = other.pitch;
            bank = other.bank;
            ax = other.ax;
            ay = other.ay;
            az = other.az;
            vx = other.vx;
            vy = other.vy;
            vz = other.vz;
            bankrate = other.bankrate;
            pitchrate = other.pitchrate;
            yawrate = other.yawrate;
            ias = other.ias;
            wayPoint = other.wayPoint;
            thermal = other.thermal;
            thermalPoint = new Point3d(other.thermalPoint);
            tas = other.tas;
            wheelheight = other.wheelheight;
            flaps = other.flaps;
            water = other.water;
            dijkstraRange = other.dijkstraRange;
            planevisible = other.planevisible;
            plane_x = other.plane_x;
            plane_y = other.plane_y;
            plane_bank = other.plane_bank;
            yawstring_x = other.yawstring_x;
            yawstring_y = other.yawstring_y;
            targetSpeed = other.targetSpeed;
            targetHeading = other.targetHeading;
            targetRange = other.targetRange;
            range = other.range;
            stickRoll = other.stickRoll;
            stickPitch = other.stickPitch;
            targetPitch = other.targetPitch;
            targetBank = other.targetBank;
            targetTurn = other.targetTurn;

        }

        public string ToAugmentedString(TerrainMap terrainMap)
        {
            string str = ToString();
            terrainMap.GetNodeData(x, y, out Node node);
            str += "ground heigth = " + node.height.ToString("F0") + ";";
            str += "safe height = " + node.safeHeight().ToString("F0") + ";";
            return str;
        }

        public double getSpeed()
        {
            return ias;
        }
        public double getHeading()
        {
            return Math.Atan2(vy, vx) * 180 / Math.PI;
        }

        public override string ToString()
        {
            var hours = Math.Floor(time);
            var mindec = 60*(time - hours);
            var mins = Math.Floor(mindec);
            string strMin = mins.ToString();
            if (mins < 10)
                strMin = "0" + strMin;
            var secs = 60 * (mindec - Math.Floor(mindec));
            string strSec = secs.ToString("F1");
            if (secs < 10)
                strSec = "0" + strSec;
            if (hours > 24)
                hours -= 24;

            var speed = 3.6*Math.Sqrt(vx * vx + vy * vy + vz * vz);

            string strTime = hours.ToString() + ":" + strMin + ":" + strSec;

            string str = "Index = " + index.ToString();
            str += "\n\ntime = " + strTime + ";";
            str += "speed = " + speed.ToString("F1") + ";";
            str += "x = " + x.ToString("F0") + ";";
            str += "y = " + y.ToString("F0") + ";";
            str += "altitude = " + altitude.ToString("F0") + "\n\n";
            str += "vario = " + vario.ToString("F2") + ";";
            str += "turnrate = " + Utils.rad2deg(turnrate).ToString("F1") + ";";
            str += "yawstring = " + Utils.rad2deg(yawstring).ToString("F1") + ";";
            str += "yaw = " + Utils.rad2deg(yaw).ToString("F1") + ";";
            str += "pitch = " + Utils.rad2deg(pitch).ToString("F1") + ";";
            str += "bank = " + Utils.rad2deg(bank).ToString("F1") + ";";
            str += "ax = " + ax.ToString("F2") + ";";
            str += "ay = " + ay.ToString("F2") + ";";
            str += "az = " + az.ToString("F2") + ";";
            str += "vx = " + vx.ToString("F1") + ";";
            str += "vy = " + vy.ToString("F1") + ";";
            str += "vz = " + vz.ToString("F1") + ";";
            str += "bankrate = " + Utils.rad2deg(bankrate).ToString("F1") + ";";
            str += "pitchrate = " + Utils.rad2deg(pitchrate).ToString("F1") + ";";
            str += "yawrate = " + Utils.rad2deg(yawrate).ToString("F1") + ";";
            str += "ias = " + ias.ToString("F0") + ";";
            str += "wayPoint = " + wayPoint.ToString() + ";";
            str += "thermal = " + thermal.ToString() + ";";
            str += "dijkstraRange = " + dijkstraRange.ToString("F0") + ";";
            str += "planevisible = " + (Convert.ToInt32(planevisible)).ToString("F0") + ";";
            str += "plane_x = " + plane_x.ToString("F3") + ";";
            str += "plane_y = " + plane_y.ToString("F3") + ";";
            str += "plane_bank = " + plane_bank.ToString("F4") + ";";
            str += "yawstring_x = " + yawstring_x.ToString("F3") + ";";
            str += "yawstring_y = " + yawstring_y.ToString("F3") + ";";



            return str;

        }

        public string ToBroadCastString()
        {
            string str = "";
            str += time.ToString() + ";";
            str += x.ToString("F0") + ";";
            str += y.ToString("F0") + ";";
            str += altitude.ToString("F1") + ";";
            str += vario.ToString("F2") + ";";
            str += turnrate.ToString("F4") + ";";
            str += yawstring.ToString("F4") + ";";
            str += yaw.ToString("F4") + ";";
            str += pitch.ToString("F4") + ";";
            str += bank.ToString("F4") + ";";
            str += ax.ToString("F2") + ";";
            str += ay.ToString("F2") + ";";
            str += az.ToString("F2") + ";";
            str += vx.ToString("F2") + ";";
            str += vy.ToString("F2") + ";";
            str += vz.ToString("F2") + ";";
            str += bankrate.ToString("F4") + ";";
            str += pitchrate.ToString("F4") + ";";
            str += yawrate.ToString("F4") + ";";
            str += ias.ToString("F1") + ";";
            str += wayPoint.ToString() + ";";
            str += thermal.ToString() + ";";
            str += dijkstraRange.ToString("F0");
            str += (Convert.ToInt32(planevisible)).ToString("F0") + ";";
            str += plane_x.ToString("F3") + ";";
            str += plane_y.ToString("F3") + ";";
            str += plane_bank.ToString("F4") + ";";
            str += yawstring_x.ToString("F3") + ";";
            str += yawstring_y.ToString("F3") + ";";

            return str;

        }

        public bool FromBroadCastString(string strBroadCast)
        {
            string[] subs = strBroadCast.Split(';');

            if (subs.Length != 23)
                return false;

            time = Convert.ToSingle(subs[0]);
            x = Convert.ToSingle(subs[1]);
            y = Convert.ToSingle(subs[2]);
            altitude = Convert.ToSingle(subs[3]);
            vario = Convert.ToSingle(subs[4]);
            turnrate = Convert.ToSingle(subs[5]);
            yawstring = Convert.ToSingle(subs[6]);
            yaw = Convert.ToSingle(subs[7]);
            pitch = Convert.ToSingle(subs[8]);
            bank = Convert.ToSingle(subs[9]);
            ax = Convert.ToSingle(subs[10]);
            ay = Convert.ToSingle(subs[11]);
            az = Convert.ToSingle(subs[12]);
            vx = Convert.ToSingle(subs[13]);
            vy = Convert.ToSingle(subs[14]);
            vz = Convert.ToSingle(subs[15]);
            bankrate = Convert.ToSingle(subs[16]);
            pitchrate = Convert.ToSingle(subs[17]);
            yawrate = Convert.ToSingle(subs[18]);
            ias = Convert.ToSingle(subs[19]);
            wayPoint = Convert.ToInt32(subs[20]);
            thermal = Convert.ToInt32(subs[21]);
            dijkstraRange = Convert.ToSingle(subs[22]);

            return true;
        }

        public FlightPathPoint GetFlightPoint()
        {
            Vector3d vec = new Vector3d(vx, vy, vz);
            float speed = vec.length();
            vec.normalize();
            FlightPathPoint pt = new FlightPathPoint(
                x, y, altitude, vario, vec, speed, 0, 0, time
                );

            return pt;
        }
        public Point3d GetPosition()
        {
            return new Point3d(x, y, altitude);
        }

        public bool FileWriter(BinaryWriter file)
        {
            file.Write(x);
            file.Write(y);
            file.Write(time);
            file.Write(altitude);
            file.Write(vario);
            file.Write(turnrate);
            file.Write(yawstring);
            file.Write(yaw);
            file.Write(pitch);
            file.Write(bank);
            file.Write(ax);
            file.Write(ay);
            file.Write(az);
            file.Write(vx);
            file.Write(vy);
            file.Write(vz);
            file.Write(bankrate);
            file.Write(pitchrate);
            file.Write(yawrate);
            file.Write(ias);
            file.Write(Convert.ToInt32(wayPoint));
            file.Write(Convert.ToInt32(thermal));
            file.Write(thermalPoint.X);
            file.Write(thermalPoint.Y);
            file.Write(thermalPoint.Z);
            file.Write(dijkstraRange);
            //file.Write(acceleration);

            return true;
        }

        public bool FileReader(BinaryReader file, int version)
        {
            try
            {
                x = file.ReadSingle();
                y = file.ReadSingle();
                time = file.ReadSingle();
                altitude = file.ReadSingle();
                vario = file.ReadSingle();
                turnrate = file.ReadSingle();
                yawstring = file.ReadSingle();
                yaw = file.ReadSingle();
                pitch = file.ReadSingle();
                bank = file.ReadSingle();
                ax = file.ReadSingle();
                ay = file.ReadSingle();
                az = file.ReadSingle();
                vx = file.ReadSingle();
                vy = file.ReadSingle();
                vz = file.ReadSingle();
                bankrate = file.ReadSingle();
                pitchrate = file.ReadSingle();
                yawrate = file.ReadSingle();
                ias = file.ReadSingle();
                wayPoint = file.ReadInt32();
                thermal = file.ReadInt32();
                float dIAS2TAS = NaviCon.IAS2TAS(1, altitude);
                tas = dIAS2TAS * ias;
                thermalPoint = new Point3d();
                thermalPoint.X = file.ReadSingle();
                thermalPoint.Y = file.ReadSingle();
                thermalPoint.Z = file.ReadSingle();
                if (version > 1)
                    dijkstraRange = file.ReadSingle();
                else
                    dijkstraRange = 30000;


                //cas = file.ReadSingle();
                //acceleration = file.ReadSingle();
            }
            catch (IOException ioexp)
            {
                string strMessage = $"\nError:  {ioexp.Message}";
                file.Close();
                return false;
            }


            return true;
        }
        public bool Interpolate(float dTime, CondorUDPData data1, CondorUDPData data2)
        {
            time = dTime;
            x = Utils.InterpolateFloat(dTime, data1.time, data1.x, data2.time, data2.x);
            y = Utils.InterpolateFloat(dTime, data1.time, data1.y, data2.time, data2.y);
            altitude = Utils.InterpolateFloat(dTime, data1.time, data1.altitude, data2.time, data2.altitude);
            vario = Utils.InterpolateFloat(dTime, data1.time, data1.vario, data2.time, data2.vario);
            turnrate = Utils.InterpolateFloat(dTime, data1.time, data1.turnrate, data2.time, data2.turnrate);
            yawstring = Utils.InterpolateFloat(dTime, data1.time, data1.yawstring, data2.time, data2.yawstring);
            pitch = Utils.InterpolateFloat(dTime, data1.time, data1.pitch, data2.time, data2.pitch);
            bank = Utils.InterpolateFloat(dTime, data1.time, data1.bank, data2.time, data2.bank);
            ax = Utils.InterpolateFloat(dTime, data1.time, data1.ax, data2.time, data2.ax);
            ay = Utils.InterpolateFloat(dTime, data1.time, data1.ay, data2.time, data2.ay);
            az = Utils.InterpolateFloat(dTime, data1.time, data1.az, data2.time, data2.az);
            vx = Utils.InterpolateFloat(dTime, data1.time, data1.vx, data2.time, data2.vx);
            vy = Utils.InterpolateFloat(dTime, data1.time, data1.vy, data2.time, data2.vy);
            vz = Utils.InterpolateFloat(dTime, data1.time, data1.vz, data2.time, data2.vz);
            bankrate = Utils.InterpolateFloat(dTime, data1.time, data1.bankrate, data2.time, data2.bankrate);
            pitchrate = Utils.InterpolateFloat(dTime, data1.time, data1.pitchrate, data2.time, data2.pitchrate);
            yawrate = Utils.InterpolateFloat(dTime, data1.time, data1.yawrate, data2.time, data2.yawrate);
            ias = Utils.InterpolateFloat(dTime, data1.time, data1.ias, data2.time, data2.ias);
            wayPoint = data1.wayPoint;
            thermal = data2.thermal;
            thermalPoint = new Point3d(data2.thermalPoint);
            float dIAS2TAS = NaviCon.IAS2TAS(1, altitude);
            tas = dIAS2TAS * ias;
            wheelheight = data1.wheelheight;
            flaps = data1.flaps;
            water = data1.water;
            targetSpeed = data1.targetSpeed;
            targetHeading = data1.targetHeading;
            targetRange = data1.targetRange;
            range = data1.range;
            stickRoll = data1.stickRoll;
            stickPitch = data1.stickPitch;
            targetPitch = data1.targetPitch;
            targetBank = data1.targetBank;
            targetTurn = data1.targetTurn;



            float yaw1 = data1.yaw;
            float yaw2 = data2.yaw;
            float pi = Convert.ToSingle(Math.PI);

            if (yaw1-yaw2 > pi)
            {
                yaw1 -= 2 * pi;
            }
            else if (yaw2 - yaw1 > pi)
            {
                yaw2 -= 2 * pi;
            }

            yaw = Utils.InterpolateFloat(dTime, data1.time, yaw1, data2.time, yaw2);

            return true;
        }

        static public void WriteLog(string strFileName, string strFlightPlan, List<CondorUDPData>myCondorUDPDataLog)
        {

            BinaryWriter binWriter =
                new BinaryWriter(File.Open(strFileName, FileMode.Create), Encoding.ASCII);

            binWriter.Write(VERSION);
            binWriter.Write(strFlightPlan);
            for (int i = 0; i < myCondorUDPDataLog.Count; i++)
            {
                myCondorUDPDataLog[i].FileWriter(binWriter);
            }
            binWriter.Close();
        }
        static public bool ReadLog(string strFileName, out string strFlightPlan, List<CondorUDPData> myCondorUDPDataLog)
        {
            int thisVersion = -1;
            myCondorUDPDataLog.Clear();
            BinaryReader binReader =
                new BinaryReader(File.Open(strFileName, FileMode.Open), Encoding.ASCII);
            try
            {
                thisVersion = binReader.ReadInt32();
                strFlightPlan = binReader.ReadString();
            }
            catch (IOException ioexp)
            {
                string strMessage = $"\nError:  {ioexp.Message}";
                binReader.Close();
                strFlightPlan = "";
                return false;
            }

            bool bContinueReading = true;
            int index = 0;
            while (bContinueReading)
            {
                CondorUDPData condorUDPData = new CondorUDPData();

                bContinueReading = condorUDPData.FileReader(binReader, thisVersion);
                if (bContinueReading)
                {
                    condorUDPData.index = index;
                    index++; myCondorUDPDataLog.Add(condorUDPData);
                }
            }
            binReader.Close();
            return true;
        }

        static public int Locate(Point3d ptLoc, List<CondorUDPData> myCondorUDPDataLog)
        {
            double dClosest = 10000;
            int iFound = -1;
            for (int j = 0; j < myCondorUDPDataLog.Count(); j += 1)
            {
                //if (ptLoc.distanceXYTo(myCondorUDPDataLog[j].getPosition()) < 10000)
                //{
                //    for (int i = j; i < Math.Min(j+200, myCondorUDPDataLog.Count()); i++)
                //    {
                //        if (Math.Abs(myCondorUDPDataLog[i].altitude - ptLoc.Z) < 50)
                //        {
                            double dQ = ptLoc.distanceXYTo(myCondorUDPDataLog[j].GetPosition()) + Math.Abs(myCondorUDPDataLog[j].altitude - ptLoc.Z);

                            if (dQ < dClosest)
                            {
                                dClosest = dQ;
                                iFound = j;
                            }
                //        }
                //    }
                //}

            }

            return iFound;
        }


        static public int Locate(float dTime, int iPos, List<CondorUDPData> myCondorUDPDataLog)
        {
            iPos = Math.Max(iPos, 0);
            iPos = Math.Min(iPos, myCondorUDPDataLog.Count - 1);

            if (myCondorUDPDataLog[iPos].time > dTime)
            {
                for (int j = iPos; j > 0; j -= 1)
                {
                    if (dTime >= myCondorUDPDataLog[j].time &&
                        dTime <= myCondorUDPDataLog[j + 1].time)
                    {
                        return j;
                    }
                }
            }
            else
            {
                for (int j = iPos; j < myCondorUDPDataLog.Count-1; j += 1)
                {
                    if (dTime >= myCondorUDPDataLog[j].time &&
                        dTime <= myCondorUDPDataLog[j + 1].time)
                    {
                        return j;
                    }
                }
            }

            return -1;
        }


    }
}
