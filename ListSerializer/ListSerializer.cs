using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ProtoBuf;

namespace ListSerializer
{
    public class ListSerializer : IListSerializer
    {
        public async Task Serialize(ListNode head, Stream s)
        {
            if (head == null) return;

            foreach (var nodeDto in ConvertToProtoModel(head))
            {
                var serialized = ProtoSerialize(nodeDto);
                await s.WriteAsync(serialized.AsMemory(0, serialized.Length));
            }

            byte[] ProtoSerialize(ListNodeDto node)
            {
                using var stream = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(stream, node, PrefixStyle.Base128, 1);
                return stream.ToArray();
            }
        }

        public Task<ListNode> Deserialize(Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);
            if (s.Length == 0) throw new ArgumentException("Input stream is empty");

            return Task.FromResult(DeserializeFromProtobuf(s));
        }

        public Task<ListNode> DeepCopy(ListNode head)
        {
            var oldToNewMap = new Dictionary<ListNode, ListNode>();
            var oldNode = head;

            while (oldNode != null)
            {
                oldToNewMap[oldNode] = new ListNode {Data = oldNode.Data};
                oldNode = oldNode.Next;
            }

            oldNode = head;

            while (oldNode != null)
            {
                oldToNewMap[oldNode].Next = oldNode.Next == null ? null : oldToNewMap[oldNode.Next];
                oldToNewMap[oldNode].Previous = oldNode.Previous == null ? null : oldToNewMap[oldNode.Previous];
                oldToNewMap[oldNode].Random = oldNode.Random == null ? null : oldToNewMap[oldNode.Random];

                oldNode = oldNode.Next;
            }

            return Task.FromResult(oldToNewMap[head]);
        }

        private IEnumerable<ListNodeDto> ConvertToProtoModel(ListNode node)
        {
            if (node == null) yield return null;

            var head = node;

            var dtos = new List<ListNodeDto>();
            var processedNodes = new Dictionary<ListNode, int>();

            int id = 0;
            while (head != null)
            {
                var nodeDto = new ListNodeDto(head.Data);

                dtos.Add(nodeDto);
                processedNodes[head] = id;
                head = head.Next;
                id++;
            }

            head = node;

            id = 0;

            while (head != null)
            {
                dtos[id].Random = head.Random == null ? -1 : processedNodes[head.Random];

                yield return dtos[id];

                id++;
                head = head.Next;
            }
        }

        //Less readable cuz of special first and last node processing, but without comparisons on each iteration  
        private ListNode DeserializeFromProtobuf(Stream stream)
        {
            var listNodes = new Dictionary<int, ListNode>();
            var randomNodeIndexes = new List<int>();
            var id = 0;
            
            try
            {
                ListNodeDto nodeDto;

                while ((nodeDto = Serializer.DeserializeWithLengthPrefix<ListNodeDto>(stream, PrefixStyle.Base128, 1)) != null)
                {
                    var listNode = new ListNode {Data = nodeDto.Data};
                    randomNodeIndexes.Add(nodeDto.Random);
                    listNodes[id] = listNode;
                    id++;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Stream contains invalid data", ex);
            }

            var lastIndex = randomNodeIndexes.Count - 1;

            listNodes[0].Random = randomNodeIndexes[0] == -1 ? null : listNodes[randomNodeIndexes[0]];

            //If there is single node - work is done, just return it
            if (lastIndex == 0) return listNodes[0];
            
            //First node has no previous 
            listNodes[0].Next = listNodes[1];

            for (int i = 1; i < lastIndex; i++)
            {
                var randomNodeIndex = randomNodeIndexes[i];
                listNodes[i].Next = listNodes[i + 1];
                listNodes[i].Previous = listNodes[i - 1];
                listNodes[i].Random = randomNodeIndex == -1 ? null : listNodes[randomNodeIndex];
            }

            //Last node has no next
            listNodes[lastIndex].Previous = listNodes[lastIndex - 1];
            listNodes[lastIndex].Random = randomNodeIndexes[lastIndex] == -1 ? null : listNodes[randomNodeIndexes[lastIndex]];

            return listNodes[0];
        }
    }
}