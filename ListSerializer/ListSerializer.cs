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
                var nodeDto = new ListNodeDto(id, head.Data);

                dtos.Add(nodeDto);
                processedNodes[head] = id;
                head = head.Next;
                id++;
            }

            head = node;

            id = 0;

            while (head != null)
            {
                dtos[id].Next = head.Next == null ? -1 : processedNodes[head.Next];
                dtos[id].Previous = head.Previous == null ? -1 : processedNodes[head.Previous];
                dtos[id].Random = head.Random == null ? -1 : processedNodes[head.Random];

                yield return dtos[id];

                id++;
                head = head.Next;
            }
        }

        private ListNode DeserializeFromProtobuf(Stream stream)
        {
            var listNodes = new Dictionary<int, ListNode>();
            var listDto = new List<ListNodeDto>();
            try
            {
                ListNodeDto nodeDto;
                while ((nodeDto =
                    Serializer.DeserializeWithLengthPrefix<ListNodeDto>(stream, PrefixStyle.Base128, 1)) != null)
                {
                    var listNode = new ListNode {Data = nodeDto.Data};
                    listDto.Add(nodeDto);
                    listNodes[nodeDto.Id] = listNode;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Stream contains invalid data", ex);
            }

            int i = 0;
            foreach (var dto in listDto)
            {
                listNodes[i].Next = dto.Next == -1 ? null : listNodes[dto.Next];
                listNodes[i].Previous = dto.Previous == -1 ? null : listNodes[dto.Previous];
                listNodes[i].Random = dto.Random == -1 ? null : listNodes[dto.Random];
                i++;
            }

            return listNodes[0];
        }
    }
}