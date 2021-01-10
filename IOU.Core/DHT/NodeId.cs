using System;
using System.Linq;
using System.Numerics;

namespace IOU.DHT {
	public class NodeId : IEquatable<NodeId> {
		public readonly byte[] Id;

		public NodeId(byte[] id) {
			Id = id;
		}

		public static byte[] XorBytes(byte[] a, byte[] b) {
			var buf = new byte[a.Length];
			a.CopyTo(buf, 0);
			for (var i = 0; i < a.Length; i++)
				buf[i] ^= b[i];
			return buf;
		}

		public bool Equals(NodeId? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id.SequenceEqual(other.Id);
		}

		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((NodeId)obj);
		}

		public override int GetHashCode() {
			return new BigInteger(Id, true).GetHashCode();
		}

		public static bool operator ==(NodeId? left, NodeId? right)
			=> Equals(left, right);

		public static bool operator !=(NodeId? left, NodeId? right)
			=> !Equals(left, right);
	}
}

