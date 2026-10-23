using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    public class Point3d
    {
        public float X, Y, Z;
        public Point3d(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        public Point3d(Point3d pt)
        {
            X = pt.X;
            Y = pt.Y;
            Z = pt.Z;
        }
        public Point3d()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Point3d(float lat, float lon)
        {
            float qx = 0; 
            float qy = 0;
            Form1.naviCon64.calcUTMIter(lat, lon, out qx, out qy);
            X = qx;
            Y = qy;
            Z = 0;
        }

        public void Set(Point3d pt)
        {
            X = pt.X;
            Y = pt.Y;
            Z = pt.Z;
        }
        public void Set(float x, float y, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3d add(float dist, Vector3d vec)
        {
            Point3d ptOut = new Point3d(this);
            ptOut.X += dist * vec.x;
            ptOut.Y += dist * vec.y;
            ptOut.Z += dist * vec.z;

            return ptOut;
        }
        public Point3d add(float distx, Vector3d vecx, float disty, Vector3d vecy)
        {
            Point3d ptOut = new Point3d(this);
            ptOut.X += distx * vecx.x + disty * vecy.x;
            ptOut.Y += distx * vecx.y + disty * vecy.y;
            ptOut.Z += distx * vecx.z + disty * vecy.z;

            return ptOut;
        }
        public Point3d add(float distx, float disty, Vector3d vecx)
        {
            Point3d ptOut = new Point3d(this);
            ptOut.X += distx * vecx.x - disty * vecx.y;
            ptOut.Y += distx * vecx.y + disty * vecx.x;
            ptOut.Z += distx * vecx.z + disty * vecx.z;

            return ptOut;
        }
        public float distanceXYTo(Point3d pt)
        {
            double dVal = Math.Sqrt((X - pt.X) * (X - pt.X) + (Y - pt.Y) * (Y - pt.Y));

            return Convert.ToSingle(dVal);
        }

        public Vector3d vectorXYTo(Point3d pt)
        {
            Vector3d vec = new Vector3d(pt.X - X, pt.Y - Y, 0);
            vec.normalize();
            return vec;

        }

    }
    public class Vector3d
    {
        public float x;
        public float y;
        public float z;
        public Vector3d(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3d(double x, double y, double z)
        {
            this.x = Convert.ToSingle(x);
            this.y = Convert.ToSingle(y);
            this.z = Convert.ToSingle(z);
        }
        public Vector3d()
        {
            x = 0;
            y = 0;
            z = 0;
        }
        public Vector3d(Point3d ptStart, Point3d ptEnd)
        {
            x = ptEnd.X - ptStart.X;
            y = ptEnd.Y - ptStart.Y;
            z = ptEnd.Z - ptStart.Z;
        }
        public Vector3d(Vector3d vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }
        public Vector3d(double dAngle)
        {
            x = Convert.ToSingle(Math.Cos(dAngle));
            y = Convert.ToSingle(Math.Sin(dAngle));
            z = 0;
        }
        public void set(Vector3d vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        static public Vector3d WorldZ()
        {
            return new Vector3d(0, 0, 1);
        }


        public void setNull()
        {
            x = 0;
            y = 0;
            z = 0;
        }
        public void Multiply(float fact)
        {
            x *= fact;
            y *= fact;
            z *= fact;
        }

        public float length()
        {
            float dl = x * x + y * y + z * z;
            dl = Convert.ToSingle(Math.Sqrt(Convert.ToDouble(dl)));
            return dl;
        }
        public void normalize()
        {
            float dl = length();
            if (dl == 0)
                return;
            x /= dl;
            y /= dl;
            z /= dl;

        }
        public Vector3d normalizeN()
        {
            Vector3d vec = new Vector3d();
            float dl = length();
            if (dl == 0)
                return vec;
            vec.x = x / dl;
            vec.y = y / dl;
            vec.z = z / dl;

            return vec;

        }
        public void normalizeXY()
        {
            z = 0;
            float dl = length();
            if (dl == 0)
                return;
            x /= dl;
            y /= dl;
            z /= dl;

        }

        public float dotProduct(Vector3d vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }

        public Vector3d crossProduct(Vector3d vec)
        {
            float x, y, z;

            x = this.y * vec.z - this.z * vec.y;
            y = -this.x * vec.z + this.z * vec.x;
            z = this.x * vec.y - this.y * vec.x;

            Vector3d vecOut = new Vector3d(x, y, z);
            return vecOut;
        }

        public void negate()
        {
            this.x = -x;
            this.y = -y;
            this.z = -z;
        }
        public void add(Vector3d other, float dL = 1)
        {
            x += dL * other.x;
            y += dL * other.y;
            z += dL * other.z;
        }

        public double angleXYTo(Vector3d vec2)
        {
            Vector3d vecXY, vecXY2, vecPerp;
            vecXY = this;
            vecXY.z = 0;
            vecXY.normalize();
            vecPerp = new Vector3d(-vecXY.y, vecXY.x, 0);

            vecXY2 = vec2;
            vecXY2.z = 0;
            vecXY2.normalize();

            double dCos = vecXY.dotProduct(vecXY2);
            double dSin = vecPerp.dotProduct(vecXY2);
            dCos = Math.Min(1, Math.Max(-1, dCos));
            double dAngle = Math.Acos(dCos);

            if (dSin < 0)
                dAngle = -dAngle;


            return dAngle;
        }

    }

    class Viewer
    {
        public Point3d m_ptEye { get; set; }
        private Vector3d m_vecX, m_vecY, m_vecView;
        private float m_dScreen;
        private float m_dScaleScreen, m_dScaleVario;
        private bool m_bViewerOk = false;

 

        public Viewer(Point3d ptEye, Vector3d vecView, float dScreen, float dScaleScreen, float dScaleVario)
        {
            if (vecView.length() < 1)
            {
                m_bViewerOk = false;
            }
            m_bViewerOk = true;
            m_ptEye = ptEye;
            m_vecView = vecView;
            m_vecView.normalize();
            m_vecX = vecView.crossProduct(new Vector3d(0, 0, 1));
            m_vecX.normalize();
            m_vecY = m_vecX.crossProduct(m_vecView);
            m_dScaleScreen = dScaleScreen;
            m_dScaleVario = dScaleVario;
            m_dScreen = dScreen;
        }

        public bool getScreen(Point3d ptPath, ref PointF pt)
        {
            Vector3d vecToPoint = new Vector3d(
                ptPath.X - m_ptEye.X,
                ptPath.Y - m_ptEye.Y,
                ptPath.Z - m_ptEye.Z
                );

            double dL = vecToPoint.length();

            Vector3d vecInScreen = new Vector3d(
                vecToPoint.dotProduct(m_vecX),
                vecToPoint.dotProduct(m_vecY),
                vecToPoint.dotProduct(m_vecView)
                );
            if (vecInScreen.z < m_dScreen)
                return false;

            pt.X = vecInScreen.x * m_dScaleScreen * m_dScreen / vecInScreen.z;
            pt.Y = vecInScreen.y * m_dScaleScreen * m_dScreen / vecInScreen.z;
            return true;
        }

        public bool IsNormalInView(Point3d pt1, Vector3d vecNormal)
        {
            Vector3d vecView = new Vector3d(m_ptEye, pt1);
            int n = 1;
            if (vecNormal.z > 0)
                n = -1;

            return (n*vecView.dotProduct(vecNormal) > 0);
        }
        public bool getPolarScreen(Point3d ptPath, ref PointF pt)
        {
            Vector3d vecToPoint = new Vector3d(
                ptPath.X - m_ptEye.X,
                ptPath.Y - m_ptEye.Y,
                ptPath.Z - m_ptEye.Z
                );

            double dL = vecToPoint.length();

            Vector3d vecInScreen = new Vector3d(
                vecToPoint.dotProduct(m_vecX),
                vecToPoint.dotProduct(m_vecY),
                vecToPoint.dotProduct(m_vecView)
                );
            if (dL < m_dScreen || vecInScreen.z/dL < -0.7)
                return false;

            double dLXZ = Math.Sqrt(vecInScreen.x * vecInScreen.x + vecInScreen.z * vecInScreen.z);

            double dAngleX = 180 * Math.Atan2(vecInScreen.x, vecInScreen.z) / Math.PI;
            double dAngleY = 180 * Math.Atan2(vecInScreen.y, dLXZ) / Math.PI;

            pt.X = Convert.ToSingle(dAngleX * 5);
            pt.Y = Convert.ToSingle(dAngleY * 8);
            return true;
        }

        public bool getScreen(FlightPathPoint ptPath, ref PointF ptBottomScreen, ref PointF ptTopScreen)
        {
            if (!m_bViewerOk)
                return false;

            Point3d ptBottom = new Point3d(
                ptPath.x,
                ptPath.y,
                m_ptEye.Z - 50
                ) ;

             if (!getScreen(ptBottom, ref ptBottomScreen))
                return false;

            Point3d ptTop = new Point3d(
                ptPath.x,
                ptPath.y,
                m_ptEye.Z + ptPath.vario * m_dScaleVario
                ); ;

            if (!getScreen(ptTop, ref ptTopScreen))
                return false;

            return true;

        }
        public bool getScreen(FlightPathPoint ptPath, ref PointF ptScreen, ref float dRadius)
        {
            if (!m_bViewerOk)
                return false;

            Point3d ptBottom = new Point3d(
                ptPath.x,
                ptPath.y,
                m_ptEye.Z
                );

            if (!getScreen(ptBottom, ref ptScreen))
                return false;

            Vector3d vec = new Vector3d(m_ptEye, ptBottom);
            dRadius = 25 / vec.length() * m_dScreen* m_dScaleScreen;
            return true;

        }
        public bool getScreen(FlightPathPoint ptPath, double dTime, ref PointF[] ptScreen)
        {
            if (!m_bViewerOk)
                return false;
            //float deltaTime = 3600*Convert.ToSingle(dTime - ptPath.m_timestamp);
            Point3d ptCenter = new Point3d(
                ptPath.x,
                ptPath.y,
                ptPath.z /*+ deltaTime * (ptPath.m_vario - 0.5f)*/
                );

            Vector3d vecZ = new Vector3d(0, 0, 1);
            Vector3d vecRect = ptPath.vec.crossProduct(vecZ);

            Point3d pt = ptCenter.add(10, vecRect, 10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[0]))
                return false;

            pt = ptCenter.add(-10, vecRect, 10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[1]))
                return false;

            pt = ptCenter.add(-10, vecRect, -10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[2]))
                return false;

            pt = ptCenter.add(10, vecRect, -10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[3]))
                return false;


            return true;

        }
        public bool getScreen(Point3d ptPath1, Point3d ptPath2, ref PointF[] ptScreen)
        {
            if (!m_bViewerOk)
                return false;
            //float deltaTime = 3600*Convert.ToSingle(dTime - ptPath.m_timestamp);
            Point3d ptCenter = new Point3d(
                ptPath1.X,
                ptPath1.Y,
                ptPath1.Z /*+ deltaTime * (ptPath.m_vario - 0.5f)*/
                );
            Vector3d vec = new Vector3d(ptPath1, ptPath2);
            vec.normalize();
            Vector3d vecZ = new Vector3d(0, 0, 1);
            Vector3d vecRect = vec.crossProduct(vecZ);

            Point3d pt = ptCenter.add(10, vecRect, 10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[0]))
                return false;

            pt = ptCenter.add(-10, vecRect, 10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[1]))
                return false;

            pt = ptCenter.add(-10, vecRect, -10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[2]))
                return false;

            pt = ptCenter.add(10, vecRect, -10, vecZ);
            if (!getPolarScreen(pt, ref ptScreen[3]))
                return false;


            return true;

        }

        //public bool getScreenPathExtended(FlightPlanExtendedPoint ptPath, ref PointF[] ptScreen)
        //{
        //    if (!m_bViewerOk)
        //        return false;
        //    Point3d ptCenter = new Point3d(
        //        ptPath.pt.X,
        //        ptPath.pt.Y,
        //        m_ptEye.Z - 500
        //        );

        //    Vector3d vecZ = new Vector3d(0, 0, 1);
        //    Vector3d vecRect = ptPath.vec.crossProduct(vecZ);

        //    Point3d pt = ptCenter.add(50, vecRect, 50, vecZ);
        //    if (!getPolarScreen(pt, ref ptScreen[0]))
        //        return false;

        //    pt = ptCenter.add(-50, vecRect, 50, vecZ);
        //    if (!getPolarScreen(pt, ref ptScreen[1]))
        //        return false;

        //    pt = ptCenter.add(-50, vecRect, -50, vecZ);
        //    if (!getPolarScreen(pt, ref ptScreen[2]))
        //        return false;

        //    pt = ptCenter.add(50, vecRect, -50, vecZ);
        //    if (!getPolarScreen(pt, ref ptScreen[3]))
        //        return false;


        //    return true;

        //}

    }




    public class FlightPathPoint
    {
        public float x, y, z;
        public float vario;
        public Vector3d vec;
        public double timestamp;
        public float speed;
        public float windx, windy;
        public float dPosFunction;
        public FlightPathPoint(float x, float y, float z, float vario, Vector3d vec, float speed,
            float windx, float windy, double timestamp)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.vario = vario;
            this.vec = new Vector3d(vec);
            this.speed = speed;
            this.windx = windx;
            this.windy = windy;
            this.timestamp = timestamp;
        }
        public FlightPathPoint()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.vario = 0;
            this.vec = new Vector3d() ;
            this.speed = 0;
            this.windx = 0;
            this.windy = 0;
            this.timestamp = 0;
        }
        public FlightPathPoint(Point3d pt)
        {
            this.x = pt.X;
            this.y = pt.Y;
            this.z = pt.Z;
            this.vario = 0;
            this.vec = new Vector3d();
            this.speed = 0;
            this.windx = 0;
            this.windy = 0;
            this.timestamp = 0;
        }
        public FlightPathPoint(FlightPathPoint pt)
        {
            this.x = pt.x;
            this.y = pt.y;
            this.z = pt.z;
            this.vario = pt.vario;
            this.vec = new Vector3d(pt.vec);
            this.speed = pt.speed;
            this.windx = pt.windx;
            this.windy = pt.windy;
            this.timestamp = pt.timestamp;
        }

        public void Set(Point3d pt)
        {
            if (pt.Z == Single.NaN) pt.Z = 0;
            this.x = pt.X;
            this.y = pt.Y;
            this.z = pt.Z;
        }

        public void Add(FlightPathPoint pt, double dVx, double dVy)
        {
            Add(pt, Convert.ToSingle(dVx), Convert.ToSingle(dVy));
        }

        public void Add(FlightPathPoint pt, float dVx, float dVy)
        {
            this.vario = pt.vario;
            this.vec = new Vector3d(pt.vec);
            this.speed = pt.speed;
            this.windx = pt.windx;
            this.windy = pt.windy;
            this.timestamp = pt.timestamp;

            this.x = pt.x + pt.vec.x * dVx;
            this.y = pt.y + pt.vec.y * dVx;
            this.z = pt.z;
            this.x = this.x - pt.vec.y * dVy;
            this.y = this.y + pt.vec.x * dVy;
        }
        public void Set(FlightPathPoint pt)
        {
            this.vario = pt.vario;
            this.vec = new Vector3d(pt.vec);
            this.speed = pt.speed;
            this.windx = pt.windx;
            this.windy = pt.windy;
            this.timestamp = pt.timestamp;

            this.x = pt.x ;
            this.y = pt.y ;
            this.z = pt.z;
        }

        public void Add(FlightPathPoint pt, Vector3d vecDir, double dVx, double dVy)
        {
            Add(pt, vecDir, Convert.ToSingle(dVx), Convert.ToSingle(dVy));
        }

        public void Add(FlightPathPoint pt, Vector3d vecDir, float dVx, float dVy)
        {
            this.vario = pt.vario;
            this.vec = new Vector3d(pt.vec);
            this.speed = pt.speed;
            this.windx = pt.windx;
            this.windy = pt.windy;
            this.timestamp = pt.timestamp;

            this.x = pt.x + vecDir.x * dVx;
            this.y = pt.y + vecDir.y * dVx;
            this.z = pt.z;
            this.x = this.x - vecDir.y * dVy;
            this.y = this.y + vecDir.x * dVy;
        }

        public Point3d getPosition()
        {
            Point3d pt = new Point3d(x, y, z);
            return pt;
        }
        public Vector3d getWind()
        {
            Vector3d vec = new Vector3d(windx, windy, 0);
            return vec;
        }
        public Vector3d getWindNorm()
        {
            Vector3d vec = new Vector3d(windx, windy, 0);
            vec.normalize();
            return vec;
        }
        public Vector3d getSpeedVector()
        {
            Vector3d vecSpeed = new Vector3d(speed*vec.x, speed*vec.y, 0);
            return vecSpeed;
        }
        public Vector3d getSpeedDirection()
        {
            Vector3d vecSpeed = new Vector3d(vec.x, vec.y, 0);
            return vecSpeed;
        }

        public float distanceTo(Point3d pt2)
        {
            Vector3d vecTemp = new Vector3d(this.getPosition(), pt2);
            return vecTemp.length();
        }
        public Vector3d vectorTo(Point3d pt2)
        {
            Vector3d vecTemp = new Vector3d(this.getPosition(), pt2);
            vecTemp.normalize();
            return vecTemp;
        }

        public float distanceTo(FlightPathPoint pt2, bool bHorizontal = true)
        {
            Vector3d vec = new Vector3d(getPosition(), pt2.getPosition());
            if (bHorizontal)
                vec.z = 0;

            return vec.length();
        }
        public Vector3d vectorTo(FlightPathPoint pt2, bool bHorizontal = true)
        {
            Vector3d vecTemp = new Vector3d(this.getPosition(), pt2.getPosition());
            if (bHorizontal)
                vec.z = 0;
            vecTemp.normalize();
            return vecTemp;
        }
        //public getViewCo(PointF ptEye, PointF vecView, ref PointF)
    }
    public class FlightPath
    {
        public FlightPathPoint[] path = new FlightPathPoint[125];

        public FlightPath()
        {
            for (int i = 0; i < path.Length; i++)
            {
                path[i] = new FlightPathPoint(0, 0, 0, 0, new Vector3d(0,0,0),0, 0, 0, 0);
            }

        }
       public void Init(FlightPathPoint pt)
        {
            for (int i = 0; i < path.Length; i++)
            {
                path[i] = new FlightPathPoint(pt.x, pt.y, pt.z, 0, new Vector3d(0,0,0),0, 0, 0, 0);
            }

        }

        public void Add(FlightPathPoint pt)
        {
            //shift
            for (int i = path.Length-1; i > 0; i--)
            {
                path[i].x = path[i - 1].x;
                path[i].y = path[i - 1].y;
                path[i].z = path[i - 1].z;
                path[i].vario = path[i - 1].vario;
                path[i].vec = path[i - 1].vec;
                path[i].speed = path[i - 1].speed;
                path[i].windx = path[i - 1].windx;
                path[i].windy = path[i - 1].windy;
                path[i].timestamp = path[i - 1].timestamp;
            }
            path[0] = pt;
        }

        public bool HasCrashed()
        {
            if (path[0].timestamp == 0)
                return false;

            if (path[0].speed > 10)
                return false;

            if (path[0].timestamp == path[15].timestamp)
                return false;

            double dist = Math.Abs(path[0].x - path[15].x)
                + Math.Abs(path[0].y - path[15].y)
                + Math.Abs(path[0].z - path[15].z);
            if (dist < 2)
                return true;

            return false;
        }

    }
}
