﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;
using ModernWpf.Controls;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace ModernWpf.Tests.MUXControls.ApiTests.RepeaterTests
{
    using ElementFactoryGetArgs = ModernWpf.Controls.ElementFactoryGetArgs;
    using ElementFactoryRecycleArgs = ModernWpf.Controls.ElementFactoryRecycleArgs;
    using ItemsRepeater = ModernWpf.Controls.ItemsRepeater;
    using ItemsRepeaterScrollHost = ModernWpf.Controls.ItemsRepeaterScrollHost;
    using RecyclePool = ModernWpf.Controls.RecyclePool;
    using RecyclingElementFactory = ModernWpf.Controls.RecyclingElementFactory;
    using RepeaterTestHooks = Microsoft.UI.Private.Controls.RepeaterTestHooks;
    using SelectTemplateEventArgs = ModernWpf.Controls.SelectTemplateEventArgs;
    using StackLayout = ModernWpf.Controls.StackLayout;

    [TestClass]
    public class ItemTemplateTests : ApiTestBase
    {
        [TestMethod]
        public void ValidateRecycling()
        {
            RunOnUIThread.Execute(() =>
            {
                var elementFactory = new RecyclingElementFactory()
                {
                    RecyclePool = new RecyclePool(),
                };
                elementFactory.Templates["even"] = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <TextBlock Text='even' />
                    </DataTemplate>");
                elementFactory.Templates["odd"] = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <TextBlock Text='odd' />
                    </DataTemplate>");

                elementFactory.SelectTemplateKey +=
                delegate (RecyclingElementFactory sender, SelectTemplateEventArgs args)
                {
                    args.TemplateKey = ((int)args.DataContext % 2 == 0) ? "even" : "odd";
                };

                const int numItems = 10;
                ItemsRepeater repeater = new ItemsRepeater()
                {
                    ItemsSource = Enumerable.Range(0, numItems),
                    ItemTemplate = elementFactory,
                };

                var context = (ElementFactoryGetArgs)RepeaterTestHooks.CreateRepeaterElementFactoryGetArgs();
                context.Parent = repeater;
                var clearContext = (ElementFactoryRecycleArgs)RepeaterTestHooks.CreateRepeaterElementFactoryRecycleArgs();
                clearContext.Parent = repeater;

                // Element0 is of type even, a new one should be created
                context.Data = 0;
                var element0 = elementFactory.GetElement(context);
                Verify.IsNotNull(element0);
                Verify.AreEqual("even", (element0 as TextBlock).Text);
                clearContext.Element = element0;
                elementFactory.RecycleElement(clearContext);

                // Element1 is of type odd, a new one should be created
                context.Data = 1;
                var element1 = elementFactory.GetElement(context);
                Verify.IsNotNull(element1);
                Verify.AreNotSame(element0, element1);
                Verify.AreEqual("odd", (element1 as TextBlock).Text);
                clearContext.Element = element1;
                elementFactory.RecycleElement(clearContext);

                // Element0 should be recycled for element2
                context.Data = 2;
                var element2 = elementFactory.GetElement(context);
                Verify.AreEqual("even", (element2 as TextBlock).Text);
                Verify.AreSame(element0, element2);

                // Element1 should be recycled for element3
                context.Data = 3;
                var element3 = elementFactory.GetElement(context);
                Verify.AreEqual("odd", (element3 as TextBlock).Text);
                Verify.AreSame(element1, element3);
            });
        }

        // Validate data context propagation and template selection
        [TestMethod]
        public void ValidateBindingAndTemplateSelection()
        {
            RunOnUIThread.Execute(() =>
            {
                var staticTemplate = (DataTemplate)XamlReader.Parse(
                   @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <TextBlock Text='static' />
                    </DataTemplate>");
                var bindTemplate = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <TextBlock Text='{Binding}' />
                    </DataTemplate>");
                var elementFactory = new RecyclingElementFactory()
                {
                    RecyclePool = new RecyclePool(),
                    Templates =
                    {
                        { "static", staticTemplate },
                        { "bind", bindTemplate }
                    }
                };

                elementFactory.SelectTemplateKey +=
                delegate (RecyclingElementFactory sender, SelectTemplateEventArgs args)
                {
                    args.TemplateKey = ((int)args.DataContext % 2 == 0) ? "bind" : "static";
                };

                const int numItems = 10;
                ItemsRepeater repeater = null;
                Content = CreateAndInitializeRepeater
                (
                    itemsSource: Enumerable.Range(0, numItems),
                    elementFactory: elementFactory,
                    layout: new StackLayout(),
                    repeater: ref repeater
                );

                Content.UpdateLayout();

                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    if (i % 2 == 0)
                    {
                        // Text is bound to the data for even indicies
                        Verify.AreEqual(i.ToString(), element.Text);
                        Verify.AreEqual(i, element.DataContext);
                    }
                    else
                    {
                        // Text explicitly set on the element only for odd indicies
                        Verify.AreEqual("static", element.Text);
                    }
                }
            });
        }

        [TestMethod]
        public void ValidateCustomRecyclingElementFactory()
        {
            RunOnUIThread.Execute(() =>
            {
                var itemTemplate = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='uninitialized' />
                        </DataTemplate>");
                ItemsRepeater repeater = null;
                const int numItems = 10;
                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems).Select(i => string.Format("Item #{0}", i)),
                   elementFactory: new RecyclingElementFactoryDerived()
                   {
                       Templates = { { "key", itemTemplate } },
                       RecyclePool = new RecyclePool(),
                       GetElementFunc = delegate (int index, UIElement owner, UIElement elementFromBase)
                       {
                           ((TextBlock)elementFromBase).Text = index.ToString();
                           return elementFromBase;
                       },
                       ClearElementFunc = delegate (UIElement element, UIElement owner)
                       {
                           ((TextBlock)element).Text = "uninitialized";
                       },
                       SelectTemplateIdFunc = delegate (object data, UIElement owner)
                       {
                           return "key";
                       },
                   },
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();
                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Text);
                }
            });
        }

        [TestMethod]
        public void ValidateRecyclingElementFactoryWithSingleTemplate()
        {
            RunOnUIThread.Execute(() =>
            {
                var itemTemplate = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='uninitialized' />
                        </DataTemplate>");
                ItemsRepeater repeater = null;
                const int numItems = 10;
                var elementFactory = new RecyclingElementFactoryDerived()
                {
                    Templates = { { "key", itemTemplate } },
                    RecyclePool = new RecyclePool(),
                    GetElementFunc = delegate (int index, UIElement owner, UIElement elementFromBase)
                    {
                        ((TextBlock)elementFromBase).Text = index.ToString();
                        return elementFromBase;
                    },
                    ClearElementFunc = delegate (UIElement element, UIElement owner)
                    {
                        ((TextBlock)element).Text = "uninitialized";
                    },
                };

                elementFactory.SelectTemplateKey += delegate (RecyclingElementFactory sender, SelectTemplateEventArgs args)
                {
                    Verify.Fail("SelectTemplateKey event should not be raised when using a single template");
                };

                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems).Select(i => string.Format("Item #{0}", i)),
                   elementFactory: elementFactory,
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();
                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Text);
                }
            });
        }

        [TestMethod]
        public void ValidateDataTemplateAsItemTemplate()
        {
            RunOnUIThread.Execute(() =>
            {
                var dataTemplate = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' />
                        </DataTemplate>");
                ItemsRepeater repeater = null;
                const int numItems = 10;

                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems).Select(i => i.ToString()),
                   elementFactory: dataTemplate,
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();
                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Text);
                }

                repeater.ItemsSource = null;
                Content.UpdateLayout();

                // In versions below RS5 we faked the recycling behaivor on data template
                // so we can get to the recycle pool that we addded internally in ItemsRepeater.
                //if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone5))
                {
                    // All the created items should be in the recycle pool now.
                    var pool = RecyclePool.GetPoolInstance(dataTemplate);
                    var recycledElements = GetAllElementsFromPool(pool);
                    Verify.AreEqual(10, recycledElements.Count);
                }
            });
        }

        [TestMethod]
        public void ValidateDataTemplateSelectorAsItemTemplate()
        {
            RunOnUIThread.Execute(() =>
            {
                var dataTemplateOdd = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' Height='30' />
                        </DataTemplate>");
                var dataTemplateEven = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' Height='40' />
                        </DataTemplate>");
                ItemsRepeater repeater = null;
                const int numItems = 10;
                var selector = new MySelector()
                {
                    TemplateOdd = dataTemplateOdd,
                    TemplateEven = dataTemplateEven
                };

                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems),
                   elementFactory: selector,
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();
                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Text);
                    Verify.AreEqual(i % 2 == 0 ? 40 : 30, element.Height);
                }

                repeater.ItemsSource = null;
                Content.UpdateLayout();

                // In versions below RS5 we faked the recycling behaivor on data template
                // so we can get to the recycle pool that we addded internally in ItemsRepeater.
                //if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone5))
                {
                    // All the created items should be in the recycle pool now.
                    var oddPool = RecyclePool.GetPoolInstance(dataTemplateOdd);
                    var oddElements = GetAllElementsFromPool(oddPool);
                    Verify.AreEqual(5, oddElements.Count);
                    foreach (var element in oddElements)
                    {
                        Verify.AreEqual(30, ((TextBlock)element).Height);
                    }

                    var evenPool = RecyclePool.GetPoolInstance(dataTemplateEven);
                    var evenElements = GetAllElementsFromPool(evenPool);
                    Verify.AreEqual(5, evenElements.Count);
                    foreach (var element in evenElements)
                    {
                        Verify.AreEqual(40, ((TextBlock)element).Height);
                    }
                }
            });
        }

        [TestMethod]
        public void ValidateNoSizeWhenEmptyDataTemplate()
        {
            ItemsRepeater repeater = null;
            RunOnUIThread.Execute(() =>
            {
                var elementFactory = new RecyclingElementFactory();
                elementFactory.RecyclePool = new RecyclePool();
                elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");

                repeater = new ItemsRepeater()
                {
                    ItemsSource = Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)),
                    ItemTemplate = elementFactory,
                    // Default is StackLayout, so do not have to explicitly set.
                    // Layout = new StackLayout(),
                };
                repeater.UpdateLayout();

                // Asserting render size is zero
                Verify.IsLessThan(repeater.RenderSize.Width, 0.0001);
                Verify.IsLessThan(repeater.RenderSize.Height, 0.0001);
            });
        }

        [TestMethod]
        public void ValidateCorrectSizeWhenEmptyDataTemplateInSelector()
        {
            RunOnUIThread.Execute(() =>
            {
                var dataTemplateOdd = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' Height='30' Width='50' />
                        </DataTemplate>");
                var dataTemplateEven = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");
                ItemsRepeater repeater = null;
                const int numItems = 10;
                var selector = new MySelector()
                {
                    TemplateOdd = dataTemplateOdd,
                    TemplateEven = dataTemplateEven
                };

                repeater = new ItemsRepeater()
                {
                    ItemTemplate = selector,
                    Layout = new StackLayout(),
                    ItemsSource = Enumerable.Range(0, numItems)
                };

                repeater.VerticalAlignment = VerticalAlignment.Top;
                repeater.HorizontalAlignment = HorizontalAlignment.Left;
                Content = repeater;

                Content.UpdateLayout();
                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (FrameworkElement)repeater.TryGetElement(i);
                    if (i % 2 == 0)
                    {
                        Verify.AreEqual(element.Height, 0);
                    }
                    else
                    {
                        Verify.AreEqual(element.Height, 30);
                    }
                }

                Verify.AreEqual(5 * 30, repeater.ActualHeight);

                // ItemsRepeater stretches page, so actual width is width of page and not 50
                //Verify.AreEqual(50, repeater.ActualWidth);


                repeater.ItemsSource = null;
                Content.UpdateLayout();
            });
        }

        [TestMethod]
        public void ValidateReyclingElementFactoryWithNoTemplate()
        {
            RunOnUIThread.Execute(() =>
            {
                var elementFactory = new RecyclingElementFactoryDerived()
                {
                    RecyclePool = new RecyclePool()
                };

                Verify.Throws<InvalidOperationException>(delegate
                {
                    var context = (ElementFactoryGetArgs)RepeaterTestHooks.CreateRepeaterElementFactoryGetArgs();
                    context.Parent = null;
                    context.Data = 0;

                    elementFactory.GetElement(context);
                });
            });
        }

        // Validate ability to create and use a view generator from scratch
        [TestMethod]
        public void ValidateCustomElementFactory()
        {
            RunOnUIThread.Execute(() =>
            {
                ItemsRepeater repeater = null;
                const int numItems = 10;
                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems),
                   elementFactory: new MockElementFactory()
                   {
                       GetElementFunc = delegate (int index, UIElement owner)
                       {
                           return new Button() { Content = index.ToString() };
                       },
                       ClearElementFunc = delegate (UIElement element, UIElement owner)
                       {
                           Verify.AreEqual(typeof(Button), element.GetType());
                       }
                   },
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();
                for (int i = 0; i < numItems; i++)
                {
                    var element = (Button)repeater.TryGetElement(i);
                    Verify.AreEqual(i.ToString(), element.Content.ToString());
                }
            });
        }

        [TestMethod]
        public void ValidateTemplateSwitchingRefreshesElementsVirtualizingLayout()
        {
            RunOnUIThread.Execute(() =>
            {
                ValidateTemplateSwitchingRefreshesElements(new StackLayout());
            });
        }

        [TestMethod]
        public void ValidateTemplateSwitchingRefreshesElementsNonVirtualizingLayout()
        {
            RunOnUIThread.Execute(() =>
            {
                ValidateTemplateSwitchingRefreshesElements(new NonVirtualStackLayout());
            });
        }

        public void ValidateTemplateSwitchingRefreshesElements(Layout layout)
        {
            var dataTemplate1 = (DataTemplate)XamlReader.Parse(
                     @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' />
                       </DataTemplate>");

            var dataTemplate2 = (DataTemplate)XamlReader.Parse(
                    @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <Button Content='{Binding}' />
                      </DataTemplate>");
            ItemsRepeater repeater = null;
            const int numItems = 10;
            Content = CreateAndInitializeRepeater
            (
               itemsSource: Enumerable.Range(0, numItems).Select(i => i.ToString()),
               elementFactory: dataTemplate1,
               layout: layout,
               repeater: ref repeater
            );

            Content.UpdateLayout();
            Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
            for (int i = 0; i < numItems; i++)
            {
                var element = (TextBlock)repeater.TryGetElement(i);
                Verify.AreEqual(i.ToString(), element.Text);
            }

            repeater.ItemTemplate = dataTemplate2;
            Content.UpdateLayout();

            // The old elements have been recycled but still parented under this repeater.
            Verify.AreEqual(numItems * 2, VisualTreeHelper.GetChildrenCount(repeater));
            for (int i = 0; i < numItems; i++)
            {
                var element = (Button)repeater.TryGetElement(i);
                Verify.AreEqual(i.ToString(), element.Content);
            }
        }

        [TestMethod]
        public void ValidateNullItemTemplateAndContainerInItems()
        {
            RunOnUIThread.Execute(() =>
            {
                const int numItems = 10;
                ItemsRepeater repeater = null;
                Content = CreateAndInitializeRepeater
                (
                    itemsSource: Enumerable.Range(0, numItems).Select(i => new Button() { Content = i }), // ItemsSource is UIElements
                    elementFactory: null, // No ItemTemplate
                    layout: new StackLayout(),
                    repeater: ref repeater
                );

                Content.UpdateLayout();

                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));

                for (int i = 0; i < numItems; i++)
                {
                    var element = (Button)repeater.TryGetElement(i);
                    Verify.AreEqual(i, element.Content);
                }

                Button button0 = (Button)repeater.TryGetElement(0);
                Verify.IsNotNull(button0.Parent);

                repeater.ItemsSource = null;
                Verify.IsNull(button0.Parent);
            });
        }

        [TestMethod]
        public void VerifySelectTemplateLayoutFallback()
        {
            RunOnUIThread.Execute(() =>
            {
                var dataTemplateOdd = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='static' Height='30' />
                        </DataTemplate>");
                var dataTemplateEven = (DataTemplate)XamlReader.Parse(
                        @"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' Height='30' />
                        </DataTemplate>");
                ItemsRepeater repeater = null;
                const int numItems = 10;
                var selector = new MyContainerSelector()
                {
                    TemplateOdd = dataTemplateOdd,
                    TemplateEven = dataTemplateEven
                };

                Content = CreateAndInitializeRepeater
                (
                   itemsSource: Enumerable.Range(0, numItems),
                   elementFactory: selector,
                   layout: new StackLayout(),
                   repeater: ref repeater
                );

                Content.UpdateLayout();

                Verify.AreEqual(numItems, VisualTreeHelper.GetChildrenCount(repeater));
                for (int i = 0; i < numItems; i++)
                {
                    var element = (TextBlock)repeater.TryGetElement(i);
                    if (i % 2 == 0)
                    {
                        // Text is bound to the data for even indices
                        Verify.AreEqual(i.ToString(), element.Text);
                        Verify.AreEqual(i, element.DataContext);
                    }
                    else
                    {
                        // Text explicitly set on the element only for odd indices
                        Verify.AreEqual("static", element.Text);
                    }
                }
            });
        }

        [TestMethod]
        public void VerifyNullTemplateGivesMeaningfullError()
        {
            RunOnUIThread.Execute(() =>
            {
                ItemsRepeater repeater = null;
                const int numItems = 10;

                Content = CreateAndInitializeRepeater
                (
                    itemsSource: Enumerable.Range(0, numItems),
                    // DataTemplateSelector always returns null, but throws hresult_invalid_argument when container is null
                    elementFactory: new DataTemplateSelector(),
                    layout: new StackLayout(),
                    repeater: ref repeater
                );
                bool threwException = false;
                try
                {
                    Content.UpdateLayout();
                }
                catch (Exception e)
                {
                    threwException = true;
                    Verify.IsTrue(e.Message.Contains("Null encountered as data template. That is not a valid value for a data template, and can not be used."));
                }
                Verify.IsTrue(threwException);
                // Set content to null so testapp does not try to update layout again
                Content = null;
            });
        }
        private ItemsRepeaterScrollHost CreateAndInitializeRepeater(
            object itemsSource,
            Layout layout,
            object elementFactory,
            ref ItemsRepeater repeater)
        {
            repeater = new ItemsRepeater()
            {
                ItemsSource = itemsSource,
                Layout = layout,
                ItemTemplate = elementFactory,
            };

            return new ItemsRepeaterScrollHost()
            {
                Width = 400,
                Height = 400,
                ScrollViewer = new System.Windows.Controls.ScrollViewer()
                {
                    Content = repeater
                }
            };
        }

        private List<UIElement> GetAllElementsFromPool(RecyclePool pool, string key = "")
        {
            List<UIElement> elements = new List<UIElement>();
            bool poolEmpty = false;
            while (!poolEmpty)
            {
                var next = pool.TryGetElement(key);
                if (next != null)
                {
                    elements.Add(next);
                }

                poolEmpty = next == null;
            }

            return elements;
        }
    }

    public class MySelector : DataTemplateSelector
    {
        public DataTemplate TemplateOdd { get; set; }

        public DataTemplate TemplateEven { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return (((int)item) % 2 == 0) ? TemplateEven : TemplateOdd;
        }
    }

    public class MyContainerSelector : DataTemplateSelector
    {
        public DataTemplate TemplateOdd { get; set; }

        public DataTemplate TemplateEven { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return (((int)item) % 2 == 0) ? TemplateEven : TemplateOdd;
        }

    }
}
