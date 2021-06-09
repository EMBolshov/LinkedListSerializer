namespace ListSerializer
{
    public class ListNodeDto
    {
        public int Id { get; }
        public string Data { get; }
        public int Next { get; set; }
        public int Previous { get; set; }
        public int Random { get; set; }

        public ListNodeDto(int id, string data)
        {
            Id = id;
            Data = data;
        }
    }
}