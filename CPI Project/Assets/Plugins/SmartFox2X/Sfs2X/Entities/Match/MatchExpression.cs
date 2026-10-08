#if UNITY_WEBGL
using System;
using System.Text;
using Sfs2X.Entities.Data;

namespace Sfs2X.Entities.Match
{
	public class MatchExpression
	{
		private string varName;

		private IMatcher condition;

		private object varValue;

		internal LogicOperator logicOp;

		internal MatchExpression parent;

		internal MatchExpression next;

		public string VarName => varName;

		public IMatcher Condition => condition;

		public object VarValue => varValue;

		public LogicOperator LogicOp => logicOp;

		internal static MatchExpression ChainedMatchExpression(string varName, IMatcher condition, object value, LogicOperator logicOp, MatchExpression parent)
		{
			MatchExpression matchExpression = new MatchExpression(varName, condition, value);
			matchExpression.logicOp = logicOp;
			matchExpression.parent = parent;
			return matchExpression;
		}

		public MatchExpression(string varName, IMatcher condition, object varValue)
		{
			this.varName = varName;
			this.condition = condition;
			this.varValue = varValue;
		}

		public MatchExpression And(string varName, IMatcher condition, object varValue)
		{
			next = ChainedMatchExpression(varName, condition, varValue, LogicOperator.AND, this);
			return next;
		}

		public MatchExpression Or(string varName, IMatcher condition, object varValue)
		{
			next = ChainedMatchExpression(varName, condition, varValue, LogicOperator.OR, this);
			return next;
		}

		public bool HasNext()
		{
			return next != null;
		}

		public MatchExpression Next()
		{
			return next;
		}

		public MatchExpression Rewind()
		{
			MatchExpression matchExpression = this;
			while (matchExpression.parent != null)
			{
				matchExpression = matchExpression.parent;
			}
			return matchExpression;
		}

		public string AsString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (logicOp != null)
			{
				stringBuilder.Append(" " + logicOp.Id + " ");
			}
			stringBuilder.Append("(");
			stringBuilder.Append(varName + " " + condition.Symbol + " " + ((varValue is string) ? ("'" + varValue?.ToString() + "'") : varValue));
			stringBuilder.Append(")");
			return stringBuilder.ToString();
		}

		public override string ToString()
		{
			MatchExpression matchExpression = Rewind();
			StringBuilder stringBuilder = new StringBuilder(matchExpression.AsString());
			while (matchExpression.HasNext())
			{
				matchExpression = matchExpression.next;
				stringBuilder.Append(matchExpression.AsString());
			}
			return stringBuilder.ToString();
		}

		public ISFSArray ToSFSArray()
		{
			MatchExpression matchExpression = Rewind();
			ISFSArray iSFSArray = new SFSArray();
			iSFSArray.AddSFSArray(matchExpression.ExpressionAsSFSArray());
			while (matchExpression.HasNext())
			{
				matchExpression = matchExpression.Next();
				iSFSArray.AddSFSArray(matchExpression.ExpressionAsSFSArray());
			}
			return iSFSArray;
		}

		private ISFSArray ExpressionAsSFSArray()
		{
			ISFSArray iSFSArray = new SFSArray();
			if (logicOp != null)
			{
				iSFSArray.AddUtfString(logicOp.Id);
			}
			else
			{
				iSFSArray.AddNull();
			}
			iSFSArray.AddUtfString(varName);
			iSFSArray.AddByte((byte)condition.Type);
			iSFSArray.AddUtfString(condition.Symbol);
			if (condition.Type == 0)
			{
				iSFSArray.AddBool(Convert.ToBoolean(varValue));
			}
			else if (condition.Type == 1)
			{
				iSFSArray.AddDouble(Convert.ToDouble(varValue));
			}
			else
			{
				iSFSArray.AddUtfString(Convert.ToString(varValue));
			}
			return iSFSArray;
		}
	}
}
#else
using System;
using System.Text;
using Sfs2X.Entities.Data;

namespace Sfs2X.Entities.Match
{
    public class MatchExpression
    {
        private string varName;

        private IMatcher condition;

        private object varValue;

        internal LogicOperator logicOp;

        internal MatchExpression parent;

        internal MatchExpression next;

        public string VarName => varName;

        public IMatcher Condition => condition;

        public object VarValue => varValue;

        public LogicOperator LogicOp => logicOp;

        internal static MatchExpression ChainedMatchExpression(string varName, IMatcher condition, object value, LogicOperator logicOp, MatchExpression parent)
        {
            MatchExpression matchExpression = new MatchExpression(varName, condition, value);
            matchExpression.logicOp = logicOp;
            matchExpression.parent = parent;
            return matchExpression;
        }

        public MatchExpression(string varName, IMatcher condition, object varValue)
        {
            this.varName = varName;
            this.condition = condition;
            this.varValue = varValue;
        }

        public MatchExpression And(string varName, IMatcher condition, object varValue)
        {
            next = ChainedMatchExpression(varName, condition, varValue, LogicOperator.AND, this);
            return next;
        }

        public MatchExpression Or(string varName, IMatcher condition, object varValue)
        {
            next = ChainedMatchExpression(varName, condition, varValue, LogicOperator.OR, this);
            return next;
        }

        public bool HasNext()
        {
            return next != null;
        }

        public MatchExpression Next()
        {
            return next;
        }

        public MatchExpression Rewind()
        {
            MatchExpression matchExpression = this;
            while (matchExpression.parent != null)
            {
                matchExpression = matchExpression.parent;
            }
            return matchExpression;
        }

        public string AsString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (logicOp != null)
            {
                stringBuilder.Append(" " + logicOp.Id + " ");
            }
            stringBuilder.Append("(");
            stringBuilder.Append(varName + " " + condition.Symbol + " " + ((varValue is string) ? ("'" + varValue?.ToString() + "'") : varValue));
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            MatchExpression matchExpression = Rewind();
            StringBuilder stringBuilder = new StringBuilder(matchExpression.AsString());
            while (matchExpression.HasNext())
            {
                matchExpression = matchExpression.next;
                stringBuilder.Append(matchExpression.AsString());
            }
            return stringBuilder.ToString();
        }

        public ISFSArray ToSFSArray()
        {
            MatchExpression matchExpression = Rewind();
            ISFSArray iSFSArray = new SFSArray();
            iSFSArray.AddSFSArray(matchExpression.ExpressionAsSFSArray());
            while (matchExpression.HasNext())
            {
                matchExpression = matchExpression.Next();
                iSFSArray.AddSFSArray(matchExpression.ExpressionAsSFSArray());
            }
            return iSFSArray;
        }

        private ISFSArray ExpressionAsSFSArray()
        {
            ISFSArray iSFSArray = new SFSArray();
            if (logicOp != null)
            {
                iSFSArray.AddUtfString(logicOp.Id);
            }
            else
            {
                iSFSArray.AddNull();
            }
            iSFSArray.AddUtfString(varName);
            iSFSArray.AddByte((byte)condition.Type);
            iSFSArray.AddUtfString(condition.Symbol);
            if (condition.Type == 0)
            {
                iSFSArray.AddBool(Convert.ToBoolean(varValue));
            }
            else if (condition.Type == 1)
            {
                iSFSArray.AddDouble(Convert.ToDouble(varValue));
            }
            else
            {
                iSFSArray.AddUtfString(Convert.ToString(varValue));
            }
            return iSFSArray;
        }
    }
}
#endif