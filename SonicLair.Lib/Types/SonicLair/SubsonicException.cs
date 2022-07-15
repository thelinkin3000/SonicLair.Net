using System;

namespace SonicLair.Lib.Types.SonicLair
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "I don't even know what that is, but this probably won't be serialized ever")]
    public class SubsonicException : Exception
    {
        public SubsonicException(string message) : base(message)
        {
        }

        public override string ToString()
        {
            return this.Message;
        }
    }
}
