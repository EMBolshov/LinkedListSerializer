using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ListSerializer.TestConsole
{
    /// <summary>
    /// Console for profiling with dotMemory 
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Console started");
            var serializer = new ListSerializer();
            var input = BuildListFromValues(Enumerable.Range(0, 500000).Select(n => n.ToString()));

            Console.WriteLine("ListNode built");
            await using (var stream = new MemoryStream())
            {
                await serializer.Serialize(input, stream);
                await serializer.Deserialize(stream);
            }
            
            Console.WriteLine("Done");
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