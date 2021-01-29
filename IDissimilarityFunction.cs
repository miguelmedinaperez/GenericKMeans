/*
 * Created by: Milton Garcia Borroto
 * Created: 1/5/2007
 * Comments by: Miguel Angel Medina Pérez (miguelmedinaperez@gmail.com)
 */

namespace PRFramework.Core.ComparisonFunctions
{
    /// <summary>
    ///     Represents a dissimilarity comparison function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The dissimilarity function compares objects and returns a degree of similarity between the objects. The lower the returned value, the greater is the similarity.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of the objects that can be compared.
    /// </typeparam>
    public interface IDissimilarityFunction<T>
    {
        double Compare(T source, T compareTo);
    }
}
