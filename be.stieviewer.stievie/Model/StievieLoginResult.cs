using Newtonsoft.Json;

namespace be.stieviewer.stievie.Model
{
	public class StievieLoginResult
	{
		[JsonProperty("result")]
		public bool Result { get; private set; }

		[JsonProperty("authHash")]
		public string AuthHash { get; private set; }

		[JsonProperty("validUntil")]
		public int AuthHashValidUntil { get; private set; }

		public StievieLoginResult()
		{
		}

		internal StievieLoginResult(bool success, string authHash, int authHashValidUntil)
		{
			this.Result = success;
			this.AuthHash = authHash;
			this.AuthHashValidUntil = authHashValidUntil;
		}
	}
}