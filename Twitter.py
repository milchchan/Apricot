# -*- coding: utf-8 -*-
# Twitter.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, Byte, Boolean, UInt32, Double, Char, String, Uri, DateTime, TimeSpan, Array, Convert, Random, Environment, StringComparison, Guid, Math, BitConverter, Action
from System.IO import Stream, FileStream, StreamReader, StreamWriter, Path, Directory, File, DirectoryInfo, FileInfo, FileMode, FileAccess, FileShare
from System.Collections.Generic import List, Queue, Stack, Dictionary, SortedDictionary, KeyValuePair, HashSet
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Diagnostics import Process, Trace
from System.Globalization import CultureInfo, NumberStyles, DateTimeStyles
from System.Reflection import Assembly
from System.Security.Cryptography import HMACSHA1
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, Match, Capture, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Net import ServicePointManager, WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, WebRequestMethods, HttpRequestHeader
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Thickness, RoutedEventHandler, SystemColors
from System.Windows.Controls import MenuItem, Separator, StackPanel, Border, Label, TextBox, ComboBox, ComboBoxItem, Button, CheckBox, WebBrowser, Orientation
from System.Windows.Media import Colors, SolidColorBrush, Stretch
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System.Xml import XmlDocument
from Microsoft.Win32 import OpenFileDialog
from Apricot import Agent, Script, Entry, Message, Character, Word, Sequence

