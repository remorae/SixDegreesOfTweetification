﻿using System.Collections.Generic;

namespace SixDegrees.Model
{
    class CountryResult
    {
        internal string Name { get; }
        internal IEnumerable<PlaceResult> Places { get; }

        internal CountryResult(string name, IEnumerable<PlaceResult> places)
        {
            Name = name;
            Places = places;
        }
    }
}
