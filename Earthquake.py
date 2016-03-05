# -*- coding: utf-8 -*-
# Earthquake.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("Apricot")

from System import Char, String, Uri, DateTime, TimeSpan, StringComparison
from System.IO import Stream, StreamReader
from System.Collections.Generic import List, Dictionary
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, DateTimeStyles
from System.Text import StringBuilder
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse
from System.Net.NetworkInformation import NetworkInterface
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System.Xml import XmlDocument
from Apricot import Script, Entry, Word, Sequence

def update():
	request = WebRequest.Create("http://tenki.jp/component/static_api/rss/earthquake/recent_entries_by_day.xml")
	entryList = List[Entry]()

	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				response = None
				stream = None
				
				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
				
					for itemXmlNode in doc.GetElementsByTagName("item"):
						entry = Entry()
						epicenter = None
						maxLevel = None
			
						for xmlNode in itemXmlNode.ChildNodes:
							if xmlNode.Name.Equals("link"):
								entry.Resource = Uri(xmlNode.InnerText)
							elif xmlNode.Name.Equals("description"):
								entry.Description = xmlNode.InnerText
							elif xmlNode.Name.Equals("tenkiJP:earthquake"):
								for attribute in xmlNode.Attributes:
									if attribute.Name.Equals("epicenter"):
										epicenter = attribute.Value
									elif attribute.Name.Equals("max_level"):
										maxLevel = attribute.Value
									elif attribute.Name.Equals("outbreak_datetime"):
										entry.Created = entry.Modified = DateTime.Parse(attribute.Value)
						
						if epicenter is not None:
							if String.IsNullOrEmpty(maxLevel):
								maxLevel = "N/A"

							if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
								entry.Title = String.Format("震度{0} - {1}", maxLevel, epicenter)
							else:
								entry.Title = String.Format("Intensity {0} - {1}", maxLevel, epicenter)

							entryList.Add(entry)

				finally:			
					if stream is not None:
						stream.Close()
			
					if response is not None:
						response.Close()

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)
		
	def onCompleted(task):
		global dateTime

		if entryList.Count > 0:
			dt = DateTime(0)
			newEntryList = List[Entry]()

			for entry in entryList:
				if entry.Modified > dateTime:
					newEntryList.Add(entry)

				if entry.Modified > dt:
					dt = entry.Modified

			if dt > dateTime:
				dateTime = dt
			else:
				dateTime = DateTime.Now

			if newEntryList.Count > 0:
				Script.Instance.Alert(newEntryList)

				dictionary = Dictionary[Char, List[String]]()

				for word in Script.Instance.Words:
					if word.Name.Length > 0:
						if not dictionary.ContainsKey(word.Name[0]):
							dictionary.Add(word.Name[0], List[String]())

						dictionary[word.Name[0]].Add(word.Name)

				for entry in newEntryList:
					termList = getTermList(dictionary, entry.Title)
				
					if termList.Count > 0:
						sequenceList = List[Sequence]()

						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Activate"):
								sequenceList.Add(sequence)
						
						if Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, termList)):
							break

	Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

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

def onTick(timer, e):
	update()

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(1.5)
	timer.Start()

def onStart(s, e):
	global timer

	timer.Start()

def onStop(s, e):
	global timer

	timer.Stop()

dateTime = DateTime.Now - TimeSpan(0, 30, 0)
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop