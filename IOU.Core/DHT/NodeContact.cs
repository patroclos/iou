using System.Net;

namespace IOU.DHT
{
    public struct NodeContact
    {
        public NodeId Id { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }
}
