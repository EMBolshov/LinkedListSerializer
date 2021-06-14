using ProtoBuf;

namespace ListSerializer
{
    [ProtoContract]
    public class ListNodeDto
    {
        [ProtoMember(1)]
        public string Data { get; }

        [ProtoMember(2)]
        public int Random { get; set; }

        public ListNodeDto()
        {
            
        }
        
        public ListNodeDto(string data)
        {
            Data = data;
        }
    }
}