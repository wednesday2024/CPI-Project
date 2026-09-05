using ClubPenguin.Core;
using ClubPenguin.Locomotion;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WatcherSwitch : Switch
{
	public List<TaskWatcher> enableWatchers = new List<TaskWatcher>();

	public List<TaskWatcher> disableWatchers = new List<TaskWatcher>();

	private EventDispatcher dispatcher;

	private void Start()
	{
		dispatcher = Service.Get<EventDispatcher>();
		dispatcher.AddListener<ClubPenguin.ActionSequencerEvents.ActionSequenceStarted>(onActionSequenceStarted);
	}

	private void OnDestroy()
	{
		if (dispatcher != null)
		{
			dispatcher.RemoveListener<ClubPenguin.ActionSequencerEvents.ActionSequenceStarted>(onActionSequenceStarted);
		}
	}

	private bool onActionSequenceStarted(ClubPenguin.ActionSequencerEvents.ActionSequenceStarted evt)
	{
		if (matchesWatcher(enableWatchers, evt.actionGameObject))
		{
			Change(true);
		}
		if (matchesWatcher(disableWatchers, evt.actionGameObject))
		{
			Change(false);
		}
		return false;
	}

	private bool matchesWatcher(List<TaskWatcher> watcherDefinitions, GameObject actionGameObject)
	{
		if (actionGameObject == null)
		{
			return false;
		}
		string actionPath = actionGameObject.GetPath();
		for (int i = 0; i < watcherDefinitions.Count; i++)
		{
			TaskWatcher watcherDefinition = watcherDefinitions[i];
			if (watcherDefinition != null && watcherDefinition.GetWatcherType() == "interaction" && string.Equals(watcherDefinition.GetExportParameters() as string, actionPath, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	public override object GetSwitchParameters()
	{
		Dictionary<string, List<ExportedTaskWatcher>> dictionary = new Dictionary<string, List<ExportedTaskWatcher>>();
		List<ExportedTaskWatcher> list = new List<ExportedTaskWatcher>();
		foreach (TaskWatcher enableWatcher in enableWatchers)
		{
			ExportedTaskWatcher exportedTaskWatcher = exportTaskWatcher(enableWatcher);
			if (exportedTaskWatcher != null)
			{
				list.Add(exportedTaskWatcher);
			}
		}
		dictionary.Add("enable", list);
		List<ExportedTaskWatcher> list2 = new List<ExportedTaskWatcher>();
		foreach (TaskWatcher disableWatcher in disableWatchers)
		{
			ExportedTaskWatcher exportedTaskWatcher = exportTaskWatcher(disableWatcher);
			if (exportedTaskWatcher != null)
			{
				list2.Add(exportedTaskWatcher);
			}
		}
		dictionary.Add("disable", list2);
		return dictionary;
	}

	private ExportedTaskWatcher exportTaskWatcher(TaskWatcher watcherDef)
	{
		ExportedTaskWatcher exportedTaskWatcher = new ExportedTaskWatcher();
		if (!string.IsNullOrEmpty(watcherDef.CriteriaSwitchName))
		{
			GameObject gameObject = GameObject.Find(watcherDef.CriteriaSwitchName);
			if (gameObject == null)
			{
				Log.LogError(this, "Unable to find switch criteria object " + watcherDef.CriteriaSwitchName + " for WatcherSwitch " + base.name + ". Will not be exported");
				return null;
			}
			Switch component = gameObject.GetComponent<Switch>();
			exportedTaskWatcher.criteriaSwitch = ExportedSwitch.Create(component);
		}
		exportedTaskWatcher.type = watcherDef.GetWatcherType();
		exportedTaskWatcher.parameters = watcherDef.GetExportParameters();
		return exportedTaskWatcher;
	}

	public override string GetSwitchType()
	{
		return "watcher";
	}
}
