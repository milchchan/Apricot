# -*- coding: utf-8 -*-
# Facebook.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, Byte, UInt32, Double, Char, String, Uri, DateTime, TimeSpan, Array, Environment, StringComparison, Guid, Convert, BitConverter, Action
from System.Collections.Generic import List, Stack, Dictionary, KeyValuePair
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, NumberStyles
from System.IO import Stream, FileStream, StreamReader, StreamWriter, Path, Directory, File, FileMode, FileAccess, FileShare
from System.Reflection import Assembly
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, Match, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse, WebRequestMethods, HttpWebResponse, HttpStatusCode
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Thickness, RoutedEventHandler, SystemColors
from System.Windows.Controls import MenuItem, Separator, StackPanel, Border, ComboBox, ComboBoxItem, Button, CheckBox, WebBrowser, Orientation
from System.Windows.Media import Colors, SolidColorBrush
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Microsoft.Win32 import OpenFileDialog
from Apricot import Agent, Script, Entry, Message, Character, Word, Sequence

appId = "161130897240780"
appSecret = "07aad39af244adc7eb93b89dc4047bf2"
accessToken = None

# http://www.json.org/
class JsonDecoder(Object):
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
		if json is not None:
			charArray = json.ToCharArray()
			index = 0
			value, index, success = JsonDecoder.parseValue(charArray, index, True)

			return value

		return None

	@staticmethod
	def parseObject(json, index, success):
		dictionary = Dictionary[String, Object]()
		token = -1
		i, index = JsonDecoder.nextToken(json, index) # {
		done = False

		while not done:
			token = JsonDecoder.lookAhead(json, index)

			if token == JsonDecoder.TOKEN_NONE:
				return None, index, False

			elif token == JsonDecoder.TOKEN_COMMA:
				i, index = JsonDecoder.nextToken(json, index)

			elif token == JsonDecoder.TOKEN_CURLY_CLOSE:
				i, index = JsonDecoder.nextToken(json, index)

				return dictionary, index, success

			else:
				name, index, success = JsonDecoder.parseString(json, index, success)

				if not success:
					return None, index, success

				token, index = JsonDecoder.nextToken(json, index)

				if token != JsonDecoder.TOKEN_COLON:
					return None, index, False

				value, index, success = JsonDecoder.parseValue(json, index, success)

				if not success:
					return None, index, success

				dictionary.Add(name, value)

		return dictionary, index, success

	@staticmethod
	def parseArray(json, index, success):
		list = List[Object]()
		i, index = JsonDecoder.nextToken(json, index) # [
		done = False

		while not done:
			token = JsonDecoder.lookAhead(json, index)

			if token == JsonDecoder.TOKEN_NONE:
				return None, index, False

			elif token == JsonDecoder.TOKEN_COMMA:
				i, index = JsonDecoder.nextToken(json, index)

			elif token == JsonDecoder.TOKEN_SQUARED_CLOSE:
				i, index = JsonDecoder.nextToken(json, index)

				break

			else:
				value, index, success = JsonDecoder.parseValue(json, index, success)

				if not success:
					return None, index, success

				list.Add(value)

		return list.ToArray(), index, success

	@staticmethod
	def parseValue(json, index, success):
		i = JsonDecoder.lookAhead(json, index)

		if i == JsonDecoder.TOKEN_STRING:
			return JsonDecoder.parseString(json, index, success)

		elif i == JsonDecoder.TOKEN_NUMBER:
			return JsonDecoder.parseNumber(json, index, success)

		elif i == JsonDecoder.TOKEN_CURLY_OPEN:
			return JsonDecoder.parseObject(json, index, success)

		elif i == JsonDecoder.TOKEN_SQUARED_OPEN:
			return JsonDecoder.parseArray(json, index, success)

		elif i == JsonDecoder.TOKEN_TRUE:
			j, index = JsonDecoder.nextToken(json, index)

			return True, index, success

		elif i == JsonDecoder.TOKEN_FALSE:
			j, index = JsonDecoder.nextToken(json, index)

			return False, index, success

		elif i == JsonDecoder.TOKEN_NULL:
			j, index = JsonDecoder.nextToken(json, index)

			return None, index, success

		return None, index, False

	@staticmethod
	def parseString(json, index, success):
		s = StringBuilder()
		index = JsonDecoder.skipWhitespace(json, index)
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
		index = JsonDecoder.skipWhitespace(json, index)
		lastIndex = JsonDecoder.getLastIndexOfNumber(json, index)
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
		i, j = JsonDecoder.nextToken(json, index)
		
		return i

	@staticmethod
	def nextToken(json, index):
		index = JsonDecoder.skipWhitespace(json, index)

		if index == json.Length:
			return JsonDecoder.TOKEN_NONE, index

		c = json[index]
		index += 1

		if c == '{':
			return JsonDecoder.TOKEN_CURLY_OPEN, index

		elif c == '}':
			return JsonDecoder.TOKEN_CURLY_CLOSE, index

		elif c == '[':
			return JsonDecoder.TOKEN_SQUARED_OPEN, index

		elif c == ']':
			return JsonDecoder.TOKEN_SQUARED_CLOSE, index

		elif c == ',':
			return JsonDecoder.TOKEN_COMMA, index

		elif c == '"':
			return JsonDecoder.TOKEN_STRING, index

		elif c == '0' or c == '1' or c == '2' or c == '3' or c == '4' or c == '5' or c == '6' or c == '7' or c == '8' or c == '9' or c == '-':
			return JsonDecoder.TOKEN_NUMBER, index

		elif c == ':':
			return JsonDecoder.TOKEN_COLON, index

		index -= 1
		remainingLength = json.Length - index

		# false
		if remainingLength >= 5:
			if json[index] == 'f' and json[index + 1] == 'a' and json[index + 2] == 'l' and json[index + 3] == 's' and json[index + 4] == 'e':
				index += 5

				return JsonDecoder.TOKEN_FALSE, index

		# true
		if remainingLength >= 4:
			if json[index] == 't' and json[index + 1] == 'r' and json[index + 2] == 'u' and json[index + 3] == 'e':
				index += 4

				return JsonDecoder.TOKEN_TRUE, index

		# null
		if remainingLength >= 4:
			if json[index] == 'n' and json[index + 1] == 'u' and json[index + 2] == 'l' and json[index + 3] == 'l':
				index += 4

				return JsonDecoder.TOKEN_NULL, index

		return JsonDecoder.TOKEN_NONE, index

def update():
	global accessToken

	d = Dictionary[String, String]()
	d.Add("access_token", accessToken)
	sb = StringBuilder()
		
	for kvp in d:
		if sb.Length > 0:
			sb.Append('&')

		sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))
		
	request = WebRequest.Create(String.Concat("https://graph.facebook.com/me/home?", sb.ToString()))
	entryList = List[Entry]()
	context = TaskScheduler.FromCurrentSynchronizationContext()
			
	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				response = None
				stream = None
				streamReader = None

				try:
					response = request.GetResponse()

					if response.StatusCode == HttpStatusCode.OK:
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = JsonDecoder.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
								if json.ContainsKey("data"):
									if json["data"] is not None:
										if clr.GetClrType(Array).IsInstanceOfType(json["data"]):
											for obj in json["data"]:
												if obj is not None:
													if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
														entry = Entry()
														userId = None

														if obj.ContainsKey("id"):
															if obj["id"] is not None:
																array = obj["id"].Split('_')

																if array.Length == 2:
																	entry.Resource = Uri(String.Format("http://www.facebook.com/{0}/posts/{1}", array[0], array[1]))
													
														if obj.ContainsKey("from"):
															if obj["from"] is not None:
																if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["from"]):
																	entry.Author = obj["from"]["name"]

																	if obj["from"].ContainsKey("id"):
																		userId = obj["from"]["id"]
																
														if obj.ContainsKey("created_time"):
															if obj["created_time"] is not None:
																entry.Created = DateTime.Parse(obj["created_time"])

														if obj.ContainsKey("updated_time"):
															if obj["updated_time"] is not None:
																entry.Modified = DateTime.Parse(obj["updated_time"])

														if obj.ContainsKey("message"):
															if obj["message"] is not None:
																title = Regex.Replace(obj["message"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)

																if title.Length > 100:
																	title = title.Remove(100, title.Length - 100)

																entry.Title = title

														if entry.Resource is not None and userId is not None:
															entry.Image = Uri(String.Format("https://graph.facebook.com/{0}/picture?access_token={1}", userId, urlEncode(accessToken)))
															entryList.Add(entry)

					return response.StatusCode.ToString()
					
				finally:
					if streamReader is not None:
						streamReader.Close()

					if stream is not None:
						stream.Close()
				
					if response is not None:
						response.Close()

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

		return None

	def onCompleted(task):
		global accessToken, dateTime

		if task.Result is not None:
			if task.Result.Equals(HttpStatusCode.Unauthorized.ToString()):
				accessToken = None

		if entryList.Count > 0:
			dt = DateTime(0)
			newEntryList = List[Entry]()

			for entry in entryList:
				if entry.Created > dt:
					dt = entry.Created

				if entry.Created > dateTime and not String.IsNullOrEmpty(entry.Title):
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

	Task.Factory.StartNew[String](onUpdate, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[String]](onCompleted), context)

def onTick(timer, e):
	global accessToken

	if accessToken is not None:
		update()

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(5)
	timer.Start()

def post(text, filename):
	global accessToken

	d = Dictionary[String, String]()
	d.Add("access_token", accessToken)
	sb = StringBuilder()

	for kvp in d:
		if sb.Length > 0:
			sb.Append('&')

		sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

	updateRequest = WebRequest.Create(String.Concat("https://graph.facebook.com/me/home?", sb.ToString()))
	entryList = List[Entry]()
	context = TaskScheduler.FromCurrentSynchronizationContext()
	
	def onCompleted(task):
		global accessToken, dateTime

		if task.Result is not None:
			if task.Result.Equals(HttpStatusCode.Unauthorized.ToString()):
				accessToken = None

		if entryList.Count > 0:
			dt = DateTime(0)
			newEntryList = List[Entry]()

			for entry in entryList:
				if entry.Created > dt:
					dt = entry.Created

				if entry.Created > dateTime and not String.IsNullOrEmpty(entry.Title):
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

	if File.Exists(filename):
		byteList = List[Byte]()

		d.Clear()
		d.Add("access_token", accessToken)
		d.Add("source", String.Concat('@', Path.GetFileName(filename)))
		d.Add("message", text)
		sb.Clear()

		for kvp in d:
			if sb.Length > 0:
				sb.Append('&')

			sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

		uploadRequest = WebRequest.Create(String.Format("https://graph.facebook.com/me/photos?{0}", sb.ToString()))

		def onPrepare():
			try:
				byteList.AddRange(File.ReadAllBytes(filename))

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

		def onReady(task):
			boundary = Guid.NewGuid().ToString()
			encoding = "iso-8859-1"
			header = String.Format("--{0}", boundary)
			footer = String.Format("--{0}--", boundary)

			contents = StringBuilder()
			contents.AppendLine(header)

			extension = Path.GetExtension(filename)
			fileHeader = String.Format("Content-Disposition: file; filename=\"{0}\"", Path.GetFileName(filename))
			fileData = Encoding.GetEncoding(encoding).GetString(byteList.ToArray())

			contents.AppendLine(fileHeader)
	
			if extension.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) or extension.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase):
				contents.AppendLine(String.Format("Content-Type: {0}", "image/jpeg"))
			elif extension.EndsWith(".png", StringComparison.OrdinalIgnoreCase):
				contents.AppendLine(String.Format("Content-Type: {0}", "image/png"))

			contents.AppendLine()
			contents.AppendLine(fileData)
			contents.AppendLine(footer)

			byteList.Clear()
			byteList.AddRange(Encoding.GetEncoding(encoding).GetBytes(contents.ToString()))
			
			uploadRequest.PreAuthenticate = True
			uploadRequest.ContentType = String.Format("multipart/form-data; boundary={0}", boundary)
			uploadRequest.Method = WebRequestMethods.Http.Post
			uploadRequest.ContentLength = byteList.Count

		def onExecute(task):
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					uploadRequestStream = None
					uploadResponse = None
					uploadResponseStream = None
					uploadStreamReader = None
					statusCode = None
					isUploaded = False

					try:
						uploadRequestStream = uploadRequest.GetRequestStream()
						uploadRequestStream.Write(byteList.ToArray(), 0, byteList.Count)
						uploadResponse = uploadRequest.GetResponse()

						if uploadResponse.StatusCode == HttpStatusCode.OK:
							uploadResponseStream = uploadResponse.GetResponseStream()
							uploadStreamReader = StreamReader(uploadResponseStream)

							json = JsonDecoder.decode(uploadStreamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("id"):
										isUploaded = True

						statusCode = uploadResponse.StatusCode.ToString()
					
					finally:
						if uploadStreamReader is not None:
							uploadStreamReader.Close()

						if uploadResponseStream is not None:
							uploadResponseStream.Close()

						if uploadResponse is not None:
							uploadResponse.Close()

						if uploadRequestStream is not None:
							uploadRequestStream.Close()

					if isUploaded:
						updateResponse = None
						updateStream = None
						updateStreamReader = None

						try:
							updateResponse = updateRequest.GetResponse()
							updateStream = updateResponse.GetResponseStream()
							updateStreamReader = StreamReader(updateStream)
							json = JsonDecoder.decode(updateStreamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("data"):
										if json["data"] is not None:
											if clr.GetClrType(Array).IsInstanceOfType(json["data"]):
												for obj in json["data"]:
													if obj is not None:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
															entry = Entry()
															userId = None

															if obj.ContainsKey("id"):
																if obj["id"] is not None:
																	array = obj["id"].Split('_')

																	if array.Length == 2:
																		entry.Resource = Uri(String.Format("http://www.facebook.com/{0}/posts/{1}", array[0], array[1]))
													
															if obj.ContainsKey("from"):
																if obj["from"] is not None:
																	if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["from"]):
																		entry.Author = obj["from"]["name"]

																		if obj["from"].ContainsKey("id"):
																			userId = obj["from"]["id"]
																
															if obj.ContainsKey("created_time"):
																if obj["created_time"] is not None:
																	entry.Created = DateTime.Parse(obj["created_time"])

															if obj.ContainsKey("updated_time"):
																if obj["updated_time"] is not None:
																	entry.Modified = DateTime.Parse(obj["updated_time"])

															if obj.ContainsKey("message"):
																if obj["message"] is not None:
																	title = Regex.Replace(obj["message"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)

																	if title.Length > 100:
																		title = title.Remove(100, title.Length - 100)

																	entry.Title = title

															if entry.Resource is not None and userId is not None:
																entry.Image = Uri(String.Format("https://graph.facebook.com/{0}/picture?access_token={1}", userId, urlEncode(accessToken)))
																entryList.Add(entry)
					
						finally:
							if updateStreamReader is not None:
								updateStreamReader.Close()

							if updateStream is not None:
								updateStream.Close()
				
							if updateResponse is not None:
								updateResponse.Close()

					return statusCode

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			return None

		Task.Factory.StartNew(onPrepare, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith[String](onExecute, TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[String]](onCompleted), context)

	else:
		d.Clear()
		d.Add("access_token", accessToken)
		d.Add("message", text)
		sb.Clear()

		for kvp in d:
			if sb.Length > 0:
				sb.Append('&')

			sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

		postRequest = WebRequest.Create(String.Format("https://graph.facebook.com/me/feed?{0}", sb.ToString()))
		postRequest.Method = WebRequestMethods.Http.Post

		def onExecute():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					postResponse = None
					postResponseStream = None
					postStreamReader = None
					statusCode = None
					isPosted = False

					try:
						postRequestStream = None

						try:
							bytes = Encoding.ASCII.GetBytes(String.Empty)
							postRequestStream = postRequest.GetRequestStream()
							postRequestStream.Write(bytes, 0, bytes.Length)
						
						finally:
							if postRequestStream is not None:
								postRequestStream.Close()

						postResponse = postRequest.GetResponse()

						if postResponse.StatusCode == HttpStatusCode.OK:
							postResponseStream = postResponse.GetResponseStream()
							postStreamReader = StreamReader(postResponseStream)

							json = JsonDecoder.decode(postStreamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("id"):
										isPosted = True

						statusCode = postResponse.StatusCode.ToString()
					
					finally:
						if postStreamReader is not None:
							postStreamReader.Close()

						if postResponseStream is not None:
							postResponseStream.Close()

						if postResponse is not None:
							postResponse.Close()

					if isPosted:
						updateResponse = None
						updateStream = None
						updateStreamReader = None

						try:
							updateResponse = updateRequest.GetResponse()
							updateStream = updateResponse.GetResponseStream()
							updateStreamReader = StreamReader(updateStream)
							json = JsonDecoder.decode(updateStreamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("data"):
										if json["data"] is not None:
											if clr.GetClrType(Array).IsInstanceOfType(json["data"]):
												for obj in json["data"]:
													if obj is not None:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
															entry = Entry()
															userId = None

															if obj.ContainsKey("id"):
																if obj["id"] is not None:
																	array = obj["id"].Split('_')

																	if array.Length == 2:
																		entry.Resource = Uri(String.Format("http://www.facebook.com/{0}/posts/{1}", array[0], array[1]))
													
															if obj.ContainsKey("from"):
																if obj["from"] is not None:
																	if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["from"]):
																		entry.Author = obj["from"]["name"]

																		if obj["from"].ContainsKey("id"):
																			userId = obj["from"]["id"]
																
															if obj.ContainsKey("created_time"):
																if obj["created_time"] is not None:
																	entry.Created = DateTime.Parse(obj["created_time"])

															if obj.ContainsKey("updated_time"):
																if obj["updated_time"] is not None:
																	entry.Modified = DateTime.Parse(obj["updated_time"])

															if obj.ContainsKey("message"):
																if obj["message"] is not None:
																	title = Regex.Replace(obj["message"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)

																	if title.Length > 100:
																		title = title.Remove(100, title.Length - 100)

																	entry.Title = title

															if entry.Resource is not None and userId is not None:
																entry.Image = Uri(String.Format("https://graph.facebook.com/{0}/picture?access_token={1}", userId, urlEncode(accessToken)))
																entryList.Add(entry)

						finally:
							if updateStreamReader is not None:
								updateStreamReader.Close()

							if updateStream is not None:
								updateStream.Close()
				
							if updateResponse is not None:
								updateResponse.Close()

					return statusCode

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			return None

		Task.Factory.StartNew[String](onExecute, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[String]](onCompleted), context)

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

def onOpened(s, e):
	global appId, appSecret, accessToken, menuItem

	menuItem.Items.Clear()

	if accessToken is None:
		def onLogInClick(source, rea):
			window = Window()

			stackPanel = StackPanel()
			stackPanel.UseLayoutRounding = True
			stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel.Orientation = Orientation.Vertical

			def onWebBrowserNavigated(sender, nea):
				if Regex.IsMatch(nea.Uri.AbsoluteUri, "^http(s)?://www\\.facebook\\.com/connect/login_success\\.html", RegexOptions.CultureInvariant):
					for match in Regex.Matches(nea.Uri.Query, "\\??(?<1>.+?)=(?<2>.*?)(?:&|$)", RegexOptions.CultureInvariant | RegexOptions.Singleline):
						if match.Groups[1].Value.Equals("code"):
							sb = StringBuilder()
							d = Dictionary[String, String]()
							d.Add("client_id", appId)
							d.Add("redirect_uri", "https://www.facebook.com/connect/login_success.html")
							d.Add("client_secret", appSecret)
							d.Add("code", match.Groups[2].Value)

							for kvp in d:
								if sb.Length > 0:
									sb.Append('&')

								sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

							request = WebRequest.Create(String.Concat("https://graph.facebook.com/oauth/access_token?", sb.ToString()))
							dictionary = Dictionary[String, String]()
							context = TaskScheduler.FromCurrentSynchronizationContext()

							def onAuth():
								if NetworkInterface.GetIsNetworkAvailable():
									try:
										response = None
										stream = None
										streamReader = None
										shortLivedAccessToken = None

										try:
											response = request.GetResponse()
											stream = response.GetResponseStream()
											streamReader = StreamReader(stream)

											for m in Regex.Matches(streamReader.ReadToEnd(), "(?<1>.+?)=(?<2>.*?)(?:&|$)", RegexOptions.CultureInvariant | RegexOptions.Singleline):
												if m.Groups[1].Value.Equals("access_token"):
													shortLivedAccessToken =  m.Groups[2].Value

										finally:
											if streamReader is not None:
												streamReader.Close()
												streamReader = None

											if stream is not None:
												stream.Close()
												stream = None
				
											if response is not None:
												response.Close()
												response = None

										if shortLivedAccessToken is not None:
											d.Remove("redirect_uri")
											d.Remove("code")
											d.Add("grant_type", "fb_exchange_token")
											d.Add("fb_exchange_token", shortLivedAccessToken)
											sb.Clear()

											for kvp in d:
												if sb.Length > 0:
													sb.Append('&')

												sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

											r = WebRequest.Create(String.Concat("https://graph.facebook.com/oauth/access_token?", sb.ToString()))
											
											try:
												response = r.GetResponse()
												stream = response.GetResponseStream()
												streamReader = StreamReader(stream)

												for m in Regex.Matches(streamReader.ReadToEnd(), "(?<1>.+?)=(?<2>.*?)(?:&|$)", RegexOptions.CultureInvariant | RegexOptions.Singleline):
													if not dictionary.ContainsKey(m.Groups[1].Value):
														dictionary.Add(m.Groups[1].Value, m.Groups[2].Value)

											finally:
												if streamReader is not None:
													streamReader.Close()

												if stream is not None:
													stream.Close()
				
												if response is not None:
													response.Close()

										if dictionary.ContainsKey("access_token"):
											config = None
											directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name)
											
											if Directory.Exists(directory):
												fileName1 = Path.GetFileName(Assembly.GetExecutingAssembly().Location)
		
												for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
													if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
														exeConfigurationFileMap = ExeConfigurationFileMap()
														exeConfigurationFileMap.ExeConfigFilename = fileName2
														config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
											if config is None:
												config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
												directory = None

											if config.HasFile:
												if config.AppSettings.Settings["Scripts"] is not None:
													for fileName in Directory.EnumerateFiles(config.AppSettings.Settings["Scripts"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["Scripts"].Value), "*.py"):
														fs1 = None
														sr = None
														lines = None

														try:
															fs1 = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
															sr = StreamReader(fs1, UTF8Encoding(False), True)
															lines = sr.ReadToEnd()

														finally:
															if sr is not None:
																sr.Close()

															if fs1 is not None:
																fs1.Close()

														if lines is not None:
															if Regex.IsMatch(lines, "\\#\\s*Facebook.py", RegexOptions.CultureInvariant):
																lines = Regex.Replace(lines, String.Concat("(?<=", Environment.NewLine, ")accessToken\\s*=\\s*\\S+?(?=", Environment.NewLine, ")"), String.Format("accessToken = \"{0}\"", dictionary["access_token"]), RegexOptions.CultureInvariant)
																fs2 = None
																sw = None

																try:
																	fs2 = FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read)
																	sw = StreamWriter(fs2, UTF8Encoding(False))
																	sw.Write(lines)

																finally:
																	if sw is not None:
																		sw.Close()

																	if fs2 is not None:
																		fs2.Close()

									except Exception, e:
										Trace.WriteLine(e.clsException.Message)
										Trace.WriteLine(e.clsException.StackTrace)

							def onCompleted(task):
								global accessToken

								if dictionary.ContainsKey("access_token"):
									accessToken = dictionary["access_token"]

							Task.Factory.StartNew(onAuth, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, context)

							break

			solidColorBrush1 = SolidColorBrush(Colors.Black)
			solidColorBrush1.Opacity = 0.25

			if solidColorBrush1.CanFreeze:
				solidColorBrush1.Freeze()

			border1 = Border()
			border1.HorizontalAlignment = HorizontalAlignment.Stretch
			border1.VerticalAlignment = VerticalAlignment.Stretch
			border1.BorderThickness = Thickness(0, 0, 0, 1)
			border1.BorderBrush = solidColorBrush1

			webBrowser = WebBrowser()
			webBrowser.HorizontalAlignment = HorizontalAlignment.Stretch
			webBrowser.VerticalAlignment = VerticalAlignment.Stretch
			webBrowser.Width = 640
			webBrowser.Height = 480
			webBrowser.Navigated += onWebBrowserNavigated
			
			border1.Child = webBrowser
			stackPanel.Children.Add(border1)

			def onWindowLoaded(sender, args):
				sb = StringBuilder()
				d = Dictionary[String, String]()
				d.Add("client_id", appId)
				d.Add("redirect_uri", "https://www.facebook.com/connect/login_success.html")
				d.Add("scope", "read_stream, publish_stream")
				d.Add("display", "popup")

				for kvp in d:
					if sb.Length > 0:
						sb.Append('&')

					sb.AppendFormat("{0}={1}", kvp.Key, urlEncode(kvp.Value))

				webBrowser.Navigate(Uri(String.Concat("https://www.facebook.com/dialog/oauth?", sb.ToString())))

			def onCloseClick(source, args):
				window.Close()

			solidColorBrush2 = SolidColorBrush(Colors.White)
			solidColorBrush2.Opacity = 0.5

			if solidColorBrush2.CanFreeze:
				solidColorBrush2.Freeze()

			border2 = Border()
			border2.HorizontalAlignment = HorizontalAlignment.Stretch
			border2.VerticalAlignment = VerticalAlignment.Stretch
			border2.BorderThickness = Thickness(0, 1, 0, 0)
			border2.BorderBrush = solidColorBrush2

			closeButton = Button()
			closeButton.HorizontalAlignment = HorizontalAlignment.Right
			closeButton.VerticalAlignment = VerticalAlignment.Center
			closeButton.Margin = Thickness(10, 10, 10, 10)
			closeButton.Padding = Thickness(10, 2, 10, 2)
			closeButton.IsDefault = True

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				closeButton.Content = "閉じる"
			else:
				closeButton.Content = "Close"

			closeButton.Click += onCloseClick

			border2.Child = closeButton
			stackPanel.Children.Add(border2)
			
			window.Owner = Application.Current.MainWindow
			window.Title = Application.Current.MainWindow.Title
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen
			window.ResizeMode = ResizeMode.NoResize
			window.SizeToContent = SizeToContent.WidthAndHeight
			window.Background = SystemColors.ControlBrush
			window.Content = stackPanel
			window.Loaded += onWindowLoaded
			window.Show()

		logInMenuItem = MenuItem()

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			logInMenuItem.Header = "ログイン..."
		else:
			logInMenuItem.Header = "Log In..."

		logInMenuItem.Click += onLogInClick

		menuItem.Items.Add(logInMenuItem)

	else:
		sb = StringBuilder()

		for character in Script.Instance.Characters:
			if sb.Length > 0:
				sb.Append("/")

			sb.Append(character.Name)

		def onMouseUp(sender, mbea):
			mbea.Handled = True

		def onKeyDown(sender, kea):
			kea.Handled = True

		postMenuItem = MenuItem()
		postMenuItem.KeyDown += onKeyDown

		stackPanel = StackPanel()
		stackPanel.HorizontalAlignment = HorizontalAlignment.Left
		stackPanel.VerticalAlignment = VerticalAlignment.Top
		stackPanel.Orientation = Orientation.Horizontal
	
		comboBox = ComboBox()
		comboBox.IsEditable = True
		comboBox.Width = 240
		comboBox.MouseUp += onMouseUp
	
		stack = Stack[String]()
	
		for window in Application.Current.Windows:
			if clr.GetClrType(Agent).IsInstanceOfType(window):
				for message in window.Balloon.Messages:
					stack.Push(String.Format("\"{0}: {1}\"", sb.ToString(), Regex.Replace(message.Text, Environment.NewLine, String.Empty, RegexOptions.CultureInvariant)))

		while stack.Count > 0:
			comboBoxItem = ComboBoxItem()
			comboBoxItem.Content = stack.Pop()
			comboBox.Items.Add(comboBoxItem)

			if comboBox.SelectedItem is None:
				comboBox.SelectedItem = comboBoxItem
	
		checkBox = CheckBox()
		checkBox.Margin = Thickness(10, 0, 0, 0)
		checkBox.Padding = Thickness(5, 0, 5, 0)
		checkBox.HorizontalAlignment = HorizontalAlignment.Left
		checkBox.VerticalAlignment = VerticalAlignment.Center

		def onPostClick(source, rea):
			if not String.IsNullOrEmpty(comboBox.Text):
				fileName = None

				if checkBox.IsChecked:
					currentDirectory = Directory.GetCurrentDirectory()

					openFileDialog = OpenFileDialog()
					openFileDialog.Multiselect = False

					if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
						openFileDialog.Filter = "画像ファイル (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
					else:
						openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"

					if openFileDialog.ShowDialog() == True:
						fileName = openFileDialog.FileName

					Directory.SetCurrentDirectory(currentDirectory)

				post(comboBox.Text, fileName)

		postButton = Button()
		postButton.Margin = Thickness(10, 0, 0, 0)
		postButton.Padding = Thickness(5, 0, 5, 0)
		postButton.IsDefault = True

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			checkBox.Content = "画像を添付"
			postButton.Content = "投稿"
		else:
			checkBox.Content = "Attach"
			postButton.Content = "Post"
		
		postButton.Click += onPostClick

		stackPanel.Children.Add(comboBox)
		stackPanel.Children.Add(checkBox)
		stackPanel.Children.Add(postButton)
		postMenuItem.Header = stackPanel
		menuItem.Items.Add(postMenuItem)

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

def onKeyDown(s, e):
	e.Handled = False

def onStart(s, e):
	global timer, menuItem, separator
	
	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				window.ContextMenu.AddHandler(Button.KeyDownEvent, RoutedEventHandler(onKeyDown), True)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)

	timer.Start()

def onStop(s, e):
	global timer
	
	timer.Stop()

dateTime = DateTime.Now - TimeSpan(12, 0, 0)
menuItem = MenuItem()
menuItem.Header = "Facebook"
separator = None
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop