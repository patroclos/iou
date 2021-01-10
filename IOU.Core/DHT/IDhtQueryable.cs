using System;
using System.Net;
using System.Threading.Tasks;

namespace IOU.DHT {
	public interface IDhtQueryable {
		Task<BEnc> Query(string query, BDict arguments, IPEndPoint endpoint, TimeSpan? timeout = default);
		Task<T> Query<T>(string query, object arguments, IPEndPoint endpoint, TimeSpan? timeout = default);
		void HandleResponse(BDict response);
	}
}
