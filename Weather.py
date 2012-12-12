# -*- coding: utf-8 -*-
# Weather.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("Apricot")

from System import Char, String, Uri, DateTime, TimeSpan, TimeZoneInfo, Convert, StringComparison
from System.IO import Stream, StreamReader
from System.Collections.Generic import List, Dictionary
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, DateTimeStyles
from System.Text import StringBuilder
from System.Text.RegularExpressions import Regex, RegexOptions, Match
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse
from System.Net.NetworkInformation import NetworkInterface
from System.Xml import XmlDocument, XmlNode, XmlAttribute
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Script, Entry, Word, Sequence

# livedoor Weather
# http://weather.livedoor.com/weather_hacks/webservice.html
# http://weather.livedoor.com/forecast/rss/forecastmap.xml
cityId = 63

def onTick(timer, e):
	global cityId

	uriList = List[Uri]()
	uriList.Add(Uri(String.Format("http://weather.livedoor.com/forecast/webservice/rest/v1?city={0}&day=today", cityId.ToString())))
	uriList.Add(Uri(String.Format("http://weather.livedoor.com/forecast/webservice/rest/v1?city={0}&day=tomorrow", cityId.ToString())))
	uriList.Add(Uri(String.Format("http://weather.livedoor.com/forecast/webservice/rest/v1?city={0}&day=dayaftertomorrow", cityId.ToString())))

	entryList = List[Entry]()

	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				for uri in uriList:
					request = WebRequest.Create(uri)
					response = None
					stream = None

					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(stream)

						for lwwsXmlNode in doc.SelectNodes("/lwws"):
							entry = Entry()
							telop = String.Empty
							title = String.Empty
							city = String.Empty
							forecastDate = String.Empty

							for xmlNode in lwwsXmlNode.ChildNodes:
								if xmlNode.Name.Equals("title"):
									title = xmlNode.InnerText
								if xmlNode.Name.Equals("telop"):
									telop = xmlNode.InnerText
								elif xmlNode.Name.Equals("link"):
									entry.Resource = Uri(xmlNode.InnerText)
								elif xmlNode.Name.Equals("author"):
									entry.Author = xmlNode.InnerText
								elif xmlNode.Name.Equals("forecastdate"):
									if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
										forecastDate = parse(xmlNode.InnerText).ToString("M月d日")
									else:
										forecastDate = parse(xmlNode.InnerText).ToString("MMMM d")
								elif xmlNode.Name.Equals("publictime"):
									entry.Created = entry.Modified = parse(xmlNode.InnerText)
								elif xmlNode.Name.Equals("location"):
									for attr in xmlNode.Attributes:
										if attr.Name.Equals("city"):
											city = attr.Value
								elif xmlNode.Name.Equals("image"):
									for childXmlNode in xmlNode.ChildNodes:
										if childXmlNode.Name.Equals("url"):
											entry.Image = Uri(childXmlNode.InnerText)
				
							entry.Title = String.Concat(city, " - ", telop, " - ", forecastDate)
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
				if entry.Modified > dt:
					dt = entry.Modified

				if entry.Modified > dateTime:
					newEntryList.Add(entry)

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

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(5)
	timer.Start()

def parse(s):
	dt = DateTime()
	invalidTimeZone = False
	index = s.Length;
	match = Regex.Match(s, "\\s[\\+\\-0-9A-Z]+$", RegexOptions.CultureInvariant)

	if match.Success:
		index = match.Index
	else:
		invalidTimeZone = true

	dt = Convert.ToDateTime(s.Substring(0, index), CultureInfo.InvariantCulture)

	if s.Length - index > 0:
		if s[index + 1] == '+':
			if s.Length - (index + 1) == 5:
				dt = dt.AddHours(-Convert.ToInt32(s.Substring(index + 2, 2), CultureInfo.InvariantCulture))
				dt = dt.AddMinutes(-Convert.ToInt32(s.Substring(index + 4, 2), CultureInfo.InvariantCulture))
			else:
				invalidTimeZone = true;
		elif s[index + 1] == '-':
			if s.Length - (index + 1) == 5:
				dt = dt.AddHours(Convert.ToInt32(s.Substring(index + 2, 2), CultureInfo.InvariantCulture))
				dt = dt.AddMinutes(Convert.ToInt32(s.Substring(index + 4, 2), CultureInfo.InvariantCulture))
			else:
				invalidTimeZone = true

	if not invalidTimeZone:
		dt = dt.Add(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now))

	return dt

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

dateTime = DateTime()
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop