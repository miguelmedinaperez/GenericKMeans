using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PRFramework.Core.Common
{
    public interface ICentroidsCalculator<T>
    {
        IList<T> Calculate(IEnumerable<T> objects, IFactory<T> factory);
    }
}
