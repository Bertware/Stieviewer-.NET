using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace be.stieviewer.stievie.Model
{
	/// <summary>
	/// A stievie program. This is part of an EPG. 
	/// <remarks>
	/// Name, SeriesName, SeriesId, Url are first loaded from json as integer id's. Once loaded they're instantly replaced by their actual name.
	/// </remarks>
	/// </summary>
	public class StievieProgram
	{
		/// <summary>
		/// Program Id
		/// </summary>
		[JsonProperty("ID")]
		public int Id { get; private set; }

		/// <summary>
		/// Program Name
		/// </summary>
		[JsonProperty("Name")]
		public string Name { get; internal set; }

		/// <summary>
		/// Series name. Optional.
		/// </summary>
		[JsonProperty("SeriesName")]
		public string SeriesName { get; internal set; }
		
		/// <summary>
		/// Series Id. Optional.
		/// </summary>
		[JsonProperty("SeriesID")]
		public string SeriesId { get; internal set; }

		/// <summary>
		/// Broadcast time
		/// </summary>
		[JsonProperty("Time")]
		public int Time { get; private set; }

		/// <summary>
		/// Desc field. Not sure what this does.
		/// </summary>
		[JsonProperty("Desc")]
		public string Description { get; internal set; }

		/// <summary>
		/// Hashtag
		/// </summary>
		[JsonProperty("Tag")]
		public string Tag { get; internal set; }

		/// <summary>
		/// RBlackouts field. Not sure what this does.
		/// </summary>
		[JsonProperty("RBlackouts")]
		public int RBlackouts { get; private set; }
		/// <summary>
		/// VideoLinks field. Not sure what this does.
		/// </summary>
		[JsonProperty("VideoLinks")]
		public List<String> VideoLinks { get; private set; }
		/// <summary>
		/// Program url
		/// </summary>
		[JsonProperty("Url")]
		public string Url { get; internal set; }
	}
}