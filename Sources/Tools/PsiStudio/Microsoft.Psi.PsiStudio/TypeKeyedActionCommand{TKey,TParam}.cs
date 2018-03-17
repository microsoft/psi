// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;

    /// <summary>
    /// Class for context menu commands based on stream types.
    /// </summary>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <typeparam name="TParam">The type of parameter passed to action.</typeparam>
    public class TypeKeyedActionCommand<TKey, TParam> : TypeKeyedActionCommand
    {
        private Action<TParam> action;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeKeyedActionCommand{TKey, TParam}"/> class.
        /// </summary>
        /// <param name="displayName">Name displayed in menu.</param>
        /// <param name="action">Action to be invoked when menu is clicked.</param>
        public TypeKeyedActionCommand(string displayName, Action<TParam> action)
            : base (displayName, typeof(TKey))
        {
            this.action = action;
        }

        /// <inheritdoc />
        public override void Execute(object parameter)
        {
            this.action((TParam)parameter);
        }
    }
}
