
using System;

namespace EGame
{
    /// <summary>
    /// record专门用于不可变数据,自动生成GetHashCode()
    /// </summary>
    public record class ModelID : IComparable<ModelID>
    {
        public string Category { get; }
        public string Entry { get; }

        public ModelID(string category, string entry)
        {
            this.Category = category;
            this.Entry = entry;
        }
        
        public int CompareTo(ModelID other)
        {
            int c_sort = string.Compare(this.Category, other.Category, StringComparison.Ordinal);
            if(c_sort != 0)
                return c_sort;
            return string.Compare(this.Entry, other.Entry, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return $"{Category}.{Entry}";
        }
    }
}