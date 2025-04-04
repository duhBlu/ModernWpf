﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;
using MUXControlsTestApp.Utilities;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using VirtualizingLayout = ModernWpf.Controls.VirtualizingLayout;
using ItemsRepeater = ModernWpf.Controls.ItemsRepeater;
using StackLayout = ModernWpf.Controls.StackLayout;
using UniformGridLayout = ModernWpf.Controls.UniformGridLayout;
using ItemsRepeaterScrollHost = ModernWpf.Controls.ItemsRepeaterScrollHost;
using ElementRealizationOptions = ModernWpf.Controls.ElementRealizationOptions;
using System.Windows.Media;
using ModernWpf.Controls;

namespace ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests
{
    [TestClass]
    public class LayoutTests : ApiTestBase
    {
        [TestMethod]
        public void ValidateMappingAndAutoRecycling()
        {
            ItemsRepeater repeater = null;
            ScrollViewer scrollViewer = null;
            RunOnUIThread.Execute(() =>
            {
                var layout = new MockVirtualizingLayout()
                {
                    MeasureLayoutFunc = (availableSize, context) =>
                    {
                        var element0 = context.GetOrCreateElementAt(index: 0);
                        // lookup - repeater will give back the same element and note that this element will not
                        // be pinned - i.e it will be auto recycled after a measure pass where GetElementAt(0) is not called.
                        var element0lookup = context.GetOrCreateElementAt(index: 0, options: ElementRealizationOptions.None);

                        var element1 = context.GetOrCreateElementAt(index: 1, options: ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                        // forcing a new element for index 1 that will be pinned (not auto recycled). This will be 
                        // a completely new element. Repeater does not do the mapping/lookup when forceCreate is true.
                        var element1Clone = context.GetOrCreateElementAt(index: 1, options: ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);

                        Verify.AreSame(element0, element0lookup);
                        Verify.AreNotSame(element1, element1Clone);

                        element0.Measure(availableSize);
                        element1.Measure(availableSize);
                        element1Clone.Measure(availableSize);
                        return new Size(100, 100);
                    },
                };

                Content = CreateAndInitializeRepeater(
                    itemsSource: Enumerable.Range(0, 5),
                    layout: layout,
                    elementFactory: GetDataTemplate("<Button>Hello</Button>"),
                    repeater: ref repeater,
                    scrollViewer: ref scrollViewer);

                Content.UpdateLayout();

                Verify.IsNotNull(repeater.TryGetElement(0));
                Verify.IsNotNull(repeater.TryGetElement(1));

                layout.MeasureLayoutFunc = null;

                repeater.InvalidateMeasure();
                Content.UpdateLayout();

                Verify.IsNull(repeater.TryGetElement(0)); // not pinned, should be auto recycled.
                Verify.IsNotNull(repeater.TryGetElement(1)); // pinned, should stay alive
            });
        }

        [TestMethod]
        public void ValidateNonVirtualLayoutWithItemsRepeater()
        {
            RunOnUIThread.Execute(() =>
            {
                var repeater = new ItemsRepeater();
                repeater.Layout = new NonVirtualStackLayout();
                repeater.ItemsSource = Enumerable.Range(0, 10);
                repeater.ItemTemplate = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                         <Button Content='{Binding}' Height='100' />
                    </DataTemplate>");

                Content = repeater;
                Content.UpdateLayout();

                double expectedYOffset = 0;
                for (int i = 0; i < repeater.ItemsSourceView.Count; i++)
                {
                    var child = repeater.TryGetElement(i) as Button;
                    Verify.IsNotNull(child);
                    var layoutBounds = LayoutInformation.GetLayoutSlot(child);
                    Verify.AreEqual(expectedYOffset, layoutBounds.Y);
                    Verify.AreEqual(i, child.Content);
                    expectedYOffset += 100;
                }
            });
        }

        [TestMethod]
        public void ValidateNonVirtualLayoutDoesNotGetMeasuredForViewportChanges()
        {
            RunOnUIThread.Execute(() =>
            {
                int measureCount = 0;
                int arrangeCount = 0;
                var repeater = new ItemsRepeater();

                // with a non virtualizing layout, repeater will just
                // run layout once. 
                repeater.Layout = new MockNonVirtualizingLayout()
                {
                    MeasureLayoutFunc = (size, context) =>
                    {
                        measureCount++;
                        return new Size(100, 800);
                    },
                    ArrangeLayoutFunc = (size, context) =>
                    {
                        arrangeCount++;
                        return new Size(100, 800);
                    }
                };

                repeater.ItemsSource = Enumerable.Range(0, 10);
                repeater.ItemTemplate = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                         <Button Content='{Binding}' Height='100' />
                    </DataTemplate>");

                Content = new ScrollViewer()
                {
                    Content = repeater
                };
                Content.UpdateLayout();

                Verify.AreEqual(1, measureCount);
                Verify.AreEqual(1, arrangeCount);

                measureCount = 0;
                arrangeCount = 0;

                // Once we switch to a virtualizing layout we should 
                // get at least two passes to update the viewport.
                repeater.Layout = new MockVirtualizingLayout()
                {
                    MeasureLayoutFunc = (size, context) =>
                    {
                        measureCount++;
                        return new Size(100, 800);
                    },
                    ArrangeLayoutFunc = (size, context) =>
                    {
                        arrangeCount++;
                        return new Size(100, 800);
                    }
                };

                Content.UpdateLayout(true);

                Verify.IsGreaterThan(measureCount, 1);
                Verify.IsGreaterThan(arrangeCount, 1);
            });
        }

        [TestMethod]
        public void ValidateStackLayoutDoesNotRetainIncorrectMinorWidth()
        {
            RunOnUIThread.Execute(() =>
            {
                var repeater = new ItemsRepeater()
                {
                    ItemsSource = Enumerable.Range(0, 1)
                };

                Content = new ScrollViewer()
                {
                    Content = repeater,
                    Width = 400,
                };

                Content.UpdateLayout();

                // Measure with large width.
                repeater.Measure(new Size(600, 100));
                Verify.AreEqual(600, repeater.DesiredSize.Width);
                // Measure with smaller width again before arrange. 
                // StackLayout has to pick up the smaller width for its extent.
                repeater.Measure(new Size(300, 100));
                Verify.AreEqual(300, repeater.DesiredSize.Width);

                Content.UpdateLayout();
                Verify.AreEqual(400, repeater.ActualWidth);
            });
        }

        [TestMethod]
        public void ValidateStackLayoutDisabledVirtualizationWithItemsRepeater()
        {
            RunOnUIThread.Execute(() =>
            {
                var repeater = new ItemsRepeater();
                var stackLayout = new StackLayout();
                stackLayout.DisableVirtualization = true;
                repeater.Layout = stackLayout;
                repeater.ItemsSource = Enumerable.Range(0, 10);
                repeater.ItemTemplate = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                         <Button Content='{Binding}' Height='100' />
                    </DataTemplate>");

                var scrollViewer = new ScrollViewer()
                {
                    Content = repeater
                };
                scrollViewer.Height = 100;
                Content = scrollViewer;
                Content.UpdateLayout();

                for (int i = 0; i < repeater.ItemsSourceView.Count; i++)
                {
                    var child = repeater.TryGetElement(i) as Button;
                    Verify.IsNotNull(child);
                }
            });
        }

        [TestMethod]
        public void VerifyUniformGridLayoutDoesntCrashWhenTryingToScrollToEnd()
        {

            ItemsRepeater repeater = null;
            ScrollViewer scrollViewer = null;
            RunOnUIThread.Execute(() =>
            {
                repeater = new ItemsRepeater
                {
                    ItemsSource = Enumerable.Range(0, 1000).Select(i => new Border
                    {
                        Background = new SolidColorBrush(Colors.Blue),
                        Child = new TextBlock { Text = "#" + i }
                    }).ToArray(),
                    Layout = new UniformGridLayout
                    {
                        MinItemWidth = 100,
                        MinItemHeight = 40,
                        MinRowSpacing = 10,
                        MinColumnSpacing = 10
                    }
                };
                scrollViewer = new ScrollViewer { Content = repeater };
                Content = scrollViewer;
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                scrollViewer.ChangeView(0, repeater.ActualHeight, null);
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                scrollViewer.ChangeView(0, 0, null);
            });

            IdleSynchronizer.Wait();

            // The test guards against an app crash, so this is enough to verify
            Verify.IsTrue(true);
        }

        private ItemsRepeaterScrollHost CreateAndInitializeRepeater(
           object itemsSource,
           VirtualizingLayout layout,
           object elementFactory,
           ref ItemsRepeater repeater,
           ref ScrollViewer scrollViewer)
        {
            repeater = new ItemsRepeater()
            {
                ItemsSource = itemsSource,
                Layout = layout,
                ItemTemplate = elementFactory,
                HorizontalCacheLength = 0,
                VerticalCacheLength = 0,
            };

            scrollViewer = new ScrollViewer()
            {
                Content = repeater
            };

            return new ItemsRepeaterScrollHost()
            {
                Width = 400,
                Height = 400,
                ScrollViewer = scrollViewer
            };
        }

        private DataTemplate GetDataTemplate(string content)
        {
            return (DataTemplate)XamlReader.Parse(
                       string.Format(@"<DataTemplate  
                            xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                           {0}
                        </DataTemplate>", content));
        }
    }
}
