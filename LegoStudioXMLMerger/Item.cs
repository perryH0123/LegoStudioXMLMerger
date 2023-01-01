using System;
namespace LegoStudioXMLMerger
{
	public class Item
	{
		public static string itemType = "P";
		public string Id
		{
			get;
			set;
		} = "";
		public int Key
		{
			get;
			set;
		} = -1;
		public int Color
		{
			get;
			set;
		} = 0;

		public Item() { }

		public Item(string id, int key, int color)
		{
			this.Id = id;
			this.Key = key;
			this.Color = color;
		}

        public override string ToString()
        {
            return $"Item {Id} | Key {Key} | Color {Color}";
        }

        public override int GetHashCode()
        {
			return Id.GetHashCode() + Key + Color;
        }

        public override bool Equals(object obj)
        {
			if (!(obj is Item)) return false;
			return this.GetHashCode() == obj.GetHashCode();
        }
    }
}

