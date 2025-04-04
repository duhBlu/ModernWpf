﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ModernWpf.Controls;
using MUXControlsTestApp.Samples.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MUXControlsTestApp.Samples
{
    public sealed partial class VirtualizingStackLayoutSamplePage
    {
        private string _lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus.";

        private RecyclingElementFactory elementFactory;

        public VirtualizingStackLayoutSamplePage()
        {
            this.InitializeComponent();
            elementFactory = (RecyclingElementFactory)Resources[nameof(elementFactory)];
            repeater.ItemTemplate = elementFactory;

            var rnd = new Random();
            var data = new ObservableCollection<Recipe>(Enumerable.Range(0, 300).Select(k =>
                           new Recipe
                           {
                               ImageUri = new Uri(string.Format("pack://application:,,,/Images/recipe{0}.png", k % 8 + 1)),
                               Description = k + " - " + _lorem.Substring(0, rnd.Next(50, 350))
                           }));

            repeater.ItemsSource = data;
            bringIntoView.Click += BringIntoView_Click;
        }

        private void BringIntoView_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            if (int.TryParse(tb.Text, out index))
            {
                if (index >= 0 && index < repeater.ItemsSourceView.Count)
                {
                    var anchor = repeater.GetOrCreateElement(index);
                    ((FrameworkElement)anchor).BringIntoView();
                }
            }
        }
    }
}
