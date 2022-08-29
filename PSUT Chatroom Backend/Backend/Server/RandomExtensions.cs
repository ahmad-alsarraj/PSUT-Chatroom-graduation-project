using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public static class RandomExtensions
    {
        private static readonly char[] RandomTextPool;

        static RandomExtensions()
        {
            List<char> pool = new();
            for (char i = 'a'; i <= 'z'; i++)
            {
                pool.Add(i);
            }

            for (char i = 'A'; i <= 'Z'; i++)
            {
                pool.Add(i);
            }

            for (char i = '0'; i <= '9'; i++)
            {
                pool.Add(i);
            }

            pool.AddRange(".,!");
            RandomTextPool = pool.ToArray();
        }

        public static T NextElementAndSwap<T>(this Random rand, IList<T> lst, int newIndex)
        {
            if (newIndex >= lst.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }

            var elementIndex = rand.Next(newIndex + 1);
            var element = lst[elementIndex];
            lst[elementIndex] = lst[newIndex];
            lst[newIndex] = element;
            return element;
        }

        public static T NextElement<T>(this Random rand, IList<T> lst)
        {
            if (lst.Count == 0)
            {
                throw new InvalidOperationException("List is empty.");
            }

            return lst[rand.Next(lst.Count)];
        }

        public static bool NextBool(this Random rand) => (rand.Next() & 1) == 1;

        public static string NextText(this Random rand, int length = -1)
        {
            if (length < 0)
            {
                length = rand.Next(1, 100);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(rand.NextElement(RandomTextPool));
            }

            return sb.ToString();
        }
        public static TimeSpan NextTimeSpan(this Random rand) => TimeSpan.FromHours(rand.Next(9, 18));
    }
}