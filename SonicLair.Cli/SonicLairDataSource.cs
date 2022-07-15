using NStack;

using System.Collections;

using Terminal.Gui;

namespace SonicLair.Cli
{
    // This is basically the same implementation used by the UICatalog main window
    public class SonicLairDataSource<T> : IListDataSource
    {
        private List<T> _items;
        private BitArray marks;
        private int count, len;

        private readonly Func<T, string> _serializer;

        public List<T> Items
        {
            get => _items;
            set
            {
                if (value != null)
                {
                    count = value.Count;
                    marks = new BitArray(count);
                    _items = value;
                    len = GetMaxLengthItem();
                }
            }
        }

        public bool IsMarked(int item)
        {
            if (item >= 0 && item < count)
                return marks[item];
            return false;
        }

        public int Count => Items != null ? Items.Count : 0;

        public int Length => len;

        public SonicLairDataSource(List<T> itemList, Func<T, string> serializer)
        {
            _items = itemList;
            _serializer = serializer;
        }

        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            container.Move(col, line);
            // Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
            RenderUstr(driver, _serializer(Items[item]), col, line, width, start);
        }

        public void SetMark(int item, bool value)
        {
            if (item >= 0 && item < count)
                marks[item] = value;
        }

        private int GetMaxLengthItem()
        {
            if (_items?.Count == 0)
            {
                return 0;
            }

            int maxLength = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                var sc = _serializer(Items[i]);
                var l = sc.Length;
                if (l > maxLength)
                {
                    maxLength = l;
                }
            }
            return maxLength;
        }

        private void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
        {
            int used = 0;
            int index = start;
            while (index < ustr.Length)
            {
                (var rune, var size) = Utf8.DecodeRune(ustr, index, index - ustr.Length);
                var c = System.Rune.ColumnWidth(rune);
                if (used + c >= width) break;
                driver.AddRune(rune);
                used += c;
                index += size;
            }

            while (used < width)
            {
                driver.AddRune(' ');
                used++;
            }
        }

        public IList ToList()
        {
            return Items;
        }
    }
}