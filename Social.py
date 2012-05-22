# -*- coding: utf-8 -*-
# Social.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Boolean, Byte, Char, Int32, String, StringComparison, Uri, DateTime, TimeSpan, Convert, Type, Environment
from System.IO import File, Directory, FileInfo, DirectoryInfo, Path, StreamReader, MemoryStream, SeekOrigin
from System.Collections.Generic import List, Queue, HashSet
from System.Globalization import CultureInfo
from System.Diagnostics import Process, Trace
from System.Reflection import Assembly
from System.Security.Cryptography import SHA1CryptoServiceProvider
from System.Text import StringBuilder, Encoding
from System.Text.RegularExpressions import Regex, RegexOptions, Match
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import NetworkCredential, WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, HttpRequestHeader, WebRequestMethods
from System.Net.NetworkInformation import NetworkInterface, PhysicalAddress
from System.Windows import Application, Window, HorizontalAlignment, VerticalAlignment, Thickness, SystemColors
from System.Windows.Controls import MenuItem, Separator, Label, StackPanel, Orientation, TextBlock
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System.Xml import XmlDocument, XmlNode, XmlAttribute, XmlNodeType
from System.Xml.Serialization import XmlSerializer
from Apricot import Agent, Balloon, Script, Entry, Message, Word, Sequence

def onTick(timer, e):
	entryList = List[Entry]()
	wordList = List[Word]()

	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				request = WebRequest.Create("http://social.apricotan.net/entries?limit=25")
				response = None
				stream = None
				
				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
		
					for entryXmlNode in doc.SelectNodes("/ArrayOfEntry/Entry"):
						entry = Entry()
			
						for xmlNode in entryXmlNode.ChildNodes:
							if xmlNode.Name.Equals("Resource"):
								entry.Resource = Uri(xmlNode.InnerText)
							elif xmlNode.Name.Equals("Title"):
								entry.Title = xmlNode.InnerText
							elif xmlNode.Name.Equals("Created"):
								entry.Created = DateTime.Parse(xmlNode.InnerText)
							elif xmlNode.Name.Equals("Modified"):
								entry.Modified = DateTime.Parse(xmlNode.InnerText)
							elif xmlNode.Name.Equals("Image"):
								entry.Image = Uri(xmlNode.InnerText)
							elif xmlNode.Name.Equals("Tags"):
								for childXmlNode in xmlNode.ChildNodes:
									if childXmlNode.Name.Equals("string"):
										entry.Tags.Add(childXmlNode.InnerText)
			
						entryList.Add(entry)
		
				finally:
					if stream != None:
						stream.Close()
			
					if response != None:
						response.Close()

				request = WebRequest.Create("http://social.apricotan.net/words?limit=50")
				response = None
				stream = None
				
				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
		
					for entryXmlNode in doc.SelectNodes("/ArrayOfWord/Word"):
						word = Word()
			
						for xmlNode in entryXmlNode.ChildNodes:
							if xmlNode.Name.Equals("Name"):
								word.Name = xmlNode.InnerText
							elif xmlNode.Name.Equals("Attributes"):
								for childXmlNode in xmlNode.ChildNodes:
									if childXmlNode.Name.Equals("string"):
										word.Attributes.Add(childXmlNode.InnerText)
			
						wordList.Add(word)
		
				finally:
					if stream != None:
						stream.Close()
			
					if response != None:
						response.Close()

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

	def onCompleted(task):
		global dateTime, recentEntryList, recentWordList

		if entryList.Count > 0:
			newEntryList = List[Entry]()
			nowDateTime = DateTime.Now
			dt = DateTime(0)

			for entry in entryList:
				if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False and entry.Modified > dateTime and entry.Modified <= nowDateTime:
					if not newEntryList.Exists(lambda x: x.Resource.Equals(entry.Resource)):
						newEntryList.Add(entry)
				
				if entry.Modified > dt:
					dt = entry.Modified

			if dt > dateTime:
				dateTime = dt
			else:
				dateTime = nowDateTime

			if newEntryList.Count > 0:
				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					Script.Instance.Suggest("ソーシャル", newEntryList)
				else:
					Script.Instance.Suggest("Social", newEntryList)
				
				for entry in entryList:
					if entry.HasTags:
						sequenceList = List[Sequence]()

						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Activate"):
								sequenceList.Add(sequence)
						
						if Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, entry.Tags)):
							break

			recentEntryList.Clear()

			hashSet = HashSet[Uri]()

			for entry in entryList:
				if hashSet.Contains(entry.Resource) == False and hashSet.Count < 10:
					hashSet.Add(entry.Resource)

				if hashSet.Contains(entry.Resource):
					recentEntryList.Add(entry)

		if wordList.Count > 0:
			def match(word):
				for w in Script.Instance.Words:
					if w.Name.Equals(word.Name):
						return False

				return True

			recentWordList.Clear()
			recentWordList.AddRange(wordList.FindAll(match))

			if recentWordList.Count > 10:
				recentWordList.RemoveRange(10, recentWordList.Count - 10)
			
	Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(10)
	timer.Start()

