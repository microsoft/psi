// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Base
{
    using System;

    /// <summary>
    /// A command whose sole purpose is to relay its functionality to other objects by invoking delegates.
    /// The default return value for the CanExecute method is 'true'. This class does not allow you to accept
    /// command parameters in the Execute and CanExecute callback methods.
    /// </summary>
    public class RelayCommand : RelayCommand<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            : base(execute, canExecute)
        {
        }
    }
}
