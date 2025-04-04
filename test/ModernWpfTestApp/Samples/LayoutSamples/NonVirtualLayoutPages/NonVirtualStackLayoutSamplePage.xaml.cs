﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;

namespace MUXControlsTestApp.Samples
{
    public sealed partial class NonVirtualStackLayoutSamplePage
    {
        public NonVirtualStackLayoutSamplePage()
        {
            this.InitializeComponent();
            repeater.ItemsSource = Enumerable.Range(0, 10);
        }
    }
}
