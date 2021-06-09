using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ListSerializer
{
    public class ListSerializer : IListSerializer
    {
        public async Task Serialize(ListNode head, Stream s)
        {
            var json = await SerializeToJson(head);
            var byteArr = Encoding.UTF8.GetBytes(json);
            
            await s.WriteAsync(byteArr.AsMemory(0, byteArr.Length));
            s.Position = 0;
        }

        public async Task<ListNode> Deserialize(Stream s)
        {
            if (s.Length == 0) throw new ArgumentException("Input stream is empty");
            
            var reader = new StreamReader(s);
            var json = await reader.ReadToEndAsync();

            return await DeserializeFromJson(json);
        }

        public Task<ListNode> DeepCopy(ListNode head)
        {
            var oldToNewMap = new Dictionary<ListNode, ListNode>();
            var oldNode = head;

            while (oldNode != null)
            {
                oldToNewMap[oldNode] = new ListNode { Data = oldNode.Data};
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
        
        internal async Task<string> SerializeToJson(ListNode node)
        {
            if (node == null) return "[]";
            
            var head = node;

            var dtos = new List<ListNodeDto>();
            var processedNodes = new List<ListNode>();

            int id = 0;
            while (head != null)
            {
                var nodeDto = new ListNodeDto(id, head.Data); 

                dtos.Add(nodeDto);
                processedNodes.Add(head);
                head = head.Next;
                id++;
            }

            head = node;

            id = 0;

            while (head != null)
            {
                dtos[id].Next = processedNodes.IndexOf(head.Next);
                dtos[id].Previous = processedNodes.IndexOf(head.Previous);
                dtos[id].Random = processedNodes.IndexOf(head.Random);
                
                id++;
                head = head.Next;
            }

            //you could not utilize 3d party libraries that will serialize the full list for you
            //var json = JsonSerializer.Serialize(dtos);

            var sb = new StringBuilder();
            sb.Append('[');
            foreach (var dto in dtos)
            {
                //it's allowed to utilize 3d party libraries for serializing 1 node in particular format
                sb.Append(await Task.Factory.StartNew(() => JsonConvert.SerializeObject(dto)));
                sb.Append(',');
            }

            //Remove trailing comma
            sb.Length -= 1;
            sb.Append(']');

            return sb.ToString();
        }

        internal async Task<ListNode> DeserializeFromJson(string json)
        {
            try
            {
                var settings = new JsonSerializerSettings {MissingMemberHandling = MissingMemberHandling.Error};
                var listDto = await Task.Factory.StartNew( () => JsonConvert.DeserializeObject<List<ListNodeDto>>(json, settings));
                if (listDto == null || !listDto.Any()) return null;

                var listNodes = new Dictionary<int, ListNode>();

                foreach (var dto in listDto)
                {
                    var listNode = new ListNode();
                    listNode.Data = dto.Data;
                    listNodes[dto.Id] = listNode;
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
            catch (JsonException ex)
            {
                throw new ArgumentException("Stream contains invalid data", ex);
            }
        }
    }
}