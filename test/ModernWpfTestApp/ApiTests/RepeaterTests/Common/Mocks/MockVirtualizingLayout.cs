﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows;
using VirtualizingLayout = ModernWpf.Controls.VirtualizingLayout;
using VirtualizingLayoutContext = ModernWpf.Controls.VirtualizingLayoutContext;

namespace ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks
{
    class MockVirtualizingLayout : VirtualizingLayout
    {
        public Func<Size, VirtualizingLayoutContext, Size> MeasureLayoutFunc { get; set; }
        public Func<Size, VirtualizingLayoutContext, Size> ArrangeLayoutFunc { get; set; }

        public new void InvalidateMeasure()
        {
            base.InvalidateMeasure();
        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            return MeasureLayoutFunc != null ? MeasureLayoutFunc(availableSize, context) : default(Size);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            return ArrangeLayoutFunc != null ? ArrangeLayoutFunc(finalSize, context) : default(Size);
        }
    }
}