username = ""
password = ""
oauthToken = None
oauthTokenSecret = None
consumerKey = "5Y4BpcdcEwkZeIRqIMdJyg"
consumerSecret = "AMsengQQpxsvKEnuEX8oagKTLjrcujY7hBzVKlo72O0"
useTwitpic = False

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
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
	bodyStringBuilder = StringBuilder()
	webClient = WebClient()
	dictionary = Dictionary[String, String]()
	request = WebRequest.Create("https://api.twitter.com/1.1/statuses/home_timeline.json")
	entryList = List[Entry]()
	context = TaskScheduler.FromCurrentSynchronizationContext()

	if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("x_auth_mode", "client_auth")
		sortedDictionary.Add("x_auth_username", username)
		sortedDictionary.Add("x_auth_password", password)

		for kvp in sortedDictionary:
			if kvp.Key.StartsWith("x_auth_", StringComparison.Ordinal):
				if bodyStringBuilder.Length > 0:
					bodyStringBuilder.Append('&')

				bodyStringBuilder.Append(kvp.Key)
				bodyStringBuilder.Append('=')
				bodyStringBuilder.Append(kvp.Value)

		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&")))
		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://api.twitter.com/oauth/access_token"), sortedDictionary))))

		sortedDictionary.Add("oauth_signature", signature)
		
		webClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
		webClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")
		
	def onAuth():
		try:
			if NetworkInterface.GetIsNetworkAvailable() and (String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret)) and bodyStringBuilder.Length > 0:
				bytes = webClient.UploadData(Uri("https://api.twitter.com/oauth/access_token"), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(bodyStringBuilder.ToString()))

				for s in Encoding.ASCII.GetString(bytes).Split('&'):
					index = s.IndexOf('=')

					if index == -1:
						dictionary.Add(s, String.Empty)
					else:
						dictionary.Add(s.Substring(0, index), s.Substring(index + 1))

		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

	def onReady(task):
		global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
		for kvp in dictionary:
			if kvp.Key.Equals("oauth_token"):
				oauthToken = kvp.Value
			elif kvp.Key.Equals("oauth_token_secret"):
				oauthTokenSecret = kvp.Value

		if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/statuses/home_timeline.json"), sortedDictionary))))
			
			sortedDictionary.Add("oauth_signature", signature)

			request.Method = WebRequestMethods.Http.Get
			request.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
			request.ContentType = "application/json"

			return True

		return False

	def onUpdate(task):
		if NetworkInterface.GetIsNetworkAvailable() and task.Result:
			try:
				response = None
				stream = None
				streamReader = None

				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					streamReader = StreamReader(stream)
					json = JsonDecoder.decode(streamReader.ReadToEnd())

					if json is not None:
						if clr.GetClrType(Array).IsInstanceOfType(json):
							for obj in json:
								if obj is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
										entry = Entry()
										idStr = String.Empty
										imageUriList = List[Uri]()
										screenName = String.Empty

										if obj.ContainsKey("id_str"):
											if obj["id_str"] is not None:
												idStr = obj["id_str"]

										if obj.ContainsKey("text"):
											if obj["text"] is not None:
												entry.Title = Regex.Replace(obj["text"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
											
										if obj.ContainsKey("entities"):
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entities"]):
												if obj["entities"].ContainsKey("media"):
													if clr.GetClrType(Array).IsInstanceOfType(obj["entities"]["media"]):
														for o in obj["entities"]["media"]:
															if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(o):
																if o.ContainsKey("media_url_https") and o.ContainsKey("type"):
																	if o["type"].Equals("photo"):
																		imageUriList.Add(Uri(o["media_url_https"]))

										if obj.ContainsKey("created_at"):
											if obj["created_at"] is not None:
												entry.Created = entry.Modified = DateTime.ParseExact(obj["created_at"], "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)

										if obj.ContainsKey("user"):
											if obj["user"] is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["user"]):
													user = obj["user"]

													if user is not None:
														if user.ContainsKey("screen_name"):
															if user["screen_name"] is not None:
																screenName = user["screen_name"]

														if user.ContainsKey("profile_image_url_https"):
															if user["profile_image_url_https"] is not None:
																imageUriList.Insert(0, Uri(user["profile_image_url_https"]))
																
										if imageUriList.Count == 1:
											entry.Image = imageUriList[0]

										elif imageUriList.Count > 1:
											sb = StringBuilder()

											for uri in imageUriList:
												sb.AppendFormat("<img src=\"{0}\" />", uri.ToString())

											entry.Description = sb.ToString()

										entry.Resource = Uri(String.Concat("https://twitter.com/", screenName, "/statuses/", idStr))
										entry.Author = screenName
										entryList.Add(entry)

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

	def onCompleted(task):
		global dateTime

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

	Task.Factory.StartNew(onAuth, TaskCreationOptions.LongRunning).ContinueWith[Boolean](onReady, context).ContinueWith(Action[Task[Boolean]](onUpdate), TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

def search(query):
	global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
	if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
		entryList = List[Entry]()

		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("oauth_token", oauthToken)
		sortedDictionary.Add("q", urlEncode(query))

		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode(query))), sortedDictionary))))
		
		sortedDictionary.Add("oauth_signature", signature)

		request = WebRequest.Create(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode(query)))
		request.Method = WebRequestMethods.Http.Get
		request.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
		request.ContentType = "application/json"

		def onSearch():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					response = None
					stream = None
					streamReader = None

					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = JsonDecoder.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
								if json.ContainsKey("statuses"):
									if clr.GetClrType(Array).IsInstanceOfType(json["statuses"]):
										for status in json["statuses"]:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(status):
												
												entry = Entry()
												idStr = String.Empty
												screenName = String.Empty

												if status.ContainsKey("id_str"):
													if status["id_str"] is not None:
														idStr = status["id_str"]

												if status.ContainsKey("text"):
													if status["text"] is not None:
														text = Regex.Replace(status["text"], "[\r\n]|\\#apricotan", String.Empty, RegexOptions.CultureInvariant).Trim()

														if text.StartsWith("\"", StringComparison.Ordinal) and text.EndsWith("\"", StringComparison.Ordinal):
															text = text.Trim("\"".ToCharArray())

														entry.Title = text

												if status.ContainsKey("created_at"):
													if status["created_at"] is not None:
														entry.Created = entry.Modified = DateTime.ParseExact(status["created_at"], "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)

												if status.ContainsKey("user"):
													if status["user"] is not None:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(status["user"]):
															user = status["user"]

															if user is not None:
																if user.ContainsKey("screen_name"):
																	if user["screen_name"] is not None:
																		screenName = user["screen_name"]

																if user.ContainsKey("profile_image_url_https"):
																	if user["profile_image_url_https"] is not None:
																		entry.Image = Uri(user["profile_image_url_https"])

												if screenName.StartsWith("apricotan", StringComparison.Ordinal):
													continue
												
												entry.Resource = Uri(String.Concat("https://twitter.com/", screenName, "/statuses/", idStr))
												entry.Author = screenName
												entryList.Add(entry)

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

		def onCompleted(task):
			global recentEntryList

			if entryList.Count > 0:
				recentEntryList.Clear()
				recentEntryList.AddRange(entryList)

		Task.Factory.StartNew(onSearch, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

def onTick(timer, e):
	global username, password, oauthToken, oauthTokenSecret

	if (not String.IsNullOrEmpty(username) and not String.IsNullOrEmpty(password)) or (not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret)):
		update()

	search("#apricotan")

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(3)
	timer.Start()

def post(text, filename):
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret, useTwitpic
	
	isLoadable = File.Exists(filename)
	bodyStringBuilder = StringBuilder()
	authWebClient = WebClient()
	dictionary = Dictionary[String, String]()
	byteList = List[Byte]()
	timelineRequest = WebRequest.Create("https://api.twitter.com/1.1/statuses/home_timeline.json")
	entryList = List[Entry]()
	context = TaskScheduler.FromCurrentSynchronizationContext()

	if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("x_auth_mode", "client_auth")
		sortedDictionary.Add("x_auth_username", username)
		sortedDictionary.Add("x_auth_password", password)

		for kvp in sortedDictionary:
			if kvp.Key.StartsWith("x_auth_", StringComparison.Ordinal):
				if bodyStringBuilder.Length > 0:
					bodyStringBuilder.Append('&')

				bodyStringBuilder.Append(kvp.Key)
				bodyStringBuilder.Append('=')
				bodyStringBuilder.Append(kvp.Value)

		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&")))
		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://api.twitter.com/oauth/access_token"), sortedDictionary))))

		sortedDictionary.Add("oauth_signature", signature)

		authWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
		authWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")

	def onPrepare():
		try:
			if NetworkInterface.GetIsNetworkAvailable() and (String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret)) and bodyStringBuilder.Length > 0:
				bytes = authWebClient.UploadData(Uri("https://api.twitter.com/oauth/access_token"), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(bodyStringBuilder.ToString()))

				for s in Encoding.ASCII.GetString(bytes).Split('&'):
					index = s.IndexOf('=')

					if index == -1:
						dictionary.Add(s, String.Empty)
					else:
						dictionary.Add(s.Substring(0, index), s.Substring(index + 1))
				
			if isLoadable:
				byteList.AddRange(File.ReadAllBytes(filename))

		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

	def onCompleted(task):
		global dateTime

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

	if useTwitpic:
		uploadRequest = WebRequest.Create("http://api.twitpic.com/2/upload.xml")
		sb = StringBuilder()
		updateWebClient = WebClient()
		
		def onReady(task):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
			for kvp in dictionary:
				if kvp.Key.Equals("oauth_token"):
					oauthToken = kvp.Value
				elif kvp.Key.Equals("oauth_token_secret"):
					oauthTokenSecret = kvp.Value

			if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret) and isLoadable:
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

				sortedDictionary.Add("realm", "http://api.twitter.com/")
				sortedDictionary.Add("oauth_signature", signature)

				twitpicApiKey = "61d167d8728a76fd937d363767db097b"
				boundary = Guid.NewGuid().ToString()
				encoding = "iso-8859-1"
				header = String.Format("--{0}", boundary)
				footer = String.Format("--{0}--", boundary)

				contents = StringBuilder()
				contents.AppendLine(header)

				extension = Path.GetExtension(filename)
				fileHeader = String.Format("Content-Disposition: file; name=\"{0}\"; filename=\"{1}\"", "media", filename)
				fileData = Encoding.GetEncoding(encoding).GetString(byteList.ToArray())

				contents.AppendLine(fileHeader)
	
				if extension.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) or extension.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase):
					contents.AppendLine(String.Format("Content-Type: {0}", "image/jpeg"))
				elif extension.EndsWith(".png", StringComparison.OrdinalIgnoreCase):
					contents.AppendLine(String.Format("Content-Type: {0}", "image/png"))

				contents.AppendLine()
				contents.AppendLine(fileData)
				contents.AppendLine(header)
				contents.AppendLine(String.Format("Content-Disposition: form-data; name=\"{0}\"", "key"))
				contents.AppendLine()
				contents.AppendLine(twitpicApiKey)

				if not String.IsNullOrEmpty(text):
					contents.AppendLine(header)
					contents.AppendLine(String.Format("Content-Disposition: form-data; name=\"{0}\"", "message"))
					contents.AppendLine()
					contents.AppendLine(Encoding.GetEncoding(encoding).GetString(Encoding.UTF8.GetBytes(text)))

				contents.AppendLine(footer)

				byteList.Clear()
				byteList.AddRange(Encoding.GetEncoding(encoding).GetBytes(contents.ToString()))
			
				uploadRequest.PreAuthenticate = True
				uploadRequest.ContentType = String.Format("multipart/form-data; boundary={0}", boundary)
				uploadRequest.Method = WebRequestMethods.Http.Post
				uploadRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
				uploadRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
				uploadRequest.ContentLength = byteList.Count

				return True

			return False

		def onUpload(task):
			if NetworkInterface.GetIsNetworkAvailable() and isLoadable and task.Result:
				try:
					requestStream = None
					response = None
					responseStream = None

					try:
						requestStream = uploadRequest.GetRequestStream()
						requestStream.Write(byteList.ToArray(), 0, byteList.Count)
						response = uploadRequest.GetResponse()

						responseStream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(responseStream)
			
						dictionary.Clear()

						for xmlNode in doc.SelectNodes("/image/url"):
							if not dictionary.ContainsKey("/image/url"):
								dictionary.Add("/image/url", xmlNode.InnerText)
				
					finally:
						if responseStream is not None:
							responseStream.Close()

						if response is not None:
							response.Close()

						if requestStream is not None:
							requestStream.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

		def onUploaded(task):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret

			if dictionary.ContainsKey("/image/url"):
				hashtag = "#apricotan"
				index = text.IndexOf(hashtag)

				if index >= 0:
					sb.AppendFormat("{0} {1} #apricotan", text.Remove(index, hashtag.Length).Trim(), dictionary["/image/url"])
				else:
					sb.AppendFormat("{0} {1}", text, dictionary["/image/url"])
			
			else:
				sb.Append(text)

			if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)
				sortedDictionary.Add("status", urlEncode(sb.ToString()))

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri(String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(sb.ToString()))), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)
				sortedDictionary.Remove("status")

				updateWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				updateWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/json")

				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/statuses/home_timeline.json"), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)
		
				timelineRequest.Method = WebRequestMethods.Http.Get
				timelineRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				timelineRequest.ContentType = "application/json"

				return True

			return False
		
		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable() and task.Result:
				try:
					updateWebClient.UploadData(Uri(String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(sb.ToString()))), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Empty))
				
					response = None
					stream = None
					streamReader = None

					try:
						response = timelineRequest.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = JsonDecoder.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											entry = Entry()
											idStr = String.Empty
											imageUriList = List[Uri]()
											screenName = String.Empty

											if obj.ContainsKey("id_str"):
												if obj["id_str"] is not None:
													idStr = obj["id_str"]

											if obj.ContainsKey("text"):
												if obj["text"] is not None:
													entry.Title = Regex.Replace(obj["text"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
											
											if obj.ContainsKey("entities"):
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entities"]):
													if obj["entities"].ContainsKey("media"):
														if clr.GetClrType(Array).IsInstanceOfType(obj["entities"]["media"]):
															for o in obj["entities"]["media"]:
																if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(o):
																	if o.ContainsKey("media_url_https") and o.ContainsKey("type"):
																		if o["type"].Equals("photo"):
																			imageUriList.Add(Uri(o["media_url_https"]))

											if obj.ContainsKey("created_at"):
												if obj["created_at"] is not None:
													entry.Created = entry.Modified = DateTime.ParseExact(obj["created_at"], "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)

											if obj.ContainsKey("user"):
												if obj["user"] is not None:
													if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["user"]):
														user = obj["user"]

														if user is not None:
															if user.ContainsKey("screen_name"):
																if user["screen_name"] is not None:
																	screenName = user["screen_name"]

															if user.ContainsKey("profile_image_url_https"):
																if user["profile_image_url_https"] is not None:
																	imageUriList.Insert(0, Uri(user["profile_image_url_https"]))
										
											if imageUriList.Count == 1:
												entry.Image = imageUriList[0]

											elif imageUriList.Count > 1:
												descriptionStringBuilder = StringBuilder()

												for uri in imageUriList:
													descriptionStringBuilder.AppendFormat("<img src=\"{0}\" />", uri.ToString())

												entry.Description = descriptionStringBuilder.ToString()

											entry.Resource = Uri(String.Concat("https://twitter.com/", screenName, "/statuses/", idStr))
											entry.Author = screenName
											entryList.Add(entry)

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

		Task.Factory.StartNew(onPrepare, TaskCreationOptions.LongRunning).ContinueWith[Boolean](onReady, context).ContinueWith(Action[Task[Boolean]](onUpload), TaskContinuationOptions.LongRunning).ContinueWith[Boolean](onUploaded, context).ContinueWith(Action[Task[Boolean]](onUpdate), TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)
	else:
		updateRequest = WebRequest.Create("https://api.twitter.com/1.1/statuses/update_with_media.json" if isLoadable else String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(text)))
		
		def onReady(task):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
			for kvp in dictionary:
				if kvp.Key.Equals("oauth_token"):
					oauthToken = kvp.Value
				elif kvp.Key.Equals("oauth_token_secret"):
					oauthTokenSecret = kvp.Value

			if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)
				
				if isLoadable:
					boundary = Guid.NewGuid().ToString()
					encoding = "iso-8859-1"
					header = String.Format("--{0}", boundary)
					footer = String.Format("--{0}--", boundary)

					contents = StringBuilder()
					contents.AppendLine(header)
					contents.AppendLine(String.Format("Content-Disposition: form-data; name=\"{0}\"", "status"))
					contents.AppendLine()
					contents.AppendLine(Encoding.GetEncoding(encoding).GetString(Encoding.UTF8.GetBytes(text)))
					
					extension = Path.GetExtension(filename)
					fileHeader = String.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", "media[]", filename)
					fileData = Encoding.GetEncoding(encoding).GetString(byteList.ToArray())

					contents.AppendLine(header)
					contents.AppendLine(fileHeader)
					contents.AppendLine(String.Format("Content-Type: {0}", "application/octet-stream"))
					contents.AppendLine()
					contents.AppendLine(fileData)
					contents.AppendLine(footer)

					byteList.Clear()
					byteList.AddRange(Encoding.GetEncoding(encoding).GetBytes(contents.ToString()))
			
					updateRequest.ContentType = String.Format("multipart/form-data; boundary={0}", boundary)
				else:
					updateRequest.ContentType = "application/json"
					sortedDictionary.Add("status", urlEncode(text))

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://api.twitter.com/1.1/statuses/update_with_media.json" if isLoadable else String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(text))), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				if sortedDictionary.ContainsKey("status"):
					sortedDictionary.Remove("status")

				updateRequest.PreAuthenticate = True
				updateRequest.Method = WebRequestMethods.Http.Post
				updateRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				updateRequest.ContentLength = byteList.Count

				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/statuses/home_timeline.json"), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				timelineRequest.Method = WebRequestMethods.Http.Get
				timelineRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				timelineRequest.ContentType = "application/json"

				return True

			return False

		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable() and task.Result:
				try:
					updateResponse = None
	
					try:
						requestStream = None

						try:
							requestStream = updateRequest.GetRequestStream()
							requestStream.Write(byteList.ToArray(), 0, byteList.Count)

						finally:
							if requestStream is not None:
								requestStream.Close()
					
						updateResponse = updateRequest.GetResponse()

					finally:
						if updateResponse is not None:
							updateResponse.Close()

					response = None
					stream = None
					streamReader = None

					try:
						response = timelineRequest.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = JsonDecoder.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											entry = Entry()
											idStr = String.Empty
											imageUriList = List[Uri]()
											screenName = String.Empty

											if obj.ContainsKey("id_str"):
												if obj["id_str"] is not None:
													idStr = obj["id_str"]

											if obj.ContainsKey("text"):
												if obj["text"] is not None:
													entry.Title = Regex.Replace(obj["text"], "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
											
											if obj.ContainsKey("entities"):
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entities"]):
													if obj["entities"].ContainsKey("media"):
														if clr.GetClrType(Array).IsInstanceOfType(obj["entities"]["media"]):
															for o in obj["entities"]["media"]:
																if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(o):
																	if o.ContainsKey("media_url_https") and o.ContainsKey("type"):
																		if o["type"].Equals("photo"):
																			imageUriList.Add(Uri(o["media_url_https"]))

											if obj.ContainsKey("created_at"):
												if obj["created_at"] is not None:
													entry.Created = entry.Modified = DateTime.ParseExact(obj["created_at"], "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)

											if obj.ContainsKey("user"):
												if obj["user"] is not None:
													if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["user"]):
														user = obj["user"]

														if user is not None:
															if user.ContainsKey("screen_name"):
																if user["screen_name"] is not None:
																	screenName = user["screen_name"]

															if user.ContainsKey("profile_image_url_https"):
																if user["profile_image_url_https"] is not None:
																	imageUriList.Insert(0, Uri(user["profile_image_url_https"]))

											if imageUriList.Count == 1:
												entry.Image = imageUriList[0]

											elif imageUriList.Count > 1:
												descriptionStringBuilder = StringBuilder()

												for uri in imageUriList:
													descriptionStringBuilder.AppendFormat("<img src=\"{0}\" />", uri.ToString())

												entry.Description = descriptionStringBuilder.ToString()

											entry.Resource = Uri(String.Concat("https://twitter.com/", screenName, "/statuses/", idStr))
											entry.Author = screenName
											entryList.Add(entry)

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

		Task.Factory.StartNew(onPrepare, TaskCreationOptions.LongRunning).ContinueWith[Boolean](onReady, context).ContinueWith(Action[Task[Boolean]](onUpdate), TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

def onOpened(s, e):
	global menuItem, username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	menuItem.Items.Clear()

	if (String.IsNullOrEmpty(username) or String.IsNullOrEmpty(password)) and (String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret)):
		def onSignInClick(source, rea):
			dictionary = Dictionary[String, String]()
			context = TaskScheduler.FromCurrentSynchronizationContext()
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_callback", "oob")
			sortedDictionary.Add("oauth_signature", Convert.ToBase64String(HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&"))).ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://api.twitter.com/oauth/request_token"), sortedDictionary)))))
			
			request = WebRequest.Create("https://api.twitter.com/oauth/request_token")
			request.Method = WebRequestMethods.Http.Post
			request.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
			request.ContentType = "application/x-www-form-urlencoded"
			
			def onOAuthRequestToken():
				try:
					if NetworkInterface.GetIsNetworkAvailable():
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream, Encoding.ASCII)

							for s in streamReader.ReadToEnd().Split('&'):
								index = s.IndexOf('=')

								if index == -1:
									dictionary.Add(s, String.Empty)
								else:
									dictionary.Add(s.Substring(0, index), s.Substring(index + 1))

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

			def onOAuthRequestTokenCompleted(task):
				if dictionary.ContainsKey("oauth_token") and dictionary.ContainsKey("oauth_token_secret"):
					window = Window()
					pinTextBox = TextBox()
					
					stackPanel1 = StackPanel()
					stackPanel1.UseLayoutRounding = True
					stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
					stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
					stackPanel1.Orientation = Orientation.Vertical

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
					
					border1.Child = webBrowser
					stackPanel1.Children.Add(border1)

					def onWindowLoaded(sender, args):
						webBrowser.Navigate(Uri(String.Concat("https://api.twitter.com/oauth/authorize?oauth_token=", dictionary["oauth_token"])))

					def onVerifyClick(source, args):
						d = Dictionary[String, String]()
						sd = createCommonParameters(consumerKey)
						sd.Add("oauth_verifier", pinTextBox.Text)
						sd.Add("oauth_token", dictionary["oauth_token"])
						sd.Add("oauth_signature", Convert.ToBase64String(HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", dictionary["oauth_token_secret"]))).ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://api.twitter.com/oauth/access_token"), sortedDictionary)))))
		
						r = WebRequest.Create(String.Concat("https://api.twitter.com/oauth/access_token?oauth_verifier=", pinTextBox.Text))
						r.Method = WebRequestMethods.Http.Post
						r.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sd))
						r.ContentType = "application/x-www-form-urlencoded"

						def onOAuthAccessToken():
							try:
								if NetworkInterface.GetIsNetworkAvailable():
									response = None
									stream = None
									streamReader = None

									try:
										response = r.GetResponse()
										stream = response.GetResponseStream()
										streamReader = StreamReader(stream, Encoding.ASCII)

										for s in streamReader.ReadToEnd().Split('&'):
											index = s.IndexOf('=')

											if index == -1:
												d.Add(s, String.Empty)
											else:
												d.Add(s.Substring(0, index), s.Substring(index + 1))

										if d.ContainsKey("oauth_token") and d.ContainsKey("oauth_token_secret"):
											config = None
											directoryInfo = DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name))
			
											if directoryInfo.Exists:
												fileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location)
		
												for fileInfo in directoryInfo.EnumerateFiles("*.config"):
													if fileName.Equals(Path.GetFileNameWithoutExtension(fileInfo.Name)):
														exeConfigurationFileMap = ExeConfigurationFileMap()
				
														exeConfigurationFileMap.ExeConfigFilename = fileInfo.FullName
														config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
											if config is None:
												config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
												directoryInfo = None

											if config.HasFile:
												if config.AppSettings.Settings["Scripts"] is not None:
													di = DirectoryInfo(config.AppSettings.Settings["Scripts"].Value if directoryInfo is None else Path.Combine(directoryInfo.FullName, config.AppSettings.Settings["Scripts"].Value));
													
													for fileInfo in di.GetFiles("*.py"):
														fs1 = None
														sr = None
														lines = None

														try:
															fs1 = FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)
															sr = StreamReader(fs1, UTF8Encoding(False), True)
															lines = sr.ReadToEnd()

														finally:
															if sr is not None:
																sr.Close()

															if fs1 is not None:
																fs1.Close()

														if lines is not None:
															if Regex.IsMatch(lines, "\\#\\s*Twitter.py", RegexOptions.CultureInvariant):
																lines = Regex.Replace(lines, "oauthToken\\s*=\\s*None", String.Format("oauthToken = \"{0}\"", d["oauth_token"]), RegexOptions.CultureInvariant)
																lines = Regex.Replace(lines, "oauthTokenSecret\\s*=\\s*None", String.Format("oauthTokenSecret = \"{0}\"", d["oauth_token_secret"]), RegexOptions.CultureInvariant)
																fs2 = None
																sw = None

																try:
																	fs2 = FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)
																	sw = StreamWriter(fs2, UTF8Encoding(False))
																	sw.Write(lines)

																finally:
																	if sw is not None:
																		sw.Close()

																	if fs2 is not None:
																		fs2.Close()

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

						def onOAuthAccessTokenCompleted(task):
							global oauthToken, oauthTokenSecret
	
							for kvp in d:
								if kvp.Key.Equals("oauth_token"):
									oauthToken = kvp.Value
								elif kvp.Key.Equals("oauth_token_secret"):
									oauthTokenSecret = kvp.Value

						Task.Factory.StartNew(onOAuthAccessToken, TaskCreationOptions.LongRunning).ContinueWith(onOAuthAccessTokenCompleted, context)

						window.Close()

					def onCancelClick(source, args):
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

					stackPanel2 = StackPanel()
					stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
					stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
					stackPanel2.Orientation = Orientation.Horizontal
					
					pinLabel = Label()
					pinLabel.HorizontalAlignment = HorizontalAlignment.Right
					pinLabel.VerticalAlignment = VerticalAlignment.Center
					pinLabel.Foreground = SystemColors.ControlTextBrush
					pinLabel.Margin = Thickness(0, 10, 0, 10)
					pinLabel.Content = "PIN"
					
					pinTextBox.HorizontalAlignment = HorizontalAlignment.Right
					pinTextBox.VerticalAlignment = VerticalAlignment.Center
					pinTextBox.Margin = Thickness(0, 10, 0, 10)
					pinTextBox.Width = 100
					
					verifyButton = Button()
					verifyButton.HorizontalAlignment = HorizontalAlignment.Right
					verifyButton.VerticalAlignment = VerticalAlignment.Center
					verifyButton.Margin = Thickness(10, 10, 5, 10)
					verifyButton.Padding = Thickness(10, 2, 10, 2)
					verifyButton.IsDefault = True

					cancelButton = Button()
					cancelButton.HorizontalAlignment = HorizontalAlignment.Right
					cancelButton.VerticalAlignment = VerticalAlignment.Center
					cancelButton.Margin = Thickness(5, 10, 10, 10)
					cancelButton.Padding = Thickness(10, 2, 10, 2)

					if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
						verifyButton.Content = "認証"
						cancelButton.Content = "キャンセル"
					else:
						verifyButton.Content = "Verify"
						cancelButton.Content = "Cancel"

					verifyButton.Click += onVerifyClick
					cancelButton.Click += onCancelClick

					stackPanel2.Children.Add(pinLabel)
					stackPanel2.Children.Add(pinTextBox)
					stackPanel2.Children.Add(verifyButton)
					stackPanel2.Children.Add(cancelButton)
					border2.Child = stackPanel2
					stackPanel1.Children.Add(border2)
			
					window.Owner = Application.Current.MainWindow
					window.Title = Application.Current.MainWindow.Title
					window.WindowStartupLocation = WindowStartupLocation.CenterScreen
					window.ResizeMode = ResizeMode.NoResize
					window.SizeToContent = SizeToContent.WidthAndHeight
					window.Background = SystemColors.ControlBrush
					window.Content = stackPanel1
					window.Loaded += onWindowLoaded
					window.Show()

			Task.Factory.StartNew(onOAuthRequestToken, TaskCreationOptions.LongRunning).ContinueWith(onOAuthRequestTokenCompleted, context)

		signInMenuItem = MenuItem()

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			signInMenuItem.Header = "サインイン..."
		else:
			signInMenuItem.Header = "Sign in..."

		signInMenuItem.Click += onSignInClick

		menuItem.Items.Add(signInMenuItem)

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

		tweetMenuItem = MenuItem()
		tweetMenuItem.KeyDown += onKeyDown

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
					stack.Push(String.Format("\"{0}: {1}\" #apricotan", sb.ToString(), Regex.Replace(message.Text, Environment.NewLine, String.Empty, RegexOptions.CultureInvariant)))

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

		def onTweetClick(source, rea):
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

		tweetButton = Button()
		tweetButton.Margin = Thickness(10, 0, 0, 0)
		tweetButton.Padding = Thickness(5, 0, 5, 0)
		tweetButton.IsDefault = True

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			checkBox.Content = "画像を添付"
			tweetButton.Content = "投稿"
		else:
			checkBox.Content = "Attach"
			tweetButton.Content = "Tweet"
		
		tweetButton.Click += onTweetClick

		stackPanel.Children.Add(comboBox)
		stackPanel.Children.Add(checkBox)
		stackPanel.Children.Add(tweetButton)
		tweetMenuItem.Header = stackPanel
		menuItem.Items.Add(tweetMenuItem)

	hashSet = HashSet[Uri]()
	queue = Queue[Entry](recentEntryList)

	if recentEntryList.Count > 0:
		menuItem.Items.Add(Separator())

	while queue.Count > 0 and menuItem.Items.Count < 10:
		entry = queue.Dequeue()
			
		if not hashSet.Contains(entry.Resource):
			hashSet.Add(entry.Resource)

			mi = MenuItem()
			mi.Header = entry.Title
			mi.Tag = entry.Resource

			def onStatusClick(sender, args):
				def onStart(state):
					Process.Start(state)

				Task.Factory.StartNew(onStart, sender.Tag.ToString())

			mi.Click += onStatusClick

			menuItem.Items.Add(mi)

