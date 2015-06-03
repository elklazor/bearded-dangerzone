using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bearded_Dangerzone.GamePart
{
    static class Extension
    {
        public static bool FindItem<T>(this List<T> toSearch, Func<T, bool> pred, out T output)
        {
            try
            {
                output = toSearch.First(pred);
                return true;
            }
            catch (Exception)
            {
                output = default(T);
                return false;
            }
        }
        
    }
}
