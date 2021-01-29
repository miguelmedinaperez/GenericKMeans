/*
 * Created by: Miguel Angel Medina Pérez (miguelmedinaperez@gmail.com)
 * Created: 11/16/2016
 * Comments by: Miguel Angel Medina Pérez (miguelmedinaperez@gmail.com)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PRFramework.Core.Common
{
    /// <summary>
    ///     Interface that allows sampling objects from a collection.
    /// </summary>
    /// <typeparam name="T">The type of the objects in the collection.</typeparam>
    public interface ISampler<T>
    {
        IEnumerable<T> GetSample(IEnumerable<T> population, int sampleCount);
    }
}
