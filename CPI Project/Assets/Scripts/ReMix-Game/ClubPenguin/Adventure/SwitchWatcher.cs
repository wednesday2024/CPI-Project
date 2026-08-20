using ClubPenguin.Core;
using ClubPenguin.Switches;
using ClubPenguin.Task;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClubPenguin.Adventure
{
	[Serializable]
	[CreateAssetMenu(menuName = "Watcher/Switch")]
	public class SwitchWatcher : TaskWatcher
	{
		public string SwitchName;

		private Transform owner;

		private List<Switch> parts;

		private HashSet<Switch> counted;

		public override object GetExportParameters()
		{
			return ExportedSwitch.Create(GameObject.Find(SwitchName).GetComponent<Switch>());
		}

		public override string GetWatcherType()
		{
			return "switch";
		}

		public override void OnActivate()
		{
			base.OnActivate();
			GameObject gameObject = GameObject.Find(SwitchName);
			if (gameObject == null)
			{
				owner = null;
				return;
			}
			owner = gameObject.transform;
			parts = getCountedParts(gameObject);
			if (parts != null)
			{
				counted = new HashSet<Switch>();
				for (int i = 0; i < parts.Count; i++)
				{
					parts[i].StateChanged += onPartChanged;
				}
				recount();
			}
			else
			{
				base.dispatcher.AddListener<SwitchEvents.SwitchChange>(onSwitchChange);
			}
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();
			if (parts != null)
			{
				for (int i = 0; i < parts.Count; i++)
				{
					if (parts[i] != null)
					{
						parts[i].StateChanged -= onPartChanged;
					}
				}
				parts = null;
				counted = null;
			}
			else if (owner != null)
			{
				base.dispatcher.RemoveListener<SwitchEvents.SwitchChange>(onSwitchChange);
			}
		}

		private bool onSwitchChange(SwitchEvents.SwitchChange evt)
		{
			if (evt.Owner == owner && evt.Value)
			{
				taskIncrement();
			}
			return false;
		}

		private void onPartChanged(Switch part, bool value)
		{
			if (value)
			{
				recount();
			}
		}

		// The volumes to count one by one, or null to count the group as a whole
		// like before. A group that can't go back off once it fired, an OR with a
		// latch in it or an AND latched all the way down, only ever raises one
		// SwitchChange, which is why "read all 7 signs" stopped at 1. Groups that
		// do go back off are lap counters, they stay on the old path
		private List<Switch> getCountedParts(GameObject root)
		{
			ClubPenguin.Task.Task self = base.task as ClubPenguin.Task.Task;
			if (self == null || self.Goal <= 1 || !string.IsNullOrEmpty(CriteriaSwitchName))
			{
				return null;
			}
			Switch component = root.GetComponent<Switch>();
			if (component == null || !sticksOn(component))
			{
				return null;
			}
			List<Switch> list = new List<Switch>();
			collectParts(root.transform, list);
			return (list.Count > 0) ? list : null;
		}

		// Once this one is on, can it ever go off again
		private bool sticksOn(Switch node)
		{
			if (node.Latch)
			{
				return true;
			}
			CompoundSwitch compoundSwitch = node as CompoundSwitch;
			if (compoundSwitch == null)
			{
				return false;
			}
			List<Switch> list = new List<Switch>();
			for (int i = 0; i < node.transform.childCount; i++)
			{
				Switch component = node.transform.GetChild(i).GetComponent<Switch>();
				if (component != null)
				{
					list.Add(component);
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			if (compoundSwitch.Operator == CompoundSwitch.Operators.AND)
			{
				for (int j = 0; j < list.Count; j++)
				{
					if (!sticksOn(list[j]))
					{
						return false;
					}
				}
				return true;
			}
			for (int k = 0; k < list.Count; k++)
			{
				if (sticksOn(list[k]))
				{
					return true;
				}
			}
			return false;
		}

		// The volumes themselves, however deep they sit - the seven sub lights
		// hang off a second CompoundSwitch
		private void collectParts(Transform parent, List<Switch> list)
		{
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				Switch component = child.GetComponent<Switch>();
				if (component != null)
				{
					int count = list.Count;
					collectParts(child, list);
					if (list.Count == count)
					{
						list.Add(component);
					}
				}
			}
		}

		// Count the volumes the player has been in rather than the times one of
		// them fired: a volume they already stand in when the watcher wakes
		// up would never count otherwise, and the sea caves drop you inside the
		// first sign. Only ever goes up, the day's saved progress is in there
		private void recount()
		{
			ClubPenguin.Task.Task self = base.task as ClubPenguin.Task.Task;
			if (self == null)
			{
				return;
			}
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i] != null && parts[i].OnOff)
				{
					counted.Add(parts[i]);
				}
			}
			int num = counted.Count;
			if (self.Definition.CounterMax > 0 && num > self.Definition.CounterMax)
			{
				num = self.Definition.CounterMax;
			}
			if (num > self.Counter)
			{
				self.SetCounter(num);
			}
		}
	}
}
