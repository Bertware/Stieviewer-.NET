using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace be.stieviewer.stievie.Model
{
	public class StievieEpg
	{
		[JsonProperty("ID")]
		public int ChannelId { get; private set; }

		[JsonProperty("Programs")]
		public List<StievieProgram> Programs { get; private set; } 
	}
}
