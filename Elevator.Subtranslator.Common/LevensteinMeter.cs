using System;

namespace Elevator.Subtranslator.Common
{
	public class LevenshteinMeter
	{
		private static int MaxTextLength = 100;

		private static int[,] D = new int[MaxTextLength, MaxTextLength];

		public LevenshteinMeter(int insertionWeight, int deletionWeight, int replaceWeight)
		{
			InsertionWeight = insertionWeight;
			DeletionWeight = deletionWeight;
			ReplaceWeight = replaceWeight;
		}

		public int InsertionWeight { get; set; }
		public int DeletionWeight { get; set; }
		public int ReplaceWeight { get; set; }


		/*
		 D(0,0) = 0
		 для всех j от 1 до N
		   D(0,j) = D(0,j-1) + цена вставки символа S2[j]
		 для всех i от 1 до M
		   D(i,0) = D(i-1,0) + цена удаления символа S1[i]
		   для всех j от 1 до N
			 D(i,j) = min(
				D(i-1, j) + цена удаления символа S1[i],
				D(i, j-1) + цена вставки символа S2[j],
				D(i-1, j-1) + цена замены символа S1[i] на символ S2[j]
			 )
		 вернуть D(M,N)
		*/

		public float GetNormedDistance(string a, string b)
		{
			int length = Math.Min(a.Length, b.Length);
			return GetDistance(a, b) / (float)length;
		}

		public int GetDistance(string a, string b)
		{
			int m = a.Length;
			int n = b.Length;

			int maxLengthAB = Math.Max(m + 1, n + 1);
			if (maxLengthAB > MaxTextLength)
			{
				MaxTextLength = maxLengthAB;
				D = new int[MaxTextLength, MaxTextLength];
			}

			D[0, 0] = 0;

			for (int j = 1; j <= n; ++j)
			{
				D[0, j] = D[0, j - 1] + InsertionWeight;
			}

			for (int i = 1; i <= m; ++i)
			{
				D[i, 0] = D[i - 1, 0] + DeletionWeight;

				for (int j = 1; j <= n; ++j)
				{
					D[i, j] = Min(
						D[i - 1, j] + DeletionWeight,
						D[i, j - 1] + InsertionWeight,
						D[i - 1, j - 1] + ReplaceWeight * (a[i - 1] == b[j - 1] ? 0 : 1)
						);
				}
			}

			return D[m, n];
		}

		public float GetNormedDistanceQ(string a, string b, float maxDist)
		{
			int maxDistN = (int)(maxDist * Math.Min(a.Length, b.Length));
			return GetDistanceQ(a, b, maxDistN);
		}

		public int GetDistanceQ(string a, string b, int maxDist)
		{
			int m = a.Length;
			int n = b.Length;

			int maxLengthAB = Math.Max(m + 1, n + 1);
			if (maxLengthAB > MaxTextLength)
			{
				MaxTextLength = maxLengthAB;
				D = new int[MaxTextLength, MaxTextLength];
			}

			D[0, 0] = 0;

			for (int j = 1; j <= n; ++j)
			{
				D[0, j] = D[0, j - 1] + InsertionWeight;
			}

			for (int i = 1; i <= m; ++i)
			{
				D[i, 0] = D[i - 1, 0] + DeletionWeight;

				int j0 = Math.Max(1, i - maxDist);
				int jn = Math.Min(n, i + maxDist);

				for (int j = j0; j <= jn; ++j)
				{
					D[i, j] = Min(
						D[i - 1, j] + DeletionWeight,
						D[i, j - 1] + InsertionWeight,
						D[i - 1, j - 1] + ReplaceWeight * (a[i - 1] == b[j - 1] ? 0 : 1)
						);
				}
				if (D[i, i] > maxDist)
					return maxDist;
			}

			return D[m, n];
		}

		private static int Min(int a, int b, int c)
		{
			int minAB = Math.Min(a, b);
			return Math.Min(minAB, c);
		}
	}
}
