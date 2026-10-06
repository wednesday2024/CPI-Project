using System;

namespace Sfs2X.Entities.Data
{
	public class Vec3D
	{
		private float fx;

		private float fy;

		private float fz;

		private int ix;

		private int iy;

		private int iz;

		private bool useFloat;

		public float FloatX => fx;

		public float FloatY => fy;

		public float FloatZ => fz;

		public int IntX => ix;

		public int IntY => iy;

		public int IntZ => iz;

		public static Vec3D fromArray(object array)
		{
			if (array is SFSArrayLite)
			{
				SFSArrayLite sFSArrayLite = array as SFSArrayLite;
				object elementAt = sFSArrayLite.GetElementAt(0);
				object elementAt2 = sFSArrayLite.GetElementAt(1);
				object value = ((sFSArrayLite.Size() > 2) ? sFSArrayLite.GetElementAt(2) : ((object)0));
				array = ((!(elementAt is double)) ? ((object)new int[3]
				{
					Convert.ToInt32(elementAt),
					Convert.ToInt32(elementAt2),
					Convert.ToInt32(value)
				}) : ((object)new float[3]
				{
					Convert.ToSingle(elementAt),
					Convert.ToSingle(elementAt2),
					Convert.ToSingle(value)
				}));
			}
			if (array is int[])
			{
				return fromIntArray((int[])array);
			}
			if (array is float[])
			{
				return fromFloatArray((float[])array);
			}
			throw new ArgumentException("Invalid Array Type, cannot convert to Vec3D!");
		}

		private static Vec3D fromIntArray(int[] array)
		{
			if (array.Length != 3)
			{
				throw new ArgumentException("Wrong array size. Vec3D requires an array with 3 parameters (x,y,z)");
			}
			return new Vec3D(array[0], array[1], array[2]);
		}

		private static Vec3D fromFloatArray(float[] array)
		{
			if (array.Length != 3)
			{
				throw new ArgumentException("Wrong array size. Vec3D requires an array with 3 parameters (x,y,z)");
			}
			return new Vec3D(array[0], array[1], array[2]);
		}

		private Vec3D()
		{
		}

		public Vec3D(int px, int py, int pz)
		{
			ix = px;
			iy = py;
			iz = pz;
			useFloat = false;
		}

		public Vec3D(int px, int py)
			: this(px, py, 0)
		{
		}

		public Vec3D(float px, float py, float pz)
		{
			fx = px;
			fy = py;
			fz = pz;
			useFloat = true;
		}

		public Vec3D(float px, float py)
			: this(px, py, 0f)
		{
		}

		public bool IsFloat()
		{
			return useFloat;
		}

		public int[] ToIntArray()
		{
			return new int[3] { ix, iy, iz };
		}

		public float[] ToFloatArray()
		{
			return new float[3] { fx, fy, fz };
		}

		public override string ToString()
		{
			if (IsFloat())
			{
				return $"({fx},{fy},{fz})";
			}
			return $"({ix},{iy},{iz})";
		}
	}
}
