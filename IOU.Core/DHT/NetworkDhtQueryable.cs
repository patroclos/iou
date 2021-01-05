using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IOU.DHT
{
	public class DhtQueryTimeoutException : TimeoutException
	{
		public string QueryName { get; }
		public BDict Arguments { get; }
		public TimeSpan Timeout { get; }

		public DhtQueryTimeoutException(string query, BDict arguments, TimeSpan timeout)
		{
			this.QueryName = query;
			this.Arguments = arguments;
			this.Timeout = timeout;
		}
	}

	public class NetworkDhtQueryable : IDhtQueryable
	{
		private readonly UdpClient _client;
		private int _lastTransactionId;

		private readonly List<(byte[], TaskCompletionSource<BEnc>)> _pending
			= new List<(byte[], TaskCompletionSource<BEnc>)>();

		public NetworkDhtQueryable(UdpClient client)
		{
			_client = client;
		}

		private byte[] NewTransaction
		{
			get
			{
				_lastTransactionId++;
				return new byte[] {
					(byte)(_lastTransactionId >> 8),
					(byte)(_lastTransactionId & 0xff),
				};
			}
		}

		public async Task<BEnc> Query(string query, BDict arguments, IPEndPoint node, TimeSpan? timeout = null)
		{
			var trn = NewTransaction;
			var args = MetaInfoSerializer.Serialize(new
			{
				y = "q",
				q = query,
				a = arguments,
				v = new byte[4],
				t = trn
			})!;
			var encoded = args.ToByteArray();

			var tcs = new TaskCompletionSource<BEnc>();
			_pending.Add((trn, tcs));

			try
			{
				await _client.SendAsync(encoded, encoded.Length, node);
				var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(3);
				var timeoutT = Task.Delay(effectiveTimeout);

				await Task.WhenAny(tcs.Task, timeoutT);

				if (timeoutT.IsCompleted)
					throw new DhtQueryTimeoutException(query, arguments, effectiveTimeout);

				return tcs.Task.Result;
			}
			finally
			{
				_pending.RemoveAll(kv => kv.Item1.SequenceEqual(trn));
			}
		}

		public async Task<T> Query<T>(string query, object arguments, IPEndPoint endpoint, TimeSpan? timeout = null)
			=> (await Query(
						query,
						MetaInfoSerializer.Serialize(arguments) as BDict ?? throw new ArgumentOutOfRangeException(nameof(arguments)),
						endpoint,
						timeout)
					).Value<T>();

		public void HandleResponse(BDict response)
		{
			if (response["y"]?.Value<string>() != "r")
				throw new ArgumentOutOfRangeException(nameof(response), $"{response} is not a response");

			var responseData = response["r"];
			if (responseData == null)
				throw new ArgumentOutOfRangeException(nameof(response), $"{response} has no response data");

			var trn = response["t"]!.Value<byte[]>();
			var completer = _pending
				.FirstOrDefault(kv => kv.Item1.SequenceEqual(trn))
				.Item2;

			if (completer == null)
				return;

			completer.TrySetResult(responseData);
		}
	}
}
