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
    ///     The class implementing this interface allows creating specific type of objects.
    /// </summary>
    /// <typeparam name="T">The type of object created by the factory.</typeparam>
    public interface IFactory<T>
    {
        T GetNew();
    }
}
