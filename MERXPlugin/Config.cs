using System.ComponentModel;
using Exiled.API.Interfaces;

namespace MERXPlugin
{
    public class Config : IConfig
    {
        /// <inheritdoc />
        [Description("Indicates whether the plugin is enabled or not.")]
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        [Description("Indicates whether the plugin is in debug mode or not. (This will fill up your console with walls of green text)")]
        public bool Debug { get; set; }
    }
}