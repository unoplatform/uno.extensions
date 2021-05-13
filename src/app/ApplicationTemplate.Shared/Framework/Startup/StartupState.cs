using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate
{
    /// <summary>
    /// General startup information.
    /// </summary>
    public class StartupState
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the startup is pre-initialized.
        /// </summary>
        public bool IsPreInitialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the startup is initialized.
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the startup is started.
        /// </summary>
        public bool IsStarted { get; set; }
    }
}