def onPing(s, e):
	entry = s.Tag
		
	if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False:
		stringBuilder = StringBuilder()
		newWordList = List[Word]()
		bytes = None
		entryList = List[Entry]()
		wordList = List[Word]()

		stringBuilder.AppendFormat("http://social.apricotan.net/ping?resource={0}&title={1}&author={2}&created={3}&modified={4}", urlEncode(entry.Resource.ToString()), urlEncode(entry.Title), urlEncode(getUserID()), urlEncode(entry.Created.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")))
			
		if entry.Image != None:
			stringBuilder.AppendFormat("&image={0}", urlEncode(entry.Image.ToString()))

		if entry.HasTags:
			sb = StringBuilder()

			for tag in entry.Tags:
				if sb.Length > 0:
					if Regex.IsMatch(tag, "\\s", RegexOptions.CultureInvariant):
						sb.AppendFormat(" \"{0}\"", tag.Replace("\"", "\\\""))
					else:
						sb.AppendFormat(" {0}", tag.Replace("\"", "\\\""))
				else:
					if Regex.IsMatch(tag, "\\s", RegexOptions.CultureInvariant):
						sb.AppendFormat("\"{0}\"", tag.Replace("\"", "\\\""))
					else:
						sb.Append(tag.Replace("\"", "\\\""))

			stringBuilder.AppendFormat("&tags={0}", urlEncode(sb.ToString()))

			for word in Script.Instance.Words:
				for tag in entry.Tags:
					if word.Name.Equals(tag) and word.HasAttributes:
						newWord = Word()
						newWord.Name = word.Name

						attributeList = List[String](word.Attributes)
						attributeList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.InvariantCulture))

						for attribute in attributeList:
							newWord.Attributes.Add(attribute)

						newWordList.Add(newWord)

						break

			ms = None
			sr = None

			try:
				ms = MemoryStream()
				words = newWordList.ToArray()
				serializer = XmlSerializer(words.GetType())
				serializer.Serialize(ms, words)
				ms.Seek(0, SeekOrigin.Begin)
				sr = StreamReader(ms)
				bytes = Encoding.UTF8.GetBytes(sr.ReadToEnd())

			finally:
				if sr != None:
					sr.Close()

				if ms != None:
					ms.Close()

		def onUpdate():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					client = WebClient()
					client.Headers.Add(HttpRequestHeader.ContentType, "text/xml")
					
					if bytes == None:
						client.UploadData(Uri(stringBuilder.ToString()), WebRequestMethods.Http.Post, Encoding.UTF8.GetBytes(String.Empty))
					else:
						client.UploadData(Uri(stringBuilder.ToString()), WebRequestMethods.Http.Post, bytes)

					request = WebRequest.Create("http://social.apricotan.net/entries?limit=25")
					response = None
					stream = None
					
					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(stream)
		
						for entryXmlNode in doc.SelectNodes("/ArrayOfEntry/Entry"):
							entry = Entry()
			
							for xmlNode in entryXmlNode.ChildNodes:
								if xmlNode.Name.Equals("Resource"):
									entry.Resource = Uri(xmlNode.InnerText)
								elif xmlNode.Name.Equals("Title"):
									entry.Title = xmlNode.InnerText
								elif xmlNode.Name.Equals("Created"):
									entry.Created = DateTime.Parse(xmlNode.InnerText)
								elif xmlNode.Name.Equals("Modified"):
									entry.Modified = DateTime.Parse(xmlNode.InnerText)
								elif xmlNode.Name.Equals("Image"):
									entry.Image = Uri(xmlNode.InnerText)
								elif xmlNode.Name.Equals("Tags"):
									for childXmlNode in xmlNode.ChildNodes:
										if childXmlNode.Name.Equals("string"):
											entry.Tags.Add(childXmlNode.InnerText)
			
							entryList.Add(entry)
		
					finally:
						if stream != None:
							stream.Close()
			
						if response != None:
							response.Close()

					request = WebRequest.Create("http://social.apricotan.net/words?limit=25")
					response = None
					stream = None
				
					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(stream)
		
						for entryXmlNode in doc.SelectNodes("/ArrayOfWord/Word"):
							word = Word()
			
							for xmlNode in entryXmlNode.ChildNodes:
								if xmlNode.Name.Equals("Name"):
									word.Name = xmlNode.InnerText
								elif xmlNode.Name.Equals("Attributes"):
									for childXmlNode in xmlNode.ChildNodes:
										if childXmlNode.Name.Equals("string"):
											word.Attributes.Add(childXmlNode.InnerText)
			
							wordList.Add(word)
		
					finally:
						if stream != None:
							stream.Close()
			
						if response != None:
							response.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

		def onCompleted(task):
			global dateTime, recentEntryList, recentWordList

			if entryList.Count > 0:
				newEntryList = List[Entry]()
				nowDateTime = DateTime.Now
				dt = DateTime(0)

				for entry in entryList:
					if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False and entry.Modified > dateTime and entry.Modified <= nowDateTime:
						if not newEntryList.Exists(lambda x: x.Resource.Equals(entry.Resource)):
							newEntryList.Add(entry)
				
					if entry.Modified > dt:
						dt = entry.Modified

				if dt > dateTime:
					dateTime = dt
				else:
					dateTime = nowDateTime

				if newEntryList.Count > 0:
					if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
						Script.Instance.Suggest("ソーシャル", newEntryList)
					else:
						Script.Instance.Suggest("Social", newEntryList)
				
					for entry in entryList:
						if entry.HasTags:
							sequenceList = List[Sequence]()

							for sequence in Script.Instance.Sequences:
								if sequence.Name.Equals("Activate"):
									sequenceList.Add(sequence)
						
							if Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, entry.Tags)):
								break

				recentEntryList.Clear()

				hashSet = HashSet[Uri]()

				for entry in entryList:
					if hashSet.Contains(entry.Resource) == False and hashSet.Count < 10:
						hashSet.Add(entry.Resource)

					if hashSet.Contains(entry.Resource):
						recentEntryList.Add(entry)

			if wordList.Count > 0:
				def match(word):
					for w in Script.Instance.Words:
						if w.Name.Equals(word.Name):
							return False

					return True

				recentWordList.Clear()
				recentWordList.AddRange(wordList.FindAll(match))

				if recentWordList.Count > 10:
					recentWordList.RemoveRange(10, recentWordList.Count - 10)
					
		Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

def onOpened(s, e):
	global menuItem, recentEntryList, recentWordList

	maxLength = 20

	menuItem.Items.Clear()

	pingMenuItem = MenuItem()
	pongMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		pingMenuItem.Header = "クラウドのお気に入りに送る"
	else:
		pingMenuItem.Header = "Ping"

	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		pongMenuItem.Header = "クラウドから言葉を受け取る"
	else:
		pongMenuItem.Header = "Pong"
	
	menuItem.Items.Add(pingMenuItem)
	menuItem.Items.Add(Separator())
	menuItem.Items.Add(pongMenuItem)
	menuItem.Items.Add(Separator())
	
	recentMenuItemList = List[MenuItem]()
	hashSet = HashSet[Uri]()
	queue = Queue[Entry](recentEntryList)

	while queue.Count > 0 and recentMenuItemList.Count < 10:
		entry = queue.Dequeue()
			
		if not hashSet.Contains(entry.Resource):
			hashSet.Add(entry.Resource)

			count = recentEntryList.FindAll(lambda x: x.Resource.Equals(entry.Resource)).Count
			title = entry.Title
				
			if title.Length > maxLength:
				title = String.Concat(title.Remove(maxLength, title.Length - maxLength), "...")

			stackPanel = StackPanel()
			stackPanel.HorizontalAlignment = HorizontalAlignment.Left
			stackPanel.VerticalAlignment = VerticalAlignment.Top
			stackPanel.Orientation = Orientation.Horizontal

			textBlock1 = TextBlock()
			textBlock1.HorizontalAlignment = HorizontalAlignment.Left
			textBlock1.Text = title

			stackPanel.Children.Add(textBlock1)

			if count > 1:
				textBlock2 = TextBlock()
				textBlock2.HorizontalAlignment = HorizontalAlignment.Right
				textBlock2.Margin = Thickness(10, 0, 0, 0)
				textBlock2.Text = count.ToString()
				textBlock2.Foreground = SystemColors.GrayTextBrush

				stackPanel.Children.Add(textBlock2)

			def onClick(sender, args):
				def onStart(state):
					Process.Start(state)

				Task.Factory.StartNew(onStart, sender.Tag.ToString())

			mi = MenuItem()
			mi.Header = stackPanel
			mi.Tag = entry.Resource
			mi.Click += onClick
				
			recentMenuItemList.Add(mi)
			
	if recentMenuItemList.Count > 0:
		for recentMenuItem in recentMenuItemList:
			menuItem.Items.Add(recentMenuItem)
	else:
		naMenuItem = MenuItem()
		naMenuItem.Header = "N/A"
		naMenuItem.IsEnabled = False
		menuItem.Items.Add(naMenuItem)

	pingEntryList = List[Entry]()
	
	for window in Application.Current.Windows:
		if clr.GetClrType(Agent).IsInstanceOfType(window):
			messageList = List[Message](window.Balloon.Messages)
			messageList.Reverse()
			for message in messageList:
				if message.HasAttachments:
					for entry in message.Attachments:
						if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False and pingEntryList.Exists(lambda x: x.Resource.Equals(entry.Resource)) == False:
							pingEntryList.Add(entry)
	
	if pingEntryList.Count == 0:
		naMenuItem = MenuItem()
		naMenuItem.Header = "N/A"
		naMenuItem.IsEnabled = False
		pingMenuItem.Items.Add(naMenuItem)
	else:
		for entry in pingEntryList:
			title = entry.Title
						
			if title.Length > maxLength:
				title = String.Concat(title.Remove(maxLength, title.Length - maxLength), "...")

			mi = MenuItem()
			mi.Header = title
			mi.Tag = entry
			mi.Click += onPing
			
			pingMenuItem.Items.Add(mi)

	for word in recentWordList:
		textBlock = TextBlock()
		textBlock.HorizontalAlignment = HorizontalAlignment.Left
		textBlock.Text = word.Name
		
		def onClick(sender, args):
			addRequired = True

			for word in Script.Instance.Words:
				if word.Name.Equals(sender.Tag.Name):
					addRequired = False

					break

			if addRequired:
				Script.Instance.Words.Add(sender.Tag)

			Script.Instance.Learn(sender.Tag.Name)
			
		mi = MenuItem()
		mi.Header = textBlock
		mi.Tag = word
		mi.Click += onClick
				
		pongMenuItem.Items.Add(mi)

	if pongMenuItem.Items.Count == 0:
		naMenuItem = MenuItem()
		naMenuItem.Header = "N/A"
		naMenuItem.IsEnabled = False
		pongMenuItem.Items.Add(naMenuItem)

def getUserID():
	stringBuilder = StringBuilder()
	nic = NetworkInterface.GetAllNetworkInterfaces()
	
	for i in range(0, nic.Length):
		if i == NetworkInterface.LoopbackInterfaceIndex:
			continue

		addr = nic[i].GetPhysicalAddress()

		if addr.GetAddressBytes().Length == 6:
			sha1 = SHA1CryptoServiceProvider()

			for b in sha1.ComputeHash(Encoding.UTF8.GetBytes(String.Concat(addr.ToString(), Environment.MachineName, Environment.UserName))):
				stringBuilder.Append(b.ToString("x2"))

			break

	return stringBuilder.ToString()

def urlEncode(value):
	unreserved = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~"
	sb = StringBuilder()
	bytes = Encoding.UTF8.GetBytes(value)

	for b in bytes:
		if b < 0x80 and unreserved.IndexOf(Convert.ToChar(b)) != -1:
			sb.Append(Convert.ToChar(b))
		else:
			sb.Append('%' + String.Format("{0:X2}", Convert.ToInt32(b)))

	return sb.ToString()

def onStart(s, e):
	global menuItem, separator, timer

	for window in Application.Current.Windows:
		if window == Application.Current.MainWindow and window.ContextMenu != None:
			if not window.ContextMenu.Items.Contains(menuItem):
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)

	timer.Start()

def onStop(s, e):
	global timer

	timer.Stop()

dateTime = DateTime.Now - TimeSpan(12, 0, 0)
recentEntryList = List[Entry]()
recentWordList = List[Word]()
menuItem = MenuItem()
separator = None

if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
	menuItem.Header = "ソーシャル"
else:
	menuItem.Header = "Social"

timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop