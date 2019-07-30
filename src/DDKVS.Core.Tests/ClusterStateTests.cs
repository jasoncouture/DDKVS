using System;
using Xunit;
using DDKVS.Core.Metadata;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace DDKVS.Core.Tests
{
    public class ClusterStateTests
    {

        [Fact]
        public void ClusterStateTest()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void JournalEntryAffectsStateAsIntended()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void JournalEntryIsIdempotent()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void JournaledCommandProducesCorrectInterfaceImplementation() 
        {
            var nodeId = Guid.NewGuid();
            var expectedBuckets = new uint[] { 1, 2, 4 };
            var rawCommand = new JournaledCommand() {
                SerialNumber = 0,
                Type = JournalCommand.AddBucketsToNode,
                Data = JObject.FromObject(new {
                    nodeId,
                    Buckets = expectedBuckets
                })
            };
            var journalEntry = rawCommand.ToJournalEntry();

            var typedJournalEntry = Assert.IsType<AddBucketsToNodeJournalEntry>(journalEntry);
            Assert.Equal(nodeId, typedJournalEntry.Data.NodeId);
            Assert.Equal(expectedBuckets.Length, typedJournalEntry.Data.Buckets.Count);
            
            Assert.True(expectedBuckets.SequenceEqual(typedJournalEntry.Data.Buckets.OrderBy(i => i)));
        }

    }
}