using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    class UTMData
    {
//  https://gis.stackexchange.com/questions/43743/extracting-elevation-from-hgt-file
private static int SECONDS_PER_MINUTE = 60;

        // alter these values for different SRTM resolutions
        public static int HGT_RES = 1; // resolution in arc seconds
        public static int HGT_ROW_LENGTH = 3601; // number of elevation values per line
        public static int HGT_VOID = -32768; // magic number which indicates 'void data' in HGT file  


        public static void main(string[] args) 
        {
    double coorLat = -22.352951806595854;
        double coorLon = -42.43121349392618;

        string filePath = "C:/Magno/DEMRJ/";
        string fileName = getHgtFileName(coorLat, coorLon);

        string htgFile = filePath + fileName;

            //.out.println(htgFile );

            //File f = new File( htgFile );

          //  ShortBuffer data = readHgtFile(htgFile);
           // ShortBuffer data = readHgtFile(htgFile);

            double fLat = frac(Math.Abs(coorLat)) * SECONDS_PER_MINUTE;
        double fLon = frac(Math.Abs(coorLon)) * SECONDS_PER_MINUTE;

        //System.out.println("fLat: " + fLat + "   fLon: " + fLon);

        int row = (int)Math.Round(fLat * SECONDS_PER_MINUTE / HGT_RES);
        int col = (int)Math.Round(fLon * SECONDS_PER_MINUTE / HGT_RES);


        row = HGT_ROW_LENGTH - row;
    int cell = (HGT_ROW_LENGTH * (row - 1)) + col;

        //System.out.println("Read SRTM elevation data from row/col/cell " + row + "," + col + ", " + cell + " == " + data.limit() );

    // valid position in buffer?
//    if (cell<data.limit() ) {

//        short ele = data.get(cell);
//        //System.out.println("==> Read SRTM elevation data from row/col/cell " + row + "," + col + ", " + cell + " = " + ele);

//        // check for data voids
//        if (ele == HGT_VOID) {
//            //System.out.println("No Elevation - VOID");
//    } else {
//            //System.out.println("Elevation : " + ele );
//}

//    } else {
//        //System.out.println("No Elevation - Out of Range");
//    }        


}


/*private static ShortBuffer readHgtFile(string file) 
{

    FileChannel fc = null;
    ShortBuffer sb = null;
    try {
        // Eclipse complains here about resource leak on 'fc' - even with 'finally' clause???
        fc = new FileInputStream(file).getChannel();
        // choose the right endianness

        ByteBuffer bb = ByteBuffer.allocateDirect((int)fc.size());
        while (bb.remaining() > 0) fc.read(bb);

        bb.flip();
        //sb = bb.order(ByteOrder.LITTLE_ENDIAN).asShortBuffer();
        sb = bb.order(ByteOrder.BIG_ENDIAN).asShortBuffer();
    } finally {
        if (fc != null) fc.close();
    }

    return sb;
}

*/


public static string getHgtFileName(double latD, double lonD)
{
    int lat = (int)latD;
    int lon = (int)lonD;
    string latPref = "N";
    if (lat < 0) latPref = "S";

    string lonPref = "E";
    if (lon < 0)
    {
        lonPref = "W";
    }
    string lonT = lon.ToString().Replace("-", "");
    if (lonT.Length == 2)
    {
        lonT = "0" + lonT;
    }
    string ret = latPref + lat + lonPref + lonT + ".hgt";
    return ret.Replace("-", "");
}


public static double frac(double d)
{
    long iPart;
    double fPart;
    iPart = (long)d;
    fPart = d - iPart;
    return fPart;
}        }
}
