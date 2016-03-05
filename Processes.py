# -*- coding: utf-8 -*-
# Processes.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("Apricot")

from System import Int32, Char, String, DateTime, TimeSpan, StringComparison
from System.Collections.Generic import List, Dictionary, KeyValuePair
from System.Diagnostics import Process
from System.Text import StringBuilder
from System.Threading.Tasks import Task, TaskScheduler
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Script, Word, Sequence

def onTick(s, e):
	processList = List[KeyValuePair[Int32, String]]()

	def onUpdate():
		try:
			for process in Process.GetProcesses():
				processList.Add(KeyValuePair[Int32, String](process.Id, process.MainWindowTitle))

		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

	def onCompleted(task):
		global dateTime, processDictionary
	
		nowDateTime = DateTime.Now
		isNew = True
		dictionary = None
		termList = List[String]()

		if processDictionary.Count > 0:
			isNew = False

		for kvp in processList:
			isUpdated = False
			mainWindowTitle = None

			if processDictionary.ContainsKey(kvp.Key):
				mainWindowTitle = processDictionary[kvp.Key]
			
				if not String.IsNullOrEmpty(kvp.Value):
					if not kvp.Value.Equals(mainWindowTitle):
						isUpdated = True

				processDictionary[kvp.Key] = kvp.Value
			else:
				if not String.IsNullOrEmpty(kvp.Value):
					processDictionary.Add(kvp.Key, kvp.Value)

					if not isNew:
						isUpdated = True
		
			if isUpdated:
				previousTermList = List[String]()

				if dictionary is None:
					dictionary = Dictionary[Char, List[String]]()

					for word in Script.Instance.Words:
						if word.Name.Length > 0:
							if not dictionary.ContainsKey(word.Name[0]):
								dictionary.Add(word.Name[0], List[String]())

							dictionary[word.Name[0]].Add(word.Name)

				if not String.IsNullOrEmpty(mainWindowTitle):
					previousTermList.AddRange(getTermList(dictionary, mainWindowTitle))
			
				for term in getTermList(dictionary, kvp.Value):
					if not previousTermList.Contains(term) and not termList.Contains(term):
						termList.Add(term)
	
		if termList.Count > 0:
			sequenceList = List[Sequence]()

			for sequence in Script.Instance.Sequences:
				if sequence.Name.Equals("Activate"):
					sequenceList.Add(sequence)

			Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, termList))

		keyList = List[Int32](processDictionary.Keys)

		for key in keyList:
			removable = True

			for kvp in processList:
				if key == kvp.Key:
					removable = False

			if removable:
				processDictionary.Remove(key)

		dateTime = nowDateTime

	Task.Factory.StartNew(onUpdate).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

def getTermList(dictionary, text):
	stringBuilder = StringBuilder(text)
	selectedTermList = List[String]()

	while stringBuilder.Length > 0:
		s = stringBuilder.ToString()
		selectedTerm = None

		if dictionary.ContainsKey(s[0]):
			for term in dictionary[s[0]]:
				if s.StartsWith(term, StringComparison.Ordinal) and term.Length > (0 if selectedTerm is None else selectedTerm.Length):
					selectedTerm = term
		
		if String.IsNullOrEmpty(selectedTerm):
			stringBuilder.Remove(0, 1)
		else:
			if not selectedTermList.Contains(selectedTerm):
				selectedTermList.Add(selectedTerm)

			stringBuilder.Remove(0, selectedTerm.Length)

	return selectedTermList

def onStart(s, e):
	global timer

	timer.Start()

def onStop(s, e):
	global timer
	
	timer.Stop()

dateTime = DateTime.Now
processDictionary = Dictionary[Int32, String]()
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromSeconds(15)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop