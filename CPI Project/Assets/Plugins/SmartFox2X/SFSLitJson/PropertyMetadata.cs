#if UNITY_WEBGL
using System;
using System.Reflection;

namespace SFSLitJson
{
	internal struct PropertyMetadata
	{
		public MemberInfo Info;

		public bool IsField;

		public Type Type;
	}
}
#else
using System;
using System.Reflection;

namespace SFSLitJson
{
    internal struct PropertyMetadata
    {
        public MemberInfo Info;

        public bool IsField;

        public Type Type;
    }
}
#endif