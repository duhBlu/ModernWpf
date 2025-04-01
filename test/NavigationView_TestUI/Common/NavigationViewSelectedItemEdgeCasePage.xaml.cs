﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ModernWpf.Controls;
using System.Windows;
using NavigationView = ModernWpf.Controls.NavigationView;
using NavigationViewItem = ModernWpf.Controls.NavigationViewItem;
using NavigationViewSelectionChangedEventArgs = ModernWpf.Controls.NavigationViewSelectionChangedEventArgs;

namespace MUXControlsTestApp
{
    public sealed partial class NavigationViewSelectedItemEdgeCasePage : TestPage
    {
        public NavigationViewSelectedItemEdgeCasePage()
        {
            this.InitializeComponent();

            NavView.SelectedItem = NavView.MenuItems[1];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

        }

        private void Button_click(object sender, RoutedEventArgs e)
        {
            var menuItem = new NavigationViewItem();
            menuItem.Content = "New Menu Item Ay";
            menuItem.Icon = new SymbolIcon(Symbol.AllApps);
            NavView.MenuItems.Add(menuItem);
        }
        private void Movies_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = MoviesItem;
        }

        private void Movies_Click2(object sender, RoutedEventArgs e)
        {
            MoviesItem.IsSelected = true;
        }

        private void TV_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = TVItem;
        }

        private void TV_Click2(object sender, RoutedEventArgs e)
        {
            TVItem.IsSelected = true;
        }

        private void CopyIsSelected_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = MoviesItem.IsSelected + " " + TVItem.IsSelected;
        }

    }
}
