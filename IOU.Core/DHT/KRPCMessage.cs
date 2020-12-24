using System;
using System.Runtime.Serialization;

namespace IOU.DHT
{
    public enum KRPCMessageType
    {
        Query = 'q',
        Response = 'r',
        Error = 'e'
    }

    // dto
    public abstract class KRPCMessage
    {
        [MetaInfoProperty("t")]
        public BStr TransactionId { get; set; }
        
        public abstract KRPCMessageType MessageType { get; }

        [MetaInfoProperty("y")]
        public BStr Type
        {
            get
            {
                switch (MessageType)
                {
                    case KRPCMessageType.Query:
                        return "q";
                    case KRPCMessageType.Response:
                        return "r";
                    case KRPCMessageType.Error:
                        return "e";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(MessageType));
                }
            }
        }

        [MetaInfoProperty("v")]
        public BStr Version { get; set; }
    }

    public class KRPCQuery : KRPCMessage
    {
        public override KRPCMessageType MessageType => KRPCMessageType.Query;

        [MetaInfoProperty("q")]
        public BStr Query { get; set; }

        [MetaInfoProperty("a")]
        public BDict Arguments { get; set; }
    }

    public class KRPCResponse : KRPCMessage
    {
        public override KRPCMessageType MessageType => KRPCMessageType.Response;
        
        [MetaInfoProperty("r")]
        public BDict Response { get; set; }
    }

    public class KRPCError : KRPCMessage
    {
        public override KRPCMessageType MessageType => KRPCMessageType.Error;
        
        [MetaInfoProperty("e")]
        public BLst Error { get; set; }
    }

    namespace Queries
    {
        public struct QueryContext
        {
            public readonly BStr TransactionId;
            public readonly BStr Version;

            public QueryContext(BStr transactionId, BStr version)
            {
                TransactionId = transactionId;
                Version = version;
            }
        }

        public interface IQuery
        {
            KRPCMessage ToDto(QueryContext ctx);
        }

        public class Ping : IQuery
        {
            [MetaInfoProperty("id")]
            public BStr Id { get; set; }

            public KRPCMessage ToDto(QueryContext ctx)
            {
                return new KRPCQuery
                {
                    TransactionId = ctx.TransactionId,
                    Version = ctx.Version,
                    Query = "ping",
                    Arguments = MetaInfoSerializer.Serialize(this) as BDict,
                };
            }
        }

        public class FindNode
        {
            [DataMember(Name = "id")] public BStr Id;
            [DataMember(Name = "target")] public BStr Target;

            public struct Response
            {
                [DataMember(Name = "id")] public BStr Id;

                [DataMember(Name = "nodes")] public BStr Nodes;
            }
        }

        public class GetPeers
        {
        }

        public class AnnouncePeer
        {
        }
    }
}