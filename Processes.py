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
		s1 = stringBuilder.ToString()
		selectedTerm1 = None

		if dictionary.ContainsKey(s1[0]):
			for term in dictionary[s1[0]]:
				if s1.StartsWith(term, StringComparison.Ordinal) and term.Length > (0 if selectedTerm1 is None else selectedTerm1.Length):
					selectedTerm1 = term
		
		if String.IsNullOrEmpty(selectedTerm1):
			stringBuilder.Remove(0, 1)
		else:
			sb = StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1))
			selectedTerm2 = None
			i = 0
			max = 0

			while sb.Length > 0 and i < selectedTerm1.Length:
				s2 = sb.ToString()

				if dictionary.ContainsKey(s2[0]):
					for term in dictionary[s2[0]]:
						if s2.StartsWith(term, StringComparison.Ordinal) and term.Length > (0 if selectedTerm2 is None else selectedTerm2.Length):
							selectedTerm2 = term
							max = i + selectedTerm2.Length

				sb.Remove(0, 1)
				i += 1

			if not String.IsNullOrEmpty(selectedTerm2) and selectedTerm1.Length < selectedTerm2.Length:
				if not selectedTermList.Contains(selectedTerm2):
					selectedTermList.Add(selectedTerm2)

				stringBuilder.Remove(0, max)

			else:
				if not selectedTermList.Contains(selectedTerm1):
					selectedTermList.Add(selectedTerm1)

				stringBuilder.Remove(0, selectedTerm1.Length)

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