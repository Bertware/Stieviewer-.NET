using System.Collections.Generic;
using Newtonsoft.Json;

namespace be.stieviewer.stievie.Model
{
	public class StievieChannel
	{
		[JsonProperty("ID")]
		public int Id { get; private set; }

		[JsonProperty("Name")]
		public string Name { get; private set; }

		[JsonProperty("Position")]
		public int Position { get; private set; }

		[JsonProperty("DarkIcon")]
		public string DarkIconUrl { get; private set; }

		[JsonProperty("LightIcon")]
		public string LightIconUrl { get; private set; }

		[JsonProperty("HashTags")]
		public string HashTags { get; private set; }

		[JsonProperty("SpecificThumbnailUrl")]
		public string SpecificThumbnailUrl { get; private set; }

		[JsonProperty("ThumbnailDelay")]
		public int ThumbnailDelay { get; private set; }

		[JsonProperty("DisplayDelay")]
		public int DisplayDelay { get; private set; }

		[JsonProperty("AllowForwardTimeshift")]
		public bool AllowForwardTimeshift { get; private set; }

		public string ThumbSmallUrl
		{
			get { return SpecificThumbnailUrl; }
		}

		public string ThumbLargeUrl
		{
			get { return SpecificThumbnailUrl.Replace(".jpg", "l.jpg"); }
		}

		public string ThumbOriginalUrl
		{
			get { return SpecificThumbnailUrl.Replace(".jpg", "o.jpg"); }
		}

		public string ThumbJsonUrl
		{
			get { return SpecificThumbnailUrl.Replace(".jpg", ".json"); }
		}

		[JsonProperty("Streams")]
		public IList<StievieStream> Streams { get; private set; }

		
	}
}