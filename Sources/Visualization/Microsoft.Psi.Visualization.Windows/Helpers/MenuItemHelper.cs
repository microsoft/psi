// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Methods to assist in building menus.
    /// </summary>
    public static class MenuItemHelper
    {
        private static readonly Style MenuItemTextBlockStyle = Application.Current.FindResource("MenuItemTextBlock") as Style;
        private static readonly Style MenuItemIconStyle = Application.Current.FindResource("MenuItemIcon") as Style;

        /// <summary>
        /// Creates a menuitem.
        /// </summary>
        /// <param name="iconSourcePath">The path to the icon to display next to the menuitem.</param>
        /// <param name="text">The text of the menu command.</param>
        /// <param name="command">The command object.</param>
        /// <param name="tag">An optional, user-defined tag to attach to the menuitem (default is null).</param>
        /// <param name="isEnabled">An optional variable indicating whether the menu item is enabled (default is true).</param>
        /// <param name="commandParameter">The command parameter, or null if the command does not take a parameter.</param>
        /// <returns>A new menuitem.</returns>
        public static MenuItem CreateMenuItem(string iconSourcePath, string text, ICommand command, object tag = null, bool isEnabled = true, object commandParameter = null)
        {
            // Create the bitmap for the icon
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(iconSourcePath ?? string.Empty, UriKind.RelativeOrAbsolute);
            image.EndInit();

            // Create the icon
            var icon = new System.Windows.Controls.Image
            {
                Height = 16,
                Width = 16,
                Margin = new Thickness(4, 0, 0, 0),
                Source = image,
                Style = MenuItemIconStyle,
            };

            var menuItemHeader = new TextBlock
            {
                Text = text,
                Style = MenuItemTextBlockStyle,
            };

            // Create the menuitem
            return new MenuItem
            {
                Height = 25,
                Icon = icon,
                Header = menuItemHeader,
                Command = command,
                CommandParameter = commandParameter,
                Tag = tag,
                IsEnabled = isEnabled,
            };
        }

        /// <summary>
        /// Creates a menuitem.
        /// </summary>
        /// <param name="text">The text of the menu command.</param>
        /// <param name="borderColor">The annotation border color.</param>
        /// <param name="fillColor">The annotation fill color.</param>
        /// <param name="command">The command object.</param>
        /// <returns>A new menuitem.</returns>
        public static MenuItem CreateAnnotationMenuItem(string text, Color borderColor, Color fillColor, ICommand command)
        {
            // Create the bitmap for the icon, filling in with the fill color and using the border color.
            var image = new BitmapImage();
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, 16, 16),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* dst = (byte*)bitmapData.Scan0.ToPointer();
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        var offset = (i * 16 + j) * 4;
                        if (i == 0 || i == 15 || j == 0 || j == 15)
                        {
                            *(dst + offset + 0) = borderColor.B;
                            *(dst + offset + 1) = borderColor.G;
                            *(dst + offset + 2) = borderColor.R;
                            *(dst + offset + 3) = borderColor.A;
                        }
                        else
                        {
                            *(dst + offset + 0) = fillColor.B;
                            *(dst + offset + 1) = fillColor.G;
                            *(dst + offset + 2) = fillColor.R;
                            *(dst + offset + 3) = fillColor.A;
                        }
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);

            // Save to a memory stream and assign image from it.
            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Bmp);

            image.BeginInit();
            image.StreamSource = memoryStream;
            image.EndInit();

            // Create the icon
            var icon = new System.Windows.Controls.Image
            {
                Height = 16,
                Width = 16,
                Margin = new Thickness(4, 0, 0, 0),
                Source = image,
                Style = MenuItemIconStyle,
            };

            var menuItemHeader = new TextBlock
            {
                Text = text,
                Style = MenuItemTextBlockStyle,
            };

            // Create the menuitem
            return new MenuItem
            {
                Height = 25,
                Icon = icon,
                Header = menuItemHeader,
                Command = command,
                IsEnabled = true,
            };
        }
    }
}
