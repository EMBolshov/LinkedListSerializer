using System;
using System.Collections.Generic;
using System.Linq;

namespace ListSerializer
{
    public static class ListNodeExtensions
    {
        public static void AddAtTail(this ListNode node, string value)
        {
            var newNode = new ListNode {Data = value};
            var tail = node.GetTail();
            
            tail.Next = newNode;
            newNode.Previous = tail;
        }

        public static ListNode GetTail(this ListNode head)
        {
            var tail = head;
            while (head != null)
            {
                tail = head;
                head = head.Next;
            }

            return tail;
        }
        
        public static void SetRandomLinks(this ListNode node)
        {
            var random = new Random();
            var nodes = new List<ListNode>();
            var head = node;

            while (head != null)
            {
                nodes.Add(head);
                head = head.Next;
            }

            head = node;
            while (head != null)
            {
                head.Random = nodes.ElementAt(random.Next(0, nodes.Count));
                head = head.Next;
            }
        }
    }
}