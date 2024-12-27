using MERX.Features.Enums;
using UnityEngine;

namespace MERX.Features
{
    /// <summary>
    /// Base class for all schematic extensions.
    /// </summary>
    public abstract class SchematicExtensionBase : MonoBehaviour
    {
        /// <summary>
        /// The type of the extension.
        /// </summary>
        public abstract ExtensionType Type { get; }

        /// <summary>
        /// Compiles the extension.
        /// </summary>
        /// <returns>True if the extension was compiled successfully, false otherwise.</returns>
        public abstract bool Compile(bool suppressLogs = false);
    }
}