def createCommonParameters(consumerKey):
	sortedDictionary = SortedDictionary[String, String]()

	sortedDictionary.Add("oauth_consumer_key", consumerKey)
	sortedDictionary.Add("oauth_nonce", generateNonce())
	sortedDictionary.Add("oauth_signature_method", "HMAC-SHA1")
	sortedDictionary.Add("oauth_timestamp", generateTimestamp())
	sortedDictionary.Add("oauth_version", "1.0")

	return sortedDictionary

def createSignatureBase(method, uri, sortedDictionary):
	parametersStringBuilder = StringBuilder()
	signatureBaseStringBuilder = StringBuilder()
    
	for kvp in sortedDictionary:
		if parametersStringBuilder.Length > 0:
			parametersStringBuilder.Append('&')

		parametersStringBuilder.Append(kvp.Key)
		parametersStringBuilder.Append('=')
		parametersStringBuilder.Append(kvp.Value)

	signatureBaseStringBuilder.Append(method)
	signatureBaseStringBuilder.Append('&')
	signatureBaseStringBuilder.Append(urlEncode(String.Format("{0}://{1}{2}", uri.Scheme, uri.Host, uri.AbsolutePath)))
	signatureBaseStringBuilder.Append('&')
	signatureBaseStringBuilder.Append(urlEncode(parametersStringBuilder.ToString()))

	return signatureBaseStringBuilder.ToString()

def createHttpAuthorizationHeader(sortedDictionary):
	headerStringBuilder = StringBuilder("OAuth ")
	queue = Queue[KeyValuePair[String, String]]()

	for kvp in sortedDictionary:
		if kvp.Key.StartsWith("oauth_", StringComparison.Ordinal):
			queue.Enqueue(kvp)

	while queue.Count > 0:
		kvp = queue.Dequeue()

		headerStringBuilder.AppendFormat("{0}=\"{1}\"", kvp.Key, urlEncode(kvp.Value))

		if queue.Count > 0:
			headerStringBuilder.Append(", ")

	return headerStringBuilder.ToString()

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

def generateNonce():
	random = Random(Environment.TickCount)
	letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
	sb = StringBuilder(64)

	for i in range(0, 64):
		sb.Append(letters[random.Next(letters.Length)])

	return sb.ToString()

def generateTimestamp():
	return Convert.ToInt64((DateTime.UtcNow - DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds).ToString()

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

ServicePointManager.Expect100Continue = False
dateTime = DateTime.Now - TimeSpan(12, 0, 0)
recentEntryList = List[Entry]()
menuItem = MenuItem()
menuItem.Header = "Twitter"
separator = None
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop