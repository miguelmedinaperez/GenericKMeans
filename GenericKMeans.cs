using System;
using System.Collections.Generic;
using System.Linq;
using PRFramework.Core.Common;
using PRFramework.Core.ComparisonFunctions;

namespace PRFramework.Core.Clustering
{
    [Serializable()]
    public class GenericKMeans<T> where T : class
    {
        public int ClusterCount { set; get; }

        public int MaxIterationCount { set; get; }

        public GenericKMeans(IDissimilarityFunction<T> dissimilarityFunction, IEqualityComparer<T> equalityComparer, ICentroidsCalculator<T> centroidsCalculator, IFactory<T> objFactory, ISampler<T> sampler)
        {
            if (dissimilarityFunction == null)
                throw new ArgumentNullException(nameof(dissimilarityFunction), $"Unable to initialize ${nameof(GenericKMeans<T>)} with null dissimilarity function.");

            if (equalityComparer == null)
                throw new ArgumentNullException(nameof(equalityComparer), $"Unable to initialize ${nameof(GenericKMeans<T>)} with null equality comparer.");

            if (centroidsCalculator == null)
                throw new ArgumentNullException(nameof(centroidsCalculator), $"Unable to initialize ${nameof(GenericKMeans<T>)} with null centroids calculator.");

            _dissimilarityFunction = dissimilarityFunction;
            _equalityComparer = equalityComparer;
            _centroidsCalculator = centroidsCalculator;
            _objFactory = objFactory;
            _sampler = sampler;
            ClusterCount = 2;
            MaxIterationCount = 100;
        }

        public IEnumerable<IEnumerable<T>> FindClusters(IEnumerable<T> objects)
        {
            return FindClustersAndCenters(objects).Values;
        }

        public Dictionary<T, List<T>> FindClustersAndCenters(IEnumerable<T> objects)
        {
            #region preconditions
            if (ClusterCount < 2)
                throw new InvalidOperationException("Unable to apply K-Means clustering algorithm: The cluster count must be greater than 1.");

            if (MaxIterationCount < 1)
                throw new InvalidOperationException("Unable to apply K-Means clustering algorithm: The iteration count must be greater than 0.");

            if (objects == null)
                throw new ArgumentNullException(nameof(objects), "Unable to apply K-Means clustering algorithm: Null dataset.");
            #endregion

            var objectsList = (objects is IList<T>) ? (IList<T>)objects : objects.ToList();
            if (objectsList.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(objects), "Unable to apply K-Means clustering algorithm: Empty dataset.");

            // Selecting K random centers and associating empty clusters to those centers
            var centers = _sampler.GetSample(objectsList, ClusterCount).ToList();
            var clusterByCenter = new Dictionary<T, List<T>>(ClusterCount);

            // Iterative finding clusters while centers vary
            bool didCentersChanged = true;
            for (int i = 0; i < MaxIterationCount && didCentersChanged; i++)
            {
                clusterByCenter.Clear();
                foreach (var instance in centers)
                    clusterByCenter.Add(instance, new List<T>());

                foreach (var instance in objectsList)
                {
                    var lessDissimilarCenter = FindLessDissimilarCenter(centers, clusterByCenter, instance);

                    if (lessDissimilarCenter == null)
                        clusterByCenter[centers[random.Next(ClusterCount)]].Add(instance);
                    else
                        clusterByCenter[lessDissimilarCenter].Add(instance);
                }

                didCentersChanged = UpdateCenters(ref centers, ref clusterByCenter);
            }

            return clusterByCenter;
        }

        private T FindLessDissimilarCenter(IList<T> centers, Dictionary<T, List<T>> clusterByCenter, T instance)
        {
            double minDissimilarity = double.MaxValue;
            T mostSimilarCenter = null;
            foreach (var center in centers)
            {
                var d = _dissimilarityFunction.Compare(instance, center);
                if (!double.IsPositiveInfinity(d) && (d < minDissimilarity || (d == minDissimilarity && _equalityComparer.Equals(instance, center))))
                {
                    minDissimilarity = d;
                    mostSimilarCenter = center;
                }
            }

            if (mostSimilarCenter == null)
                foreach (var center in centers)
                {
                    double dissimilaritySum = 0;
                    int count = 0;
                    foreach (var clusterInstance in clusterByCenter[center])
                    {
                        double d = _dissimilarityFunction.Compare(instance, clusterInstance);
                        if (!double.IsPositiveInfinity(d))
                        {
                            dissimilaritySum += d;
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        double dissimilarityAvg = dissimilaritySum / count;

                        if (dissimilarityAvg < minDissimilarity)
                        {
                            minDissimilarity = dissimilarityAvg;
                            mostSimilarCenter = center;
                        }
                    }
                }

            return mostSimilarCenter;
        }

        private bool UpdateCenters(ref List<T> centers, ref Dictionary<T, List<T>> clusterByCenter)
        {
            var didCentersChanged = false;
            var newClusterByCenter = new Dictionary<T, List<T>>(ClusterCount);
            for (int j = 0; j < centers.Count; j++)
            {
                if (clusterByCenter[centers[j]].Count == 0)
                    throw new InvalidOperationException("Unable to apply KMeans: Empty cluster found. Verify the components of KMeans.");

                T newCenter = null;
                foreach (var candidateCenter in _centroidsCalculator.Calculate(clusterByCenter[centers[j]], _objFactory))
                    if (!newClusterByCenter.ContainsKey(candidateCenter))
                    {
                        newCenter = candidateCenter;
                        break;
                    }

                if (newCenter == null)
                    throw new InvalidOperationException("Unable to apply KMeans: Two clusters with the same center. Verify the components of KMeans.");

                newClusterByCenter.Add(newCenter, clusterByCenter[centers[j]]);
                if (!_equalityComparer.Equals(centers[j], newCenter))
                {
                    didCentersChanged = true;
                    centers[j] = newCenter;
                }
            }

            clusterByCenter = newClusterByCenter;

            return didCentersChanged;
        }

        private static Random random = new Random((int)DateTime.Now.Ticks);

        private readonly ISampler<T> _sampler;

        private readonly ICentroidsCalculator<T> _centroidsCalculator;

        private readonly IDissimilarityFunction<T> _dissimilarityFunction;

        private readonly IEqualityComparer<T> _equalityComparer;

        private readonly IFactory<T> _objFactory;
    }
}
