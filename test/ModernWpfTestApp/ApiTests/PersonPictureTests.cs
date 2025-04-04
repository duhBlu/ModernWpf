﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MUXControlsTestApp.Utilities;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using PersonPicture = ModernWpf.Controls.PersonPicture;

namespace ModernWpf.Tests.MUXControls.ApiTests
{
    [TestClass]
    public class PersonPictureTests : ApiTestBase
    {
        [TestMethod]
        public void VerifyDefaultsAndBasicSetting()
        {
            RunOnUIThread.Execute(() =>
            {
                PersonPicture personPicture = new PersonPicture();
                Verify.IsNotNull(personPicture);

                // Confirm initial dependency property values
                Verify.AreEqual(personPicture.BadgeGlyph, "");
                Verify.AreEqual(personPicture.BadgeNumber, 0);
                Verify.AreEqual(personPicture.IsGroup, false);
                //Verify.AreEqual(personPicture.PreferSmallImage, false);
                Verify.AreEqual(personPicture.ProfilePicture, null);
                //Verify.AreEqual(personPicture.Contact, null);
                Verify.AreEqual(personPicture.DisplayName, "");
                Verify.AreEqual(personPicture.Initials, "");

                // Now verify setting/retrieving the parameters
                personPicture.BadgeGlyph = "\uE765";
                Verify.AreEqual(personPicture.BadgeGlyph, "\uE765");
                personPicture.BadgeNumber = 10;
                Verify.AreEqual(personPicture.BadgeNumber, 10);
                personPicture.IsGroup = true;
                Verify.AreEqual(personPicture.IsGroup, true);
                //personPicture.PreferSmallImage = true;
                //Verify.AreEqual(personPicture.PreferSmallImage, true);
                personPicture.DisplayName = "Some Name";
                Verify.AreEqual(personPicture.DisplayName, "Some Name");
                personPicture.Initials = "MS";
                Verify.AreEqual(personPicture.Initials, "MS");

                /*
                Contact contact = new Contact();
                contact.FirstName = "FirstName";
                personPicture.Contact = contact;
                Verify.AreEqual(personPicture.Contact.FirstName, "FirstName");
                */

                ImageSource imageSrc = new BitmapImage(new Uri("pack://application:,,,/Assets/StoreLogo.png"));
                personPicture.ProfilePicture = imageSrc;
                Verify.IsNotNull(personPicture.ProfilePicture);
            });
        }

        [TestMethod]
        public void VerifyAutomationName()
        {
            RunOnUIThread.Execute(() =>
            {
                PersonPicture personPicture = new PersonPicture();
                Verify.IsNotNull(personPicture);

                // Set properties and ensure that the AutomationName updates accordingly
                personPicture.Initials = "AB";
                String automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "AB");

                personPicture.DisplayName = "Jane Smith";
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "Jane Smith");

                /*
                Contact contact = new Contact();
                contact.FirstName = "John";
                contact.LastName = "Doe";
                personPicture.Contact = contact;
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "John Doe");
                */
                personPicture.DisplayName = "John Doe";

                personPicture.IsGroup = true;
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "Group");
                personPicture.IsGroup = false;

                personPicture.BadgeGlyph = "\uE765";
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "John Doe, icon");

                personPicture.BadgeText = "Skype";
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "John Doe, Skype");
                personPicture.BadgeText = "";

                personPicture.BadgeNumber = 5;
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "John Doe, 5 items");

                personPicture.BadgeText = "direct reports";
                automationName = AutomationProperties.GetName(personPicture);
                Verify.AreEqual(automationName, "John Doe, 5 direct reports");
            });
        }

        [TestMethod]
        public void VerifySmallWidthAndHeightDoNotCrash()
        {
            ManualResetEvent sizeChangedEvent = new ManualResetEvent(false);
            RunOnUIThread.Execute(() =>
            {
                var personPicture = new PersonPicture();
                Content = personPicture;
                Content.UpdateLayout();
                personPicture.SizeChanged += (sender, args) => sizeChangedEvent.Set();
                personPicture.Width = 0.4;
                personPicture.Height = 0.4;
                Content.UpdateLayout();
            });
            sizeChangedEvent.WaitOne();
        }

        [TestMethod]
        public void VerifyVSMStatesForPhotosAndInitials()
        {
            RunOnUIThread.Execute(() =>
            {
                var personPicture = new PersonPicture();
                Content = personPicture;
                Content.UpdateLayout();
                var initialsTextBlock = (TextBlock)VisualTreeUtils.FindVisualChildByName(personPicture, "InitialsTextBlock");
                var placeholderIcon = (Controls.FontIconFallback)VisualTreeUtils.FindVisualChildByName(personPicture, "PlaceholderIcon");
                personPicture.IsGroup = true;
                Content.UpdateLayout();
                //Verify.IsTrue(initialsTextBlock.FontFamily.Source.Contains("Segoe MDL2 Assets"));
                //Verify.AreEqual(initialsTextBlock.Text, "\xE716");
                Verify.IsTrue(initialsTextBlock.Visibility == System.Windows.Visibility.Collapsed);
                Verify.IsTrue(placeholderIcon.Visibility == System.Windows.Visibility.Visible);
                Verify.IsTrue(placeholderIcon.Data == placeholderIcon.FindResource("People"));

                personPicture.IsGroup = false;
                personPicture.Initials = "JS";
                Content.UpdateLayout();
                Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI");
                Verify.AreEqual(initialsTextBlock.Text, "JS");

                personPicture.Initials = "";
                Content.UpdateLayout();
                //Verify.IsTrue(initialsTextBlock.FontFamily.Source.Contains("Segoe MDL2 Assets"));
                //Verify.AreEqual(initialsTextBlock.Text, "\xE77B");
                Verify.IsTrue(initialsTextBlock.Visibility == System.Windows.Visibility.Collapsed);
                Verify.IsTrue(placeholderIcon.Visibility == System.Windows.Visibility.Visible);
                Verify.IsTrue(placeholderIcon.Data == placeholderIcon.FindResource("Contact"));

                // Make sure that custom FontFamily takes effect after the control is created
                // and also goes back to the MDL2 font after setting IsGroup = true.
                personPicture.FontFamily = new FontFamily("Segoe UI Emoji");
                personPicture.Initials = "👍";
                Content.UpdateLayout();
                Verify.AreEqual(initialsTextBlock.FontFamily.Source, "Segoe UI Emoji");
                Verify.AreEqual(initialsTextBlock.Text, "👍");

                personPicture.IsGroup = true;
                Content.UpdateLayout();
                //Verify.IsTrue(initialsTextBlock.FontFamily.Source.Contains("Segoe MDL2 Assets"));
                //Verify.AreEqual(initialsTextBlock.Text, "\xE716");
                Verify.IsTrue(initialsTextBlock.Visibility == System.Windows.Visibility.Collapsed);
                Verify.IsTrue(placeholderIcon.Visibility == System.Windows.Visibility.Visible);
                Verify.IsTrue(placeholderIcon.Data == placeholderIcon.FindResource("People"));
            });
        }
    }
}
