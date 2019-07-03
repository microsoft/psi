// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using Microsoft.Psi.Media_Interop;

    /// <summary>
    /// Defines.
    /// </summary>
    public class MediaCaptureInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureInfo"/> class.
        /// </summary>
        /// <param name="device">Media capture device to query for parameter info.</param>
        public MediaCaptureInfo(MediaCaptureDevice device)
        {
            this.BacklightCompensationInfo = this.GetInfo(VideoProperty.BacklightCompensation, device);
            this.BrightnessInfo = this.GetInfo(VideoProperty.Brightness, device);
            this.ColorEnableInfo = this.GetInfo(VideoProperty.ColorEnable, device);
            this.ContrastInfo = this.GetInfo(VideoProperty.Contrast, device);
            this.GainInfo = this.GetInfo(VideoProperty.Gain, device);
            this.GammaInfo = this.GetInfo(VideoProperty.Gamma, device);
            this.HueInfo = this.GetInfo(VideoProperty.Hue, device);
            this.SaturationInfo = this.GetInfo(VideoProperty.Saturation, device);
            this.SharpnessInfo = this.GetInfo(VideoProperty.Sharpness, device);
            this.WhiteBalanceInfo = this.GetInfo(VideoProperty.WhiteBalance, device);
            this.FocusInfo = this.GetInfo(ManagedCameraControlProperty.Focus, device);
        }

        /// <summary>
        /// Gets a value that defines attributes about of the Backlight Compensation property.
        /// </summary>
        public PropertyInfo BacklightCompensationInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Brightness property.
        /// </summary>
        public PropertyInfo BrightnessInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the ColorEnable property.
        /// </summary>
        public PropertyInfo ColorEnableInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Contrast property.
        /// </summary>
        public PropertyInfo ContrastInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Focus property.
        /// </summary>
        public PropertyInfo FocusInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Gain property.
        /// </summary>
        public PropertyInfo GainInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Gamma property.
        /// </summary>
        public PropertyInfo GammaInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Hue property.
        /// </summary>
        public PropertyInfo HueInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Saturation property.
        /// </summary>
        public PropertyInfo SaturationInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the Sharpness property.
        /// </summary>
        public PropertyInfo SharpnessInfo { get; }

        /// <summary>
        /// Gets a value that defines attributes about of the WhiteBalance property.
        /// </summary>
        public PropertyInfo WhiteBalanceInfo { get; }

        private PropertyInfo GetInfo(ManagedCameraControlProperty prop, MediaCaptureDevice device)
        {
            PropertyInfo info = new PropertyInfo();
            int min = 0, max = 0, stepSize = 0, defValue = 0, flag = 0;
            if (device.GetRange(prop, ref min, ref max, ref stepSize, ref defValue, ref flag))
            {
                info.MinValue = min;
                info.MaxValue = max;
                info.StepSize = stepSize;
                info.DefaultValue = defValue;
                info.AutoControlled = ((flag & (int)VideoPropertyFlags.Auto) != 0) ? true : false;
                info.Supported = true;
            }
            else
            {
                info.Supported = false;
            }

            return info;
        }

        private PropertyInfo GetInfo(VideoProperty prop, MediaCaptureDevice device)
        {
            PropertyInfo info = new PropertyInfo();
            int min = 0, max = 0, stepSize = 0, defValue = 0, flag = 0;
            if (device.GetRange(prop, ref min, ref max, ref stepSize, ref defValue, ref flag))
            {
                info.MinValue = min;
                info.MaxValue = max;
                info.StepSize = stepSize;
                info.DefaultValue = defValue;
                info.AutoControlled = ((flag & (int)VideoPropertyFlags.Auto) != 0) ? true : false;
                info.Supported = true;
            }
            else
            {
                info.Supported = false;
            }

            return info;
        }

        /// <summary>
        /// PropertyInfo defines attributes about various properties exposed by the media capture device.
        /// </summary>
        public class PropertyInfo
        {
            /// <summary>
            /// Gets or sets a value indicating whether the property is currently supported by the device.
            /// </summary>
            public bool Supported { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the minimum value of the property allowed by the device.
            /// </summary>
            public int MinValue { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the maximum value of the property allowed by the device.
            /// </summary>
            public int MaxValue { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the step size for the value of the property.
            /// </summary>
            public int StepSize { get; set; }

            /// <summary>
            /// Gets or sets a value indicating the default value of the property.
            /// </summary>
            public int DefaultValue { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the property can be automatically controlled by the device.
            /// </summary>
            public bool AutoControlled { get; set; }
        }
    }
}
