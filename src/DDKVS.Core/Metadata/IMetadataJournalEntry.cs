using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace DDKVS.Core.Metadata
{

    interface IMetadataJournal
    {

    }



    public interface IStateMachine
    {
        IEnumerable<INodeInfo> GetNodes();
        INodeInfo GetNode(Guid id);
        IEnumerable<INodeInfo> GetNodes(uint bucket);

        INodeInfo Leader { get; }
        INodeInfo Self { get; }

        void AddNode(INodeInfo nodeInfo);

        IEnumerable<JournaledCommand> GetJournalEntries(int startSerialNumber = 0);
    }
    public enum JournalCommand : uint
    {
        AddBucketsToNode = 0, // Add listed buckets to specified node
        AddNode = 1, // Admin approves pending node, node ID, name and base URI(s) given in journal entry.
        BlockNode = 2, // Admin blocked pending node.
        NodeContactUpdate = 3, // Node came online, provided updated URI(s) (Operation - Clear Uris in state, repopulate with provided set of uris)
        NodeNotifyBucketSynchronized = 4, // Node has replicated the data for a bucket that was recently added, and the bucket can now be
                                          // re-evaluated by the cluster leader, and removed from another node if the overseer desires (provided
                                          // that current replication goals are met.)
        RemoveBucketsFromNode = 5, // Remove bucket from a node. This does not delete data, merely updates the state machine.
                                   // The node will reclaim space from this bucket at some point in the future via a garbage collection
                                   // mechanism
        NodeStateUpdate = 6, // Notify the cluster about new information about a node.
        NodeClearBuckets = 7, // Remove all buckets from a node
    }
    public class JournaledCommand
    {
        public ulong SerialNumber { get; set; }
        public JournalCommand Type { get; set; }
        public JToken Data { get; set; }
        public IJournalEntry ToJournalEntry() => Type switch
        {
            JournalCommand.AddBucketsToNode => new AddBucketsToNodeJournalEntry(Type, Data),
            JournalCommand.RemoveBucketsFromNode => new RemoveBucketsFromNodeJournalEntry(Type, Data),
            JournalCommand.NodeContactUpdate => throw new NotImplementedException(),
            JournalCommand.AddNode => throw new NotImplementedException(),
            JournalCommand.BlockNode => throw new NotImplementedException(),
            JournalCommand.NodeNotifyBucketSynchronized => throw new NotImplementedException(),
            JournalCommand.NodeStateUpdate => throw new NotImplementedException(),
            JournalCommand.NodeClearBuckets => new ClearBucketsFromNodeJournalEntry(Type, Data),
            _ => (IJournalEntry)null
        };
    }

    public abstract class NodeJournalEntryBase<TCommandData> : JournalEntryBase<TCommandData> where TCommandData : NodeCommandData
    {
        protected NodeJournalEntryBase(JournalCommand command, JToken data) : base(command, data)
        {
        }

        public override void Apply(IStateMachine stateMachine)
        {
            Apply(stateMachine.GetNode(Data.NodeId), stateMachine);
        }

        protected abstract void Apply(INodeInfo node, IStateMachine stateMachine);
    }

    public abstract class JournalEntryBase<TCommandData> : IJournalEntry
    {
        protected JournalEntryBase(JournalCommand command, JToken data)
        {
            Command = command;
            Data = data.ToObject<TCommandData>();
        }
        public TCommandData Data { get; }
        public JournalCommand Command { get; }

        public abstract void Apply(IStateMachine stateMachine);
    }
    public class NodeCommandData
    {
        public Guid NodeId { get; set; }
    }
    public class NodeBucketCommandData : NodeCommandData
    {
        public HashSet<uint> Buckets { get; set; } = new HashSet<uint>();
    }

    public class ClearBucketsFromNodeJournalEntry : NodeJournalEntryBase<NodeCommandData>
    {
        public ClearBucketsFromNodeJournalEntry(JournalCommand command, JToken data) : base(command, data)
        {
        }

        protected override void Apply(INodeInfo node, IStateMachine stateMachine)
        {
            node?.ClearBuckets();
        }
    }

    public class RemoveBucketsFromNodeJournalEntry : NodeJournalEntryBase<NodeBucketCommandData>
    {
        public RemoveBucketsFromNodeJournalEntry(JournalCommand command, JToken data) : base(command, data)
        {

        }

        protected override void Apply(INodeInfo node, IStateMachine stateMachine)
        {
            node?.RemoveBuckets(Data.Buckets);
        }
    }
    public class AddBucketsToNodeJournalEntry : NodeJournalEntryBase<NodeBucketCommandData>
    {
        public AddBucketsToNodeJournalEntry(JournalCommand command, JToken data) : base(command, data)
        {

        }

        protected override void Apply(INodeInfo node, IStateMachine stateMachine)
        {
            node?.AddBuckets(Data.Buckets);
        }
    }

    public interface IJournalEntry
    {
        JournalCommand Command { get; }
        void Apply(IStateMachine stateMachine);
    }



    public interface INodeInfo : INotifyPropertyChanged
    {
        Guid Id { get; }
        string Name { get; }
        IReadOnlyCollection<Uri> Uris { get; }
        IReadOnlyCollection<uint> Buckets { get; }

        void SetUris(IEnumerable<Uri> uris);
        void AddBuckets(IEnumerable<uint> buckets);
        void RemoveBuckets(IEnumerable<uint> buckets);
        void ClearBuckets();

        NodeState State { get; set; }
    }

    public enum NodeState
    {
        Unknown, // We just started up, and don't have any state for this node yet, can transition to any other state.
        Offline, // Node is unreachable by us at this time, possible next states: Pending, Unhealthy, Operational, Blocked
        Pending, // Node is awaiting authorization to join the cluster, Possible next states: Offline,  Unhealthy, Operational, Blocked
        Unhealthy, // Node is unhealthy, reported by itself or by another peer (Slow response, low disk space, ram, state log or data replication is delayed, or some other fault)
        Operational, // Node is online and functional, possible next states: Unhealthy, Offline
        Blocked // Request to join cluster was denied by administrator, Possible next states: Offline
    }
}