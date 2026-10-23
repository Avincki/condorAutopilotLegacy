using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Drawing;



namespace CondorAutoPilot
{
	class GoogleRequest
	{
		private static string GoogleApiKey = "YOUR_GOOGLE_API_KEY";

		public class GooglePlaceResponseJson
		{
			public string status { get; set; }
			public resultsPlaceJson[] results { get; set; }
		}

		public class GoogleElevationResponseJson
		{
			public string status { get; set; }
			public resultsElevationsJson[] results { get; set; }
		}

		public class resultsPlaceJson
		{
			public geometryJson geometry { get; set; }
			public string name { get; set; }
			public photoJson[] photos { get; set; }

		}

		public class geometryJson
		{
			public locationJson location { get; set; }
		}

		public class locationJson
		{
			public string lat { get; set; }
			public string lng { get; set; }
		}

		public class photoJson
		{
			public string height { get; set; }
			public string width { get; set; }
			public string photo_reference { get; set; }
		}
		public class photo
		{
			public string photo_reference { get; set; }
			public string name { get; set; }

			public float lat { get; set; }
			public float lng { get; set; }
		}

		public class resultsElevationsJson
		{
			public string elevation { get; set; }

			public locationJson location { get; set; }
		}

		/*
				  "geometry" : {
					"location" : {
					   "lat" : 46.0207133,
					   "lng" : 7.749117000000001
					},
					"viewport" : {
					   "northeast" : {
						  "lat" : 46.0642799,
						  "lng" : 7.90881
					   },
					   "southwest" : {
						  "lat" : 45.91689,
						  "lng" : 7.5750799
					   }
					}
				 },
				 "icon" : "https://maps.gstatic.com/mapfiles/place_api/icons/geocode-71.png",
				 "id" : "bf57762a47f6a05802fb964e42100af1d541c669",
				 "name" : "Zermatt",
				 "photos" : [
					{
					   "height" : 495,
					   "html_attributions" : [
						  "\u003ca href=\"https://maps.google.com/maps/contrib/104949121987198987785\"\u003eVladimir Iliev\u003c/a\u003e"
					   ],
					   "photo_reference" : "CmRaAAAA1usVpBfSeXhmq_L-G22QBWLSGE2SzIcVfZ_cwWXb_Eeh1czZiv8KKnhYNvAlcv5O2KZITWRjb6Dpi1QLNLe1iRkpLYRzSed4j1p1cdh5xmGB_1latt4LGUuNS3lHp2PGEhC-ZYp9BrZdqhZ_583CaFBwGhRyyk2D5ygxVYVVpLgV8RcQ0VzpsA",
					   "width" : 768
					}
				 ],
				 "place_id" : "ChIJzeUuKaI1j0cRcG8ZQIj_AAQ",
				 "reference" : "ChIJzeUuKaI1j0cRcG8ZQIj_AAQ",
				 "scope" : "GOOGLE",
				 "types" : [ "locality", "political" ],
				 "vicinity" : "Zermatt"
			  },* 
		 * */
		private static List<string> strUsedPhotos = new List<string>();
		private static int locationRadius = 5000;

		public static bool IsNewPhoto(string strPhoto)
        {
			for (int i  = 0; i < strUsedPhotos.Count(); i++)
            {
				if (strUsedPhotos[i] == strPhoto)
					return false;
            }
			return true;
        }

		public static bool AddToUsedPhotos(string strPhoto)
		{
			if (strUsedPhotos.Count() > 10)
            {
				strUsedPhotos.RemoveAt(0);
			}
			strUsedPhotos.Add(strPhoto);
			return true;
		}


		public static bool GetElevation(float lat, float lng, ref float dElevation)
		{
			string url = String.Format("https://maps.googleapis.com/maps/api/elevation/json?location={0},{1}&key={2}",
					lat.ToString(), lng.ToString(),
					GoogleRequest.GoogleApiKey);
			string urlResponse = URLRequest(url);
			if (urlResponse == null)
				return false;

			GoogleElevationResponseJson jsonResult = JsonConvert.DeserializeObject<GoogleElevationResponseJson>(urlResponse); //deserializing the result to GoogleElevCodeResponse

			if (jsonResult.status != "OK" || jsonResult.results.Length == 0)
			{
				dElevation = -1000;
				return false;
			}

			dElevation = Convert.ToSingle(jsonResult.results[0].elevation);
			return true;
		}

		public static bool GetElevations(float lat1, float lng1, float lat2, float lng2, int iSamples, ref List<float> dElevations)
		{
			dElevations.Clear();

			string url = String.Format("https://maps.googleapis.com/maps/api/elevation/json?location={0},{1}|{2},{3}&samples={4}&key={5}",
					lat1.ToString(), lng1.ToString(),
					lat2.ToString(), lng2.ToString(),
					iSamples.ToString(),
					GoogleRequest.GoogleApiKey);
			string urlResponse = URLRequest(url);
			if (urlResponse == null)
				return false;

			GoogleElevationResponseJson jsonResult = JsonConvert.DeserializeObject<GoogleElevationResponseJson>(urlResponse); //deserializing the result to GoogleElevCodeResponse

			if (jsonResult.status != "OK" || jsonResult.results.Length == 0)
			{

				return false;
			}

			for (int i = 0; i < jsonResult.results.Length; i++)
			{
				float dElevation = Convert.ToSingle(jsonResult.results[0].elevation);
				dElevations.Add(dElevation);
			}
			return true;
		}


