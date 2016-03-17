using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using be.stieviewer.stievie.Model;
using be.stieviewer.stievie.Properties;
using Newtonsoft.Json;

namespace be.stieviewer.stievie
{

	public class Stievie
	{
		const string M3U8Base = "https://playlistsvr-stievie.triple-it.nl:443/";
		const string BaseUrlV2 = "https://vinson-stievie.triple-it.nl/V2Api/";
		const string BaseUrl = "https://vinson-stievie.triple-it.nl/V1Api/";

		const string VinsonSalt = "g6TTAK7kiL6tusOEfwje";
		// salt used for SHA1 hash in url signing , let's call it a lucky guess :-)

		const string AndroidUserAgent = "Dalvik/1.6.0 (Linux; U; Android 4.4.4; Nexus 7 Build/KTU84P)";
		const string ApiKey = "androidprod";
		const string DeviceType = "asus - Nexus 7";

		private readonly string _deviceId;
		private string _authHash;
		private int _authHashValidUntil;

		private List<StievieChannel> _channels;
		private List<StievieEpg> _epg;
		private int _channelsLastUpdated;
		private int _epgLastUpdated;

		/// <summary>
		/// Singleton constructor
		/// </summary>
		private Stievie()
		{
			// Load settings
			_deviceId = Settings.Default.deviceID;
			_authHash = Settings.Default.authHash;
			_authHashValidUntil = Settings.Default.authHashValidUntil;

			// Create guid (v4) if not set
			if (string.IsNullOrEmpty(_deviceId))
			{
				Guid id = Guid.NewGuid();
				_deviceId = id.ToString();
				UpdateSettings();
			}
		}

		private static Stievie _instance;

		/// <summary>
		/// Get an instance of Stievie. Stievie can only be instantiated once.
		/// </summary>
		/// <returns>An exisiting instance, if any. If no instance exists, one will be created</returns>
		public static Stievie GetInstance()
		{
			if (_instance == null) _instance = new Stievie();
			return _instance;
		}



		/// <summary>
		/// Wether or not a valid auth hash is available.
		/// </summary>
		/// <returns></returns>
		public bool HasValidAuthHash()
		{
			return _authHashValidUntil > GetEpochTime();
		}

		/// <summary>
		/// Sign data before sending it to the stievie API. Required.
		/// </summary>
		/// <param name="url">The API endpoint for the request (excluding base url)</param>
		/// <param name="data">The POST data to sign</param>
		/// <returns>Signed data</returns>
		public static string SignVinsonRequestData(string url, string data)
		{
			data = data.TrimEnd('&');
			string signdata = data + "&" + url + VinsonSalt;
			string signature = Sha1(signdata);
			data += "&sig=" + signature;
			return data;
		}

		/// <summary>
		/// Hash an input string using sha1
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns>The hashed result</returns>
		public static string Sha1(string input)
		{
			using (SHA1Managed sha1 = new SHA1Managed())
			{
				var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
				var sb = new StringBuilder(hash.Length * 2);

				foreach (byte b in hash)
				{
					// can be "x2" if you want lowercase
					sb.Append(b.ToString("x2"));
				}

				return sb.ToString();
			}
		}
		/// <summary>
		/// Sign time (UNIX epoch, seconds)
		/// </summary>
		/// <returns>Unix Epoch timestamp</returns>
		private static int GetEpochTime()
		{
			TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
			return (int) t.TotalSeconds;
		}

		/// <summary>
		/// Sign in to stievie. Once signed in, an auth hash is stored in the settings to remember the login.
		/// </summary>
		/// <param name="login">Email address</param>
		/// <param name="password">Password</param>
		/// <returns></returns>
		public StievieLoginResult Signin(string login, string password)
		{
			if (HasValidAuthHash())
			{
				Debug.WriteLine("Skip Sign in, we already have a valid authHash");
				LogOn();
				return new StievieLoginResult(true, _authHash, _authHashValidUntil);
			}

			Debug.WriteLine("Signing in");
			string url = "User/SignIn";
			string postdata = "apiKey=" + ApiKey +
			                  "&deviceid=" + _deviceId +
			                  "&devicetype=" + DeviceType +
			                  "&password=" + password +
			                  "&sigtime=" + GetEpochTime() +
			                  "&username=" + login;

			postdata = SignVinsonRequestData(url, postdata);
			url = BaseUrl + url;

			string response = RetrievePostData(url, postdata);
			/*
			 {
				  "ResponseCode": 200,
				  "ResponseKey": "OK",
				  "ResponseObject": {
					"result": true,
					"authhash": "5f86281ef5bf45739c4a02aaa339acb4",
					"validUntil": 1463332850
				  },
				  "ResponseTimestamp": 1458148850
				}
			 */
			dynamic json = JsonConvert.DeserializeObject(response);
			int responseCode = (int) json["ResponseCode"];
			dynamic responseObject = json["ResponseObject"];

			if (responseCode != 200)
			{
				throw new InvalidCredentialException("Failed to login (" + responseCode +
				                                     "). Are the username and password correct?");
			}

			StievieLoginResult result = responseObject.ToObject(typeof (StievieLoginResult));
			if (!result.Result)
			{
				throw new InvalidCredentialException("Login unsuccessful. Are the username and password correct?");
			}

			_authHash = result.AuthHash;
			_authHashValidUntil = result.AuthHashValidUntil;
			UpdateSettings();
			return result;
		}

		/// <summary>
		/// Register with the stievie API to revalidate the auth key. If the auth key is still valid, this method can be used instead of login.
		/// </summary>
		public void LogOn()
		{
			string endPoint = "User/LogOn";

			string data = "apikey=" + ApiKey + "&";
			data += "authhash=" + _authHash + "&";
			data += "deviceid=" + _deviceId + "&";
			data += "devicetype=" + DeviceType + "&";
			data += "sigtime=" + GetEpochTime() + "&";

			string url = BaseUrl + endPoint;
			data = SignVinsonRequestData(endPoint, data);
			string response = RetrievePostData(url, data);

			dynamic json = JsonConvert.DeserializeObject(response);
			int responseCode = (int) json["ResponseCode"];


			if (responseCode == 403)
			{
				LogOut();
				throw new InvalidCredentialException("Session timed out");
			}

			if (responseCode != 200)
			{
				throw new InvalidCredentialException("Failed to logon (" + responseCode +
				                                     "). Are the username and password correct?");
			}

			GetChannels();
			GetEpg();
		}

		/// <summary>
		/// Logout and forget credentials
		/// </summary>
		public void LogOut()
		{
			_authHash = "";
			_authHashValidUntil = 0;
			UpdateSettings();
		}

		private void UpdateSettings()
		{
			Settings.Default.authHash = _authHash;
			Settings.Default.authHashValidUntil = _authHashValidUntil;
			Settings.Default.deviceID = _deviceId;
			Settings.Default.Save();
		}

		// 30 minutes to refresh channels
		readonly Timer _dataRefresh = new Timer(30*60*1000);
		/// <summary>
		/// Get a list of all available channels with their information (like stream info)
		/// </summary>
		/// <param name="forceUpdate">Ignore cached data</param>
		/// <returns></returns>
		public List<StievieChannel> GetChannels(bool forceUpdate = false)
		{
			if (!forceUpdate && _channels != null && _channels.Count > 0 && _channelsLastUpdated > GetEpochTime() - (60*30))
			{
				return _channels;
			}

			string endPoint = "Channel/GetChannelsWithStreams";

			string data = "apikey=" + ApiKey + "&";
			data += "authhash=" + _authHash + "&";
			data += "deviceid=" + _deviceId + "&";
			data += "devicetype=" + DeviceType + "&";
			data += "includeofflinechannels=true&";
			data += "sigtime=" + GetEpochTime() + "&";
			// ENABLE HD HERE: SD/HD
			data += "streamType=hd&";

			data = SignVinsonRequestData(endPoint, data);
			string url = BaseUrl + endPoint;
			string response = RetrievePostData(url, data);

			dynamic json = JsonConvert.DeserializeObject(response);
			int responseCode = (int) json["ResponseCode"];


			if (responseCode != 200)
			{
				throw new InvalidCredentialException("Failed to retrieve channels (" + responseCode + ").");
			}

			dynamic responseObject = json["ResponseObject"];
			dynamic channels = responseObject["Channels"];

			if (!_dataRefresh.Enabled)
			{
				_dataRefresh.Start();
				_dataRefresh.AutoReset = true;
				_dataRefresh.Elapsed += UpdateData;
			}

			List<StievieChannel> channelList = channels.ToObject(typeof (List<StievieChannel>));
			_channels = channelList;
			_channelsLastUpdated = GetEpochTime();
			return channelList;
		}
		/// <summary>
		/// Refresh channel and epg lists. For timer.elapsed events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateData(object sender, ElapsedEventArgs e)
		{
			GetChannels(true);
			GetEpg(true);
		}
		/// <summary>
		/// Get the EPGs (Electronic Program Guides) for all channels. 1 EPG shows information about 1 channel.
		/// </summary>
		/// <param name="forceUpdate">Ignore cached data</param>
		/// <returns>List of EPGs for all channels</returns>
		public List<StievieEpg> GetEpg(bool forceUpdate = false)
		{
			if (_epg != null && !forceUpdate && _epgLastUpdated > GetEpochTime() - (60*30))
			{
				return _epg;
			}

			if (_channels == null)
			{
				GetChannels();
			}

			if (_channels == null)
			{
				return null;
			}

			string channelIds = "";
			foreach (StievieChannel channel in _channels)
			{
				channelIds += channel.Id + ",";
			}
			channelIds = channelIds.TrimEnd(',');

			string endPoint = "Program/GetOverview";
			string data = "apikey=" + ApiKey + "&";
			data += "authhash=" + _authHash + "&";
			data += "channelID=" + channelIds + "&";
			data += "deviceid=" + _deviceId + "&";
			data += "devicetype=" + DeviceType + "&";
			data += "sigtime=" + GetEpochTime() + "&";

			data = SignVinsonRequestData(endPoint, data);

			// this is a V2 API call
			string url = BaseUrlV2 + endPoint;

			string response = RetrievePostData(url, data);
			dynamic json = JsonConvert.DeserializeObject(response);
			int responseCode = (int) json["ResponseCode"];


			if (responseCode != 200)
			{
				throw new InvalidCredentialException("Failed to retrieve EPG (" + responseCode + ").");
			}

			dynamic responseObject = json["ResponseObject"];

			dynamic channels = responseObject["Channels"];
			List<StievieEpg> epg = channels.ToObject(typeof (List<StievieEpg>));

			List<String> serieIDs = responseObject["SeriesIDs"].ToObject(typeof (List<String>));
			List<String> series = responseObject["SeriesNames"].ToObject(typeof (List<String>));
			List<String> names = responseObject["Names"].ToObject(typeof (List<String>));
			List<String> urls = responseObject["Urls"].ToObject(typeof (List<String>));
			List<String> tags = responseObject["Tags"].ToObject(typeof(List<String>));

			foreach (StievieEpg stievieEpg in epg)
			{
				foreach (StievieProgram program in stievieEpg.Programs)
				{
					// switch name id to name
					program.Name = names[Int32.Parse(program.Name)];
					if (!string.IsNullOrEmpty(program.SeriesId))
					{
						program.SeriesId = serieIDs[Int32.Parse(program.SeriesId)];
					}
					if (!string.IsNullOrEmpty(program.SeriesId))
					{
						program.SeriesName = series[Int32.Parse(program.SeriesName)];
					}
					if (!string.IsNullOrEmpty(program.Url))
					{
						program.Url = urls[Int32.Parse(program.Url)];
					}
					if (!string.IsNullOrEmpty(program.Tag))
					{
						program.Tag = tags[Int32.Parse(program.Tag)];
					}
				}
			}
			_epg = epg;
			_epgLastUpdated = GetEpochTime();
			return _epg;
		}
		
		/// <summary>
		/// Get an m3u8 playlist file.
		/// </summary>
		/// <param name="url">URL endpoint to retrieve</param>
		/// <returns>the playlist contents/returns>
		/// <remarks>The original stieviewer replaced urls to redirect to a proxy server, so everything was handled by stieviewer. Original modifications:
		///     body = string.replaceAll(body,"/Default.m3u8","/api/m3u8/Default.m3u8");
        ///     body = string.replaceAll(body,"http://stievie","/api/proxy/http://stievie");
		/// </remarks>
		public string GetM3U8(string url)
		{
			return RetrieveGetData(M3U8Base + url);
		}

		/// <summary>
		/// Retrieve GET data, emulating the stievie app.
		/// </summary>
		/// <param name="url">URL to retrieve</param>
		/// <returns>retrieved data</returns>
		private string RetrieveGetData(string url)
		{
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
			req.Method = "GET";
			req.UserAgent = AndroidUserAgent;

			WebResponse resp = req.GetResponse();
			Stream s = resp.GetResponseStream();
			if (s == null) return null;
			StreamReader sr =
				new StreamReader(s);
			return sr.ReadToEnd().Trim();
		}

		/// <summary>
		/// Retrieve POST data, emulating the stievie app.
		/// </summary>
		/// <param name="url">URL to retrieve</param>
		/// <param name="data">data to send in body</param>
		/// <returns>retrieved data</returns>
		private string RetrievePostData(string url, string data)
		{
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
			req.Method = "POST";
			req.UserAgent = AndroidUserAgent;
			req.ContentType = "application/x-www-form-urlencoded";

			byte[] bytes = Encoding.ASCII.GetBytes(data);
			req.ContentLength = bytes.Length;
			Stream os = req.GetRequestStream();
			os.Write(bytes, 0, bytes.Length); //Push it out there
			os.Close();

			WebResponse resp = req.GetResponse();
			Stream s = resp.GetResponseStream();
			if (s == null) return null;
			StreamReader sr =
				new StreamReader(s);
			return sr.ReadToEnd().Trim();
		}
	}
}