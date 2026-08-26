using System;

namespace Veles.Core
{
    public static class ProductInfo
    {
        public const string VersionText = "0.1.0";
        public static Version Version { get { return new Version(VersionText); } }
    }
}
