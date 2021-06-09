using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ListSerializer.Test
{
    /// <summary>
    /// it's guaranteed that list provided as an argument to Serialize and DeepCopy function is consistent and doesn't contain any cycles
    /// </summary>
    [TestClass]
    public class ListSerializerTest
    {
        private static readonly IListSerializer Serializer = new ListSerializer();

        [TestMethod]
        public async Task TestSerializationAndDeserializationOfSingleNode()
        {
            var input = BuildListFromValues(new[] {"node 1"});

            await using (var stream = new MemoryStream())
            {
                await Serializer.Serialize(input, stream);

                stream.Length.Should().NotBe(0);

                var deserializedNode = await Serializer.Deserialize(stream);

                deserializedNode.Data.Should().Be("node 1");
                deserializedNode.Previous.Should().BeNull();
                deserializedNode.Next.Should().BeNull();
                deserializedNode.Random.Should().Be(deserializedNode);
            }
        }

        [TestMethod]
        public async Task TestSerializationAndDeserialization()
        {
            var input = BuildListFromValues(new[] {"node 1", "node 2", "node 3"});

            await using (var stream = new MemoryStream())
            {
                await Serializer.Serialize(input, stream);
                
                stream.Length.Should().NotBe(0);

                var deserializedNode = await Serializer.Deserialize(stream);

                deserializedNode.Data.Should().Be("node 1");
                deserializedNode.Previous.Should().BeNull();
                deserializedNode.Next.Data.Should().Be("node 2");
                deserializedNode.Next.Previous.Should().Be(deserializedNode);
                deserializedNode.Next.Next.Data.Should().Be("node 3");
                deserializedNode.Next.Next.Next.Should().BeNull();
                deserializedNode.Next.Next.Previous.Should().Be(deserializedNode.Next);
            }
        }

        [TestMethod]
        public async Task DeepCopyTest()
        {
            var input = BuildListFromValues(new[] {"node 1", "node 2", "node 3"});
            var deepCopy = await Serializer.DeepCopy(input);

            while (input != null)
            {
                input.Should().NotBeEquivalentTo(deepCopy);
                input.Next?.Should().NotBeEquivalentTo(deepCopy.Next);
                input.Previous?.Should().NotBeEquivalentTo(deepCopy.Previous);

                input.Data.Should().BeEquivalentTo(deepCopy.Data);

                input = input.Next;
                deepCopy = deepCopy.Next;
            }
        }

        [TestMethod]
        public async Task SerializeAndDeserializeNullTest()
        {
            await using var stream = new MemoryStream();
            await Serializer.Serialize(null, stream);

            //result is empty array -- "[]"
            stream.Length.Should().Be(2);

            var deserializedNode = await Serializer.Deserialize(stream);
            deserializedNode.Should().BeNull();
        }

        [TestMethod]
        public void DeserializeEmptyStreamExpectedArgumentException()
        {
            using var stream = new MemoryStream();
            Action action = () => Serializer.Deserialize(stream).GetAwaiter().GetResult();
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task DeserializeInvalidStreamExpectedArgumentException()
        {
            await using (var stream = new MemoryStream())
            {
                var data = "Definitely not JSON";
                var byteArr = Encoding.UTF8.GetBytes(data);

                await stream.WriteAsync(byteArr.AsMemory(0, byteArr.Length));
                stream.Position = 0;

                Action action = () => Serializer.Deserialize(stream).GetAwaiter().GetResult();
                action.Should().Throw<ArgumentException>();
            }
        }

        [TestMethod]
        public async Task DeserializeInvalidStreamWithJsonExpectedArgumentException()
        {
            await using (var stream = new MemoryStream())
            {
                var data = "[{\"Wrong\":\"Model\"}]";
                var byteArr = Encoding.UTF8.GetBytes(data);

                await stream.WriteAsync(byteArr.AsMemory(0, byteArr.Length));
                stream.Position = 0;

                Action action = () => Serializer.Deserialize(stream).GetAwaiter().GetResult();
                action.Should().Throw<ArgumentException>();
            }
        }

        [TestMethod]
        public async Task TestSerializationAndDeserializationOfHugeList()
        {
            var input = BuildListFromValues(Enumerable.Range(0, 10000).Select(n => n.ToString()));

            await using (var stream = new MemoryStream())
            {
                await Serializer.Serialize(input, stream);

                stream.Length.Should().NotBe(0);

                var head = await Serializer.Deserialize(stream);

                head.Previous.Should().BeNull();
                var tail = head.GetTail();
                tail.Next.Should().BeNull();

                var previous = head;
                head = head.Next;

                while (head != null)
                {
                    head.Previous.Should().Be(previous);
                    previous.Next.Should().Be(head);

                    head.Random.Should().NotBeNull().And.BeOfType(typeof(ListNode));

                    previous = head;
                    head = head.Next;
                }
            }
        }

        [TestMethod]
        public async Task TestJsonSerializationWithoutRandomNodes()
        {
            var input = LinkNodesInOrder(new[]
            {
                new ListNode {Data = "node 1"},
                new ListNode {Data = "node 2"},
                new ListNode {Data = "node 3"}
            });

            var serializer = new ListSerializer();
            var json = await serializer.SerializeToJson(input);

            json.Should().BeEquivalentTo(
                "[{\"Id\":0,\"Data\":\"node 1\",\"Next\":1,\"Previous\":-1,\"Random\":-1}," +
                "{\"Id\":1,\"Data\":\"node 2\",\"Next\":2,\"Previous\":0,\"Random\":-1}," +
                "{\"Id\":2,\"Data\":\"node 3\",\"Next\":-1,\"Previous\":1,\"Random\":-1}]");
        }

        [TestMethod]
        public async Task TestJsonDeserializationWithoutRandomNodes()
        {
            var input =
                "[{\"Id\":0,\"Data\":\"node 1\",\"Next\":1,\"Previous\":-1,\"Random\":-1}," +
                "{\"Id\":1,\"Data\":\"node 2\",\"Next\":2,\"Previous\":0,\"Random\":-1}," +
                "{\"Id\":2,\"Data\":\"node 3\",\"Next\":-1,\"Previous\":1,\"Random\":-1}]";

            var serializer = new ListSerializer();
            var node = await serializer.DeserializeFromJson(input);

            node.Data.Should().Be("node 1");
            node.Previous.Should().BeNull();
            node.Next.Data.Should().Be("node 2");
            node.Next.Previous.Should().Be(node);
            node.Next.Next.Data.Should().Be("node 3");
            node.Next.Next.Next.Should().BeNull();
            node.Next.Next.Previous.Should().Be(node.Next);
        }

        private static ListNode LinkNodesInOrder(IEnumerable<ListNode> nodes)
        {
            var input = nodes.ToList();
            if (!input.Any()) return null;

            var head = input.First();
            var prevNode = head;

            foreach (var node in input.Skip(1))
            {
                prevNode.Next = node;
                node.Previous = prevNode;

                prevNode = node;
            }

            return head;
        }

        private static ListNode BuildListFromValues(IEnumerable<string> values)
        {
            var input = values.ToList();
            if (!input.Any()) return null;

            var list = new ListNode {Data = input.First()};

            foreach (var value in input.Skip(1))
            {
                list.AddAtTail(value);
            }

            list.SetRandomLinks();

            return list;
        }
    }
}