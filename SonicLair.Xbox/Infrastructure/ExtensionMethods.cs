﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace SonicLairXbox.Infrastructure
{
    public static class ExtensionMethods
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static ScrollViewer GetScrollViewer(this DependencyObject element)
        {
            if (element is ScrollViewer)
            {
                return (ScrollViewer)element;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
        public async static Task ScrollToIndex(this ListViewBase listViewBase, int index)
        {
            bool isVirtualizing = default(bool);
            double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

            // get the ScrollViewer withtin the ListView/GridView
            var scrollViewer = listViewBase.GetScrollViewer();
            // get the SelectorItem to scroll to
            var selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;

            // when it's null, means virtualization is on and the item hasn't been realized yet
            if (selectorItem == null)
            {
                isVirtualizing = true;

                previousHorizontalOffset = scrollViewer.HorizontalOffset;
                previousVerticalOffset = scrollViewer.VerticalOffset;

                // call task-based ScrollIntoViewAsync to realize the item
                await listViewBase.ScrollIntoViewAsync(listViewBase.Items[index]);

                // this time the item shouldn't be null again
                selectorItem = (SelectorItem)listViewBase.ContainerFromIndex(index);
            }

            // calculate the position object in order to know how much to scroll to
            var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            // when virtualized, scroll back to previous position without animation
            if (isVirtualizing)
            {
                await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
            }

            // scroll to desired position with animation!
            scrollViewer.ChangeView(position.X, position.Y, null);
        }

        public async static Task ScrollToItem(this ListViewBase listViewBase, object item)
        {
            bool isVirtualizing = default(bool);
            double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

            // get the ScrollViewer withtin the ListView/GridView
            var scrollViewer = listViewBase.GetScrollViewer();
            // get the SelectorItem to scroll to
            var selectorItem = listViewBase.ContainerFromItem(item) as SelectorItem;


            // when it's null, means virtualization is on and the item hasn't been realized yet
            if (selectorItem == null)
            {
                isVirtualizing = true;

                previousHorizontalOffset = scrollViewer.HorizontalOffset;
                previousVerticalOffset = scrollViewer.VerticalOffset;

                // call task-based ScrollIntoViewAsync to realize the item
                await listViewBase.ScrollIntoViewAsync(item);

                // this time the item shouldn't be null again
                selectorItem = (SelectorItem)listViewBase.ContainerFromItem(item);
            }

            if (selectorItem != null)
            {

                // calculate the position object in order to know how much to scroll to
                var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
                var position = transform.TransformPoint(new Point(0, 0));
                // when virtualized, scroll back to previous position without animation
                if (isVirtualizing)
                {
                    await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
                }

                // scroll to desired position with animation!
                scrollViewer.ChangeView(position.X, position.Y, null);
            }



        }

        public static async Task ScrollIntoViewAsync(this ListViewBase listViewBase, object item)
        {
            var tcs = new TaskCompletionSource<object>();
            var scrollViewer = listViewBase.GetScrollViewer();

            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
            try
            {
                scrollViewer.ViewChanged += viewChanged;
                listViewBase.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= viewChanged;
            }
        }

        public static async Task ChangeViewAsync(this ScrollViewer scrollViewer, double? horizontalOffset, double? verticalOffset, bool disableAnimation)
        {
            var tcs = new TaskCompletionSource<object>();

            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
            try
            {
                scrollViewer.ViewChanged += viewChanged;
                scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= viewChanged;
            }
        }

        public static string GetAsMMSS(this int duration)
        {
            return $"{Math.Floor(duration / 60d):00}:{duration % 60:00}";
        }
        
    }
}
