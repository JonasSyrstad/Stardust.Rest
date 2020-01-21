using System.Net;

namespace Stardust.Interstellar.Rest.Extensions
{
	public class InterseptorStatus
	{
		public bool Cancel { get; set; }

		public string CancellationMessage { get; set; }

		public HttpStatusCode StatusCode { get; set; }
	}
}