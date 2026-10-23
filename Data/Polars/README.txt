Polars for XCSoar - Hangar Update 14

These polars have been computed with data taken from the Condor polar curves.

To install the polars, simply copy the .xcp files into the folder named XCSoarData\Polars 

For each glider. there are two files (except for single seaters whose fixed ballast is 0) 
- glider.xcp     : minimum dry mass (single-seaters : no fixed ballast. two-seaters : one pilot)
- glider-FXB.xcp : maximum dry mass (single-seaters : fixed ballast. two-seaters : two pilots).

With these files, for the same amount of water, the wing loading in XCSoar is identical to 
the one computed in Condor.

Winpilot format polars (.plr) are also available for LK8000

VALIDATION

The comparisons have been made at maximum wind loading, for the following MC settings :
0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0 

The agreement between the calculated polars and the Condor polars is rather good, except for the Duo-Discus whose 
polar drops above 150 kph (at minimum mass). 
It is not possible to approximate such a curve with a parabola and, if we want to have a better agreement at higher 
speeds, it will lead to overestimate the performance at lower speeds, which is in my opinion not desirable. The 
Duo-Discus calculated polar remains good up to a McCready setting of 5 at maximum mass.

For each glider, the comparison between Condor data and the computed polar (at maximum mass) can be found in file 
NAME.png

A summary of the relative RMS errors for Vz [according to MC range] can be found in file FitErrors.txt
