using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SWIMWithSimulator
{
    class AddressComparer : IEqualityComparer<Address>
    {
        public bool Equals([AllowNull] Address x, [AllowNull] Address y)
        {
            if (x is null || y is null) return false;
            return x == y;
        }

        public int GetHashCode([DisallowNull] Address obj)
        {
            return obj.Id;
        }
    }
}