		public static bool GetPhotoList(float lat, float lng, ref List<photo> photos)
		{

			photos.Clear();
			string url = String.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius={2}&type=natural_feature&key={3}",
					lat.ToString(), lng.ToString(),
					locationRadius,
					GoogleRequest.GoogleApiKey);
			string urlResponse = URLRequest(url);
			if (urlResponse == null)
				return false;


			GooglePlaceResponseJson jsonResult = JsonConvert.DeserializeObject<GooglePlaceResponseJson>(urlResponse); //deserializing the result to GoogleElevCodeResponse

			if (jsonResult.status != "OK" || jsonResult.results.Length == 0)
            {
				locationRadius *= 2;
				return false;
			}
				
			for (int i = 0; i < jsonResult.results.Length; i++)
			{
				if (jsonResult.results[i].geometry.location != null)
                {
					float dLat, dLng;
					dLat = Convert.ToSingle(jsonResult.results[i].geometry.location.lat);
					dLng = Convert.ToSingle(jsonResult.results[i].geometry.location.lng);
					if (jsonResult.results[i].photos != null)
					{
						for (int j = 0; j < jsonResult.results[i].photos.Length; j++)
						{
							if (Convert.ToSingle(jsonResult.results[i].photos[j].width) > 1.2f * Convert.ToSingle(jsonResult.results[i].photos[j].height))
							{
								photo myphoto = new photo();
								myphoto.photo_reference = jsonResult.results[i].photos[j].photo_reference;
								myphoto.lat = dLat;
								myphoto.lng = dLng;

								if (IsNewPhoto(myphoto.photo_reference))
								{
									if (jsonResult.results[i].name != null)
									{
										myphoto.name = jsonResult.results[i].name;
										photos.Add(myphoto);
									}
								}
							}
						}
					}

				}
			}
			if (photos.Count < 2)
            {
				locationRadius += 1000;
			}else if (photos.Count > 10)
            {
				locationRadius = Math.Max(locationRadius/2, 5000);
			}

			return photos.Count > 0;

		}

		public static string URLRequest(string url)
		{
			// Prepare the Request
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

			// Set method to GET to retrieve data
			request.Method = "GET";
			request.Timeout = 2000; //60 second timeout
			request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0)";

			string responseContent = null;

			try
			{
				using (WebResponse response = request.GetResponse())
				{
					// Retrieve a handle to the Stream
					using (Stream stream = response.GetResponseStream())
					{
						// Begin reading the Stream
						using (StreamReader streamreader = new StreamReader(stream))
						{
							// Read the Response Stream to the end
							responseContent = streamreader.ReadToEnd();
						}
					}
				}
			}
			catch (WebException ex)
			{
				//  Handle a WebException, such as trying to write to a "locked" file on the network
			}
			catch (Exception ex)
			{
				//  Handle a regular Exception
			}           // Get the Response

			return (responseContent);
		}


		public static bool SetRadiusText(ref System.Windows.Forms.RichTextBox richTextBox)
		{
			richTextBox.AppendText(Environment.NewLine + "Radius = " + locationRadius.ToString());
			richTextBox.ScrollToCaret();

			return true;
		}
		public static bool SetPhoto(System.Windows.Forms.PictureBox pictureBox,
			System.Windows.Forms.TextBox textBox,
			System.Windows.Forms.RichTextBox richTextBox,
			photo photoRef)
        {

			string url = String.Format("https://maps.googleapis.com/maps/api/place/photo?maxwidth=500&maxheight=500&photoreference={0}&key={1}", photoRef.photo_reference, GoogleRequest.GoogleApiKey);

            try
            {
				var request = WebRequest.Create(url);

				using (var response = request.GetResponse())
					if (response!= null)
                    {
						using (var stream = response.GetResponseStream())
						{
							if (stream != null)
                            {
								Image imageStream = Bitmap.FromStream(stream);
								pictureBox.Image = imageStream;
								pictureBox.Left = 790 - imageStream.Width / 2;
								pictureBox.Width = imageStream.Width;
								pictureBox.Top = 495;
								pictureBox.Height = imageStream.Height;

								textBox.Text = photoRef.name;
								AddToUsedPhotos(photoRef.photo_reference);

								richTextBox.AppendText(Environment.NewLine + "New photo " + photoRef.name);
								richTextBox.ScrollToCaret();
							}
						}
					}
			}
			catch (Exception)
            {

				return false;
			}
			return true;
        }
	}
}
