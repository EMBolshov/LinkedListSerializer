using ProtoBuf;

namespace ListSerializer
{
    [ProtoContract]
    public class ListNodeDto
    {
        [ProtoMember(1)]
        public int Id { get; }
        
        [ProtoMember(2)]
        public string Data { get; }
        
        [ProtoMember(3)]
        public int Next { get; set; }
        
        [ProtoMember(4)]
        public int Previous { get; set; }
        
        [ProtoMember(5)]
        public int Random { get; set; }

        public ListNodeDto()
        {
            
        }
        
        public ListNodeDto(int id, string data)
        {
            Id = id;
            Data = data;
        }
    }
}