public class PRObjectKMeans : IClusteringAlgorithm<PRObject>
{
	public int ClusterCount { set; get; }

	public int MaxIterationCount { set; get; }

	public PRObjectKMeans(NormalizedEuclideanDistance<PRObjectVector> distance)
	{
		_distance = distance;
	}

	public IEnumerable<IEnumerable<PRObject>> FindClusters(IEnumerable<PRObject> objects)
	{
		var vectorsList = new List<PRObjectVector>();
		foreach (var prObject in objects)
			vectorsList.Add(new PRObjectVector(prObject));

		var vectorsClusters = FindClustersAndCenters(vectorsList).Values;

		var prObjectsClusters = new List<IEnumerable<PRObject>>();
		foreach (var vectorCluster in vectorsClusters)
		{
			var prObjectCluster = vectorCluster.Select(vector => vector.InnerPRObject);
			prObjectsClusters.Add(prObjectCluster);
		}

		return prObjectsClusters;
	}

	public Dictionary<PRObjectVector, List<PRObjectVector>> FindClustersAndCenters(List<PRObjectVector> vectorsList)
	{
		_metaObject = vectorsList[0].InnerPRObject.MetaObject;

		var kMeansPlusPlus =
			new GenericKMeans<PRObjectVector>(
				_distance,
				new VectorEqualityComparer(),
				new VectorsMeanCalculator<PRObjectVector>(),
				new PRObjectFactory(_metaObject),
				new KMeansPlusPlusSampler<PRObjectVector>(_distance))
			{
				ClusterCount = ClusterCount,
				MaxIterationCount = MaxIterationCount
			};

		return kMeansPlusPlus.FindClustersAndCenters(vectorsList);
	}

	private class PRObjectFactory : IFactory<PRObjectVector>
	{
		public PRObjectVector GetNew()
		{
			var prObject = _metaObject.GetNewObject();
			for (int i = 0; i < _metaObject.FeatureDescriptions.Length; i++)
				prObject[i] = 0.0;
			return new PRObjectVector(prObject);
		}

		public PRObjectFactory(MetaObject metaObject)
		{
			_metaObject = metaObject;
		}

		private readonly MetaObject _metaObject;
	}

	public class PRObjectVector : IVector
	{

		public double this[int index]
		{
			get { return (double)_innerPRObject[index]; }
			set { _innerPRObject[index] = value; }
		}

		public int Length => _innerPRObject.FeatureValues.Length;

		public PRObject InnerPRObject
		{
			get { return _innerPRObject; }
		}

		public PRObjectVector(PRObject innerPrObject)
		{
			_innerPRObject = innerPrObject;
		}

		private readonly PRObject _innerPRObject;
	}


	private readonly NormalizedEuclideanDistance<PRObjectVector> _distance;

	private MetaObject _metaObject;
}


public class NormalizedEuclideanDistance<TVector> : IDissimilarityFunction<TVector> where TVector : IVector
{
	public NormalizedEuclideanDistance(IEnumerable<TVector> vectors, IEnumerable<int> features)
	{
		if (features == null)
			throw new ArgumentNullException(nameof(features), $"Unable to instantiate ${nameof(NormalizedEuclideanDistance<TVector>)}: null feature collection.");
		if (!features.Any())
			throw new ArgumentOutOfRangeException(nameof(features), features, $"Unable to instantiate ${nameof(NormalizedEuclideanDistance<TVector>)}: empty feature collection.");

		double[] minFeatureValues = null;
		double[] maxFeatureValues = null;
		int vectorsCount = 0;
		foreach (var vector in vectors)
		{
			if (minFeatureValues == null)
			{
				minFeatureValues = new double[vector.Length];
				maxFeatureValues = new double[vector.Length];
				for (int i = 0; i < vector.Length; i++)
				{
					minFeatureValues[i] = vector[i];
					maxFeatureValues[i] = vector[i];
				}
			}
			else
			{
				for (int i = 0; i < vector.Length; i++)
				{
					if (vector[i] < minFeatureValues[i])
						minFeatureValues[i] = vector[i];
					else if (vector[i] > maxFeatureValues[i])
						maxFeatureValues[i] = vector[i];
				}
			}
			vectorsCount++;
		}

		if (vectorsCount < 1)
			throw new ArgumentOutOfRangeException(nameof(vectors), vectors, $"Unable to instantiate ${nameof(NormalizedEuclideanDistance<TVector>)}: empty vector collection.");

		_maxLessMin = new double[minFeatureValues.Length];
		for (int i = 0; i < minFeatureValues.Length; i++)
			_maxLessMin[i] = maxFeatureValues[i] - minFeatureValues[i];

		_features = features;
	}

	public static List<NormalizedEuclideanDistance<TVector>> CreateDistances(IEnumerable<TVector> vectors,
		IEnumerable<IEnumerable<int>> collectionOfFeaturesCollections)
	{
		if (collectionOfFeaturesCollections == null)
			throw new ArgumentNullException(nameof(collectionOfFeaturesCollections), $"Unable to instantiate ${nameof(NormalizedEuclideanDistance<TVector>)}: null collection of features collections.");

		var distancesList = new List<NormalizedEuclideanDistance<TVector>>();
		foreach (var featuresCollection in collectionOfFeaturesCollections)
			distancesList.Add(distancesList.Count == 0
				? new NormalizedEuclideanDistance<TVector>(vectors, featuresCollection)
				: new NormalizedEuclideanDistance<TVector>(distancesList[0]._maxLessMin, featuresCollection));

		return distancesList;
	}

	public double Compare(TVector v0, TVector v1)
	{
		if (v0.Length != _maxLessMin.Length)
			throw new ArgumentOutOfRangeException(nameof(v0), v0, "Unable to compare vectors with NormalizedEuclideanDistance<TVector>: At least one vector has invalid length!");
		if (v1.Length != _maxLessMin.Length)
			throw new ArgumentOutOfRangeException(nameof(v1), v1, "Unable to compare vectors with NormalizedEuclideanDistance<TVector>: At least one vector has invalid length!");

		double sum = 0;
		foreach (var i in _features)
			if (_maxLessMin[i] > 0)
			{
				double componentDiff = Math.Abs(v0[i] - v1[i]) / _maxLessMin[i];

				sum += Math.Pow(componentDiff, 2);
			}

		return Math.Sqrt(sum);
	}

	private NormalizedEuclideanDistance(double[] maxLessMin, IEnumerable<int> features)
	{
		_maxLessMin = maxLessMin;
		_features = features;
	}

	private readonly double[] _maxLessMin;

	private readonly IEnumerable<int> _features;
}


public class VectorEqualityComparer : EqualityComparer<IVector>
{
	public override bool Equals(IVector v1, IVector v2)
	{
		if (v1 == null && v2 == null)
			return true;
		if (v1 == null || v2 == null || v1.Length != v2.Length)
			return false;

		for (int i = 0; i < v1.Length; i++)
			if (v1[i] != v2[i])
				return false;

		return true;
	}

	public override int GetHashCode(IVector v)
	{
		double hCode = v[0];
		for (int i = 1; i < v.Length; i++)
		{
			hCode*= v[1];
		}

		return hCode.GetHashCode();
	}
}


public class VectorsMeanCalculator<T>: ICentroidsCalculator<T> where T: IVector
{
	public IList<T> Calculate(IEnumerable<T> objects, IFactory<T> factory)
	{
		if (objects == null)
			throw new ArgumentOutOfRangeException(nameof(objects), "Unable to average a null reference of vectors!");

		var avgVector = factory.GetNew();
		int vectorsCount = 0;
		foreach (var v in objects)
		{
			for (int i = 0; i < v.Length; i++)
				avgVector[i] += v[i];

			vectorsCount++;
		}

		if (vectorsCount == 0)
			throw new ArgumentOutOfRangeException(nameof(objects), "Unable to average an empty collection of vectors!");

		for (int i = 0; i < avgVector.Length; i++)
		{
			avgVector[i] /= vectorsCount;

			if (double.IsNaN(avgVector[i]) || double.IsInfinity(avgVector[i]))
				throw new ArgumentOutOfRangeException(nameof(objects), "Unable to average vectors: A vector with an invalid component value was found.");
		}

		return new List<T> { avgVector };
	}
}


public class KMeansPlusPlusSampler<T>: ISampler<T>
{
	public KMeansPlusPlusSampler(IDissimilarityFunction<T> dissimilarityFunctionFunction)
	{
		_dissimilarityFunctionFunction = dissimilarityFunctionFunction;
	}

	public IList<T> GetSample(IList<T> objects, int sampleCount)
	{
		if (objects == null)
			throw new ArgumentNullException(nameof(objects), "Unable to get a sample of a null list.");
		if (sampleCount < 2)
			throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "Invalid sample count.");

		
		T[] sample = new T[sampleCount];
		int selectedIdx = random.Next(objects.Count);
		var selectedSample = objects[selectedIdx];
		sample[0] = selectedSample;

		var isSelected = new bool[objects.Count];
		var distArray = new double[objects.Count];
		for (int i = 0; i < objects.Count; i++)
		{ 
			distArray[i] = double.MaxValue;
			isSelected[i] = false;
		}

		isSelected[selectedIdx] = true;

		for (int i = 1; i < sampleCount; i++)
		{
			double distSum = 0;
			var currentCenter = sample[i - 1];
			for (int j = 0; j < objects.Count; j++)
			{
				double currentDistance = _dissimilarityFunctionFunction.Compare(objects[j], currentCenter);
				if (currentDistance < distArray[j])
					distArray[j] = currentDistance;

				distSum += distArray[j] * distArray[j];
			}

			double[] cumulativeProbabilities = new double[objects.Count];
			double accum = 0;
			for (int j = 0; j < objects.Count; j++)
			{
				accum += distArray[j] * distArray[j] / distSum;
				cumulativeProbabilities[j] = accum;
			}

			double probability = random.NextDouble();
			int idx = BinarySearch(probability, cumulativeProbabilities, isSelected);
			isSelected[idx] = true;
			sample[i] = objects[idx];
		}

		return sample;
	}

	private int BinarySearch(double value, double[] cumulativeProbabilities, bool[] isSelected)
	{
		int low = 0;
		int high = cumulativeProbabilities.Length - 1;
		bool found = false;
		int iniIdx = 0;
		while (low < high && !found)
		{
			int mid = (low + high) / 2;
			if (cumulativeProbabilities[mid] > value)
				high = mid - 1;
			else if (cumulativeProbabilities[mid] < value)
				low = mid + 1;
			else
			{
				found = true;
				for (iniIdx = mid + 1; iniIdx < cumulativeProbabilities.Length - 1 && isSelected[iniIdx]; iniIdx++) ;
			}
		}
		if (!found)
			for (iniIdx = low; iniIdx < cumulativeProbabilities.Length - 1 && isSelected[iniIdx]; iniIdx++) ;

		return iniIdx;
	}

	private static Random random = new Random((int)DateTime.Now.Ticks);

	private IDissimilarityFunction<T> _dissimilarityFunctionFunction;
	
}