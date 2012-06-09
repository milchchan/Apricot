# -*- coding: utf-8 -*-
# Social.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, ValueType, Boolean, Byte, Char, UInt32, Int32, String, StringComparison, Uri, DateTime, TimeSpan, Array, Convert, BitConverter, Type, Environment
from System.IO import StreamReader
from System.Collections.Generic import List, Dictionary, Queue, HashSet
from System.Globalization import CultureInfo, NumberStyles
from System.Diagnostics import Process, Trace
from System.Reflection import Assembly
from System.Security.Cryptography import SHA1CryptoServiceProvider
from System.Text import StringBuilder, Encoding
from System.Text.RegularExpressions import Regex, RegexOptions, Match
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, HttpRequestHeader, WebRequestMethods
from System.Net.NetworkInformation import NetworkInterface, PhysicalAddress
from System.Windows import Application, Window, HorizontalAlignment, VerticalAlignment, Thickness, SystemColors
from System.Windows.Controls import MenuItem, Separator, Label, StackPanel, Orientation, TextBlock
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Agent, Balloon, Script, Entry, Message, Word, Sequence

autoUpdate = False

# http://www.json.org/
class Json(Object):
	TOKEN_NONE = 0
	TOKEN_CURLY_OPEN = 1
	TOKEN_CURLY_CLOSE = 2
	TOKEN_SQUARED_OPEN = 3
	TOKEN_SQUARED_CLOSE = 4
	TOKEN_COLON = 5
	TOKEN_COMMA = 6
	TOKEN_STRING = 7
	TOKEN_NUMBER = 8
	TOKEN_TRUE = 9
	TOKEN_FALSE = 10
	TOKEN_NULL = 11

	@staticmethod
	def decode(json):
		if json != None:
			charArray = json.ToCharArray()
			index = 0
			value, index, success = Json.parseValue(charArray, index, True)

			return value

		return None

	@staticmethod
	def encode(json):
		sb = StringBuilder()
		success = Json.serializeValue(json, sb)

		return sb.ToString() if success else None

	@staticmethod
	def parseObject(json, index, success):
		dictionary = Dictionary[String, Object]()
		token = -1
		i, index = Json.nextToken(json, index) # {
		done = False

		while not done:
			token = Json.lookAhead(json, index)

			if token == Json.TOKEN_NONE:
				return None, index, False

			elif token == Json.TOKEN_COMMA:
				i, index = Json.nextToken(json, index)

			elif token == Json.TOKEN_CURLY_CLOSE:
				i, index = Json.nextToken(json, index)

				return dictionary, index, success

			else:
				name, index, success = Json.parseString(json, index, success)

				if not success:
					return None, index, success

				token, index = Json.nextToken(json, index)

				if token != Json.TOKEN_COLON:
					return None, index, False

				value, index, success = Json.parseValue(json, index, success)

				if not success:
					return None, index, success

				dictionary.Add(name, value)

		return dictionary, index, success

	@staticmethod
	def parseArray(json, index, success):
		list = List[Object]()
		i, index = Json.nextToken(json, index) # [
		done = False

		while not done:
			token = Json.lookAhead(json, index)

			if token == Json.TOKEN_NONE:
				return None, index, False

			elif token == Json.TOKEN_COMMA:
				i, index = Json.nextToken(json, index)

			elif token == Json.TOKEN_SQUARED_CLOSE:
				i, index = Json.nextToken(json, index)

				break

			else:
				value, index, success = Json.parseValue(json, index, success)

				if not success:
					return None, index, success

				list.Add(value)

		return list.ToArray(), index, success

	@staticmethod
	def parseValue(json, index, success):
		i = Json.lookAhead(json, index)

		if i == Json.TOKEN_STRING:
			return Json.parseString(json, index, success)

		elif i == Json.TOKEN_NUMBER:
			return Json.parseNumber(json, index, success)

		elif i == Json.TOKEN_CURLY_OPEN:
			return Json.parseObject(json, index, success)

		elif i == Json.TOKEN_SQUARED_OPEN:
			return Json.parseArray(json, index, success)

		elif i == Json.TOKEN_TRUE:
			j, index = Json.nextToken(json, index)

			return True, index, success

		elif i == Json.TOKEN_FALSE:
			j, index = Json.nextToken(json, index)

			return False, index, success

		elif i == Json.TOKEN_NULL:
			j, index = Json.nextToken(json, index)

			return None, index, success

		return None, index, False

	@staticmethod
	def parseString(json, index, success):
		s = StringBuilder()
		index = Json.skipWhitespace(json, index)
		c = json[index] # "
		index += 1
		complete = False
		
		while not complete:
			if index == json.Length:
				break

			c = json[index]
			index += 1

			if c == '"':
				complete = True
				break

			elif c == '\\':
				if index == json.Length:
					break

				c = json[index]
				index += 1

				if c == '"':
					s.Append('"')
				elif c == '\\':
					s.Append('\\')
				elif c == '/':
					s.Append('/')
				elif c == 'b':
					s.Append('\b')
				elif c == 'f':
					s.Append('\f')
				elif c == 'n':
					s.Append('\n')
				elif c == 'r':
					s.Append('\r')
				elif c == 't':
					s.Append('\t')
				elif c == 'u':
					remainingLength = json.Length - index

					if remainingLength >= 4:
						sb = StringBuilder()
						
						for i in range(4):
							sb.Append(json[index + i])

						success, codePoint = UInt32.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
						
						if not success:
							return String.Empty, index, success

						s.Append(Encoding.UTF32.GetString(BitConverter.GetBytes(codePoint)))
						index += 4

					else:
						break

			else:
				s.Append(c)

		if not complete:
			return None, index, False

		return s.ToString(), index, success

	@staticmethod
	def parseNumber(json, index, success):
		index = Json.skipWhitespace(json, index)
		lastIndex = Json.getLastIndexOfNumber(json, index)
		charLength = (lastIndex - index) + 1

		sb = StringBuilder()
						
		for i in range(charLength):
			sb.Append(json[index + i])

		success, number = Double.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture)
		index = lastIndex + 1
		
		return number, index, success

	@staticmethod
	def getLastIndexOfNumber(json, index):
		lastIndex = index

		while lastIndex < json.Length:
			if "0123456789+-.eE".IndexOf(json[lastIndex]) == -1:
				break

			lastIndex += 1

		return lastIndex - 1

	@staticmethod
	def skipWhitespace(json, index):
		while index < json.Length:
			if " \t\n\r".IndexOf(json[index]) == -1:
				break

			index += 1

		return index

	@staticmethod
	def lookAhead(json, index):
		i, j = Json.nextToken(json, index)
		
		return i

	@staticmethod
	def nextToken(json, index):
		index = Json.skipWhitespace(json, index)

		if index == json.Length:
			return Json.TOKEN_NONE, index

		c = json[index]
		index += 1

		if c == '{':
			return Json.TOKEN_CURLY_OPEN, index

		elif c == '}':
			return Json.TOKEN_CURLY_CLOSE, index

		elif c == '[':
			return Json.TOKEN_SQUARED_OPEN, index

		elif c == ']':
			return Json.TOKEN_SQUARED_CLOSE, index

		elif c == ',':
			return Json.TOKEN_COMMA, index

		elif c == '"':
			return Json.TOKEN_STRING, index

		elif c == '0' or c == '1' or c == '2' or c == '3' or c == '4' or c == '5' or c == '6' or c == '7' or c == '8' or c == '9' or c == '-':
			return Json.TOKEN_NUMBER, index

		elif c == ':':
			return Json.TOKEN_COLON, index

		index -= 1
		remainingLength = json.Length - index

		# false
		if remainingLength >= 5:
			if json[index] == 'f' and json[index + 1] == 'a' and json[index + 2] == 'l' and json[index + 3] == 's' and json[index + 4] == 'e':
				index += 5

				return Json.TOKEN_FALSE, index

		# true
		if remainingLength >= 4:
			if json[index] == 't' and json[index + 1] == 'r' and json[index + 2] == 'u' and json[index + 3] == 'e':
				index += 4

				return Json.TOKEN_TRUE, index

		# null
		if remainingLength >= 4:
			if json[index] == 'n' and json[index + 1] == 'u' and json[index + 2] == 'l' and json[index + 3] == 'l':
				index += 4

				return Json.TOKEN_NULL, index

		return Json.TOKEN_NONE, index

	@staticmethod
	def serializeValue(value, builder):
		success = True

		if clr.GetClrType(String).IsInstanceOfType(value):
			success = Json.serializeString(value, builder)
		elif clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(value):
			success = Json.serializeObject(value, builder)
		elif clr.GetClrType(Array).IsInstanceOfType(value):
			success = Json.serializeArray(value, builder)
		elif clr.GetClrType(Boolean).IsInstanceOfType(value):
			builder.Append("true" if value else "false")
		elif clr.GetClrType(ValueType).IsInstanceOfType(value):
			success = Json.serializeNumber(Convert.ToDouble(value), builder)
		elif value == None:
			builder.Append("null")
		else:
			success = False

		return success

	@staticmethod
	def serializeObject(dictionary, builder):
		builder.Append("{")

		first = True

		for kvp in dictionary:
			if not first:
				builder.Append(", ")

			Json.serializeString(kvp.Key, builder)
			builder.Append(":")

			if not Json.serializeValue(kvp.Value, builder):
				return False

			first = False

		builder.Append("}")

		return True

	@staticmethod
	def serializeArray(array, builder):
		builder.Append("[")

		first = True

		for value in array:
			if not first:
				builder.Append(", ")

			if not Json.serializeValue(value, builder):
				return False

			first = False

		builder.Append("]")

		return True

	@staticmethod
	def serializeString(s, builder):
		builder.Append("\"")

		for c in s.ToCharArray():
			if c == '"':
				builder.Append("\\\"")
			elif c == '\\':
				builder.Append("\\\\")
			elif c == '\b':
				builder.Append("\\b")
			elif c == '\f':
				builder.Append("\\f")
			elif c == '\n':
				builder.Append("\\n")
			elif c == '\r':
				builder.Append("\\r")
			elif c == '\t':
				builder.Append("\\t")
			else:
				codepoint = Convert.ToInt32(c)

				if ((codepoint >= 32) and (codepoint <= 126)):
					builder.Append(c)
				else:
					builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'))

		builder.Append("\"")

		return True

	@staticmethod
	def serializeNumber(number, builder):
		builder.Append(Convert.ToString(number, CultureInfo.InvariantCulture))
		
		return True

def onTick(timer, e):
	entryList = List[Entry]()
	wordList = List[Word]()

	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				request = WebRequest.Create("http://social.apricotan.net/entries?format=json&limit=25")
				response = None
				stream = None
				streamReader = None

				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					streamReader = StreamReader(stream)
					json = Json.decode(streamReader.ReadToEnd())

					if json != None:
						if clr.GetClrType(Array).IsInstanceOfType(json):
							for obj in json:
								if obj != None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
										if obj.ContainsKey("entry"):
											entry = Entry()

											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
												if obj["entry"].ContainsKey("resource"):
													if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
														entry.Resource = Uri(obj["entry"]["resource"])

												if obj["entry"].ContainsKey("title"):
													if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
														entry.Title = obj["entry"]["title"]

												if obj["entry"].ContainsKey("created"):
													if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
														entry.Created = DateTime.Parse(obj["entry"]["created"])

												if obj["entry"].ContainsKey("modified"):
													if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
														entry.Modified = DateTime.Parse(obj["entry"]["modified"])

												if obj["entry"].ContainsKey("image"):
													if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
														entry.Image = Uri(obj["entry"]["image"])

												if obj["entry"].ContainsKey("tags"):
													if clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
														for o in obj["entry"]["tags"]:
															if clr.GetClrType(String).IsInstanceOfType(o):
																entry.Tags.Add(o)

											entryList.Add(entry)

				finally:
					if streamReader != None:
						streamReader.Close()

					if stream != None:
						stream.Close()
			
					if response != None:
						response.Close()

				request = WebRequest.Create("http://social.apricotan.net/words?format=json&limit=50")
				response = None
				stream = None
				streamReader = None
				
				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					streamReader = StreamReader(stream)
					json = Json.decode(streamReader.ReadToEnd())

					if json != None:
						if clr.GetClrType(Array).IsInstanceOfType(json):
							for obj in json:
								if obj != None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
										if obj.ContainsKey("word"):
											word = Word()

											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
												if obj["word"].ContainsKey("name"):
													if clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
														word.Name = obj["word"]["name"]

												if obj["word"].ContainsKey("attributes"):
													if clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
														for o in obj["word"]["attributes"]:
															if clr.GetClrType(String).IsInstanceOfType(o):
																word.Attributes.Add(o)

											wordList.Add(word)
		
				finally:
					if streamReader != None:
						streamReader.Close()

					if stream != None:
						stream.Close()
			
					if response != None:
						response.Close()

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

	def onCompleted(task):
		global autoUpdate, dateTime, recentEntryList, recentWordList

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

			if autoUpdate:
				for recentWord in recentWordList:
					Script.Instance.Words.Add(recentWord)
			
	Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(10)
	timer.Start()

def onPing(s, e):
	entry = s.Tag
		
	if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False:
		stringBuilder = StringBuilder()
		newWordList = List[Dictionary[String, Object]]()
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
						dictionary1 = Dictionary[String, Object]()
						dictionary2 = Dictionary[String, Object]()
						dictionary2.Add("name", word.Name)
						attributeList = List[String](word.Attributes)
						attributeList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.InvariantCulture))
						dictionary2.Add("attributes", attributeList.ToArray())
						dictionary1.Add("word", dictionary2)
						newWordList.Add(dictionary1)

						break

			json = Json.encode(newWordList.ToArray())

			if json != None:
				bytes = Encoding.UTF8.GetBytes(json)

		def onUpdate():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					client = WebClient()
					client.Headers.Add(HttpRequestHeader.ContentType, "application/json")
					
					if bytes == None:
						client.UploadData(Uri(stringBuilder.ToString()), WebRequestMethods.Http.Post, Encoding.UTF8.GetBytes(String.Empty))
					else:
						client.UploadData(Uri(stringBuilder.ToString()), WebRequestMethods.Http.Post, bytes)

					request = WebRequest.Create("http://social.apricotan.net/entries?format=json&limit=25")
					response = None
					stream = None
					streamReader = None

					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = Json.decode(streamReader.ReadToEnd())

						if json != None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj != None:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											if obj.ContainsKey("entry"):
												entry = Entry()

												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
													if obj["entry"].ContainsKey("resource"):
														if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
															entry.Resource = Uri(obj["entry"]["resource"])

													if obj["entry"].ContainsKey("title"):
														if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
															entry.Title = obj["entry"]["title"]

													if obj["entry"].ContainsKey("created"):
														if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
															entry.Created = DateTime.Parse(obj["entry"]["created"])

													if obj["entry"].ContainsKey("modified"):
														if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
															entry.Modified = DateTime.Parse(obj["entry"]["modified"])

													if obj["entry"].ContainsKey("image"):
														if clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
															entry.Image = Uri(obj["entry"]["image"])

													if obj["entry"].ContainsKey("tags"):
														if clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
															for o in obj["entry"]["tags"]:
																if clr.GetClrType(String).IsInstanceOfType(o):
																	entry.Tags.Add(o)

												entryList.Add(entry)

					finally:
						if streamReader != None:
							streamReader.Close()

						if stream != None:
							stream.Close()
			
						if response != None:
							response.Close()

					request = WebRequest.Create("http://social.apricotan.net/words?format=json&limit=50")
					response = None
					stream = None
					streamReader = None
				
					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = Json.decode(streamReader.ReadToEnd())

						if json != None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj != None:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											if obj.ContainsKey("word"):
												word = Word()

												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
													if obj["word"].ContainsKey("name"):
														if clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
															word.Name = obj["word"]["name"]

													if obj["word"].ContainsKey("attributes"):
														if clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
															for o in obj["word"]["attributes"]:
																if clr.GetClrType(String).IsInstanceOfType(o):
																	word.Attributes.Add(o)

												wordList.Add(word)

					finally:
						if streamReader != None:
							streamReader.Close()

						if stream != None:
							stream.Close()
			
						if response != None:
							response.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

		def onCompleted(task):
			global autoUpdate, dateTime, recentEntryList, recentWordList

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

				if autoUpdate:
					for recentWord in recentWordList:
						Script.Instance.Words.Add(recentWord)

		Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

def onOpened(s, e):
	global autoUpdate, menuItem, recentEntryList, recentWordList

	maxLength = 20

	menuItem.Items.Clear()

	pingMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		pingMenuItem.Header = "クラウドのお気に入りに送る"
	else:
		pingMenuItem.Header = "Ping"

	menuItem.Items.Add(pingMenuItem)
	menuItem.Items.Add(Separator())

	if not autoUpdate:
		pongMenuItem = MenuItem()
	
		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			pongMenuItem.Header = "クラウドから言葉を受け取る"
		else:
			pongMenuItem.Header = "Pong"
	
		menuItem.Items.Add(pongMenuItem)
		menuItem.Items.Add(Separator())

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