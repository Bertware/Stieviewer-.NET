
using Newtonsoft.Json;

namespace be.stieviewer.stievie.Model
{
	public class StievieStream
	{
		[JsonProperty("ID")]
		public int Id { get; private set; }
		public string Url { get; private set; }
		public int OffsetFromNow { get; private set; }
	}
}