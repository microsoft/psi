// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Input
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// Displays wait cursor during lengthy operations. Uses dispose pattern to clean up.
    /// </summary>
    public class WaitCursor : IDisposable
    {
        private Cursor previousCursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitCursor"/> class.
        /// </summary>
        public WaitCursor()
        {
            this.previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Mouse.OverrideCursor = this.previousCursor;
        }
    }
}
