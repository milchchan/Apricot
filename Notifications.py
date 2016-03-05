# -*- coding: utf-8 -*-
# Notifications.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, UInt32, Double, Char, String, StringComparison, Uri, DateTime, Array, BitConverter, Convert, Action, Func
from System.Collections.Generic import List, Dictionary
from System.IO import Stream, StreamReader
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, NumberStyles
from System.Text import StringBuilder, Encoding
from System.Threading.Tasks import Task, TaskContinuationOptions, TaskScheduler
from System.Net import HttpListener, HttpListenerContext, WebRequestMethods, HttpStatusCode
from System.Windows import Application
from Apricot import Script, Entry, Sequence

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

def onStartup(sender, args):
	global httpListener

	if HttpListener.IsSupported:
		context = TaskScheduler.FromCurrentSynchronizationContext()

		def onCompleted(task):
			if task.Result is not None and task.Result.Count > 0:
				Script.Instance.Alert(task.Result)

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
			
		def onDispatch(task):
			global httpListener

			if task.Exception is None:
				httpListener.GetContextAsync().ContinueWith[List[Entry]](Func[Task[HttpListenerContext], List[Entry]](onDispatch), TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[List[Entry]]](onCompleted), context)

				try:
					if task.Result.Request.HttpMethod.Equals(WebRequestMethods.Http.Post) and task.Result.Request.Url.AbsolutePath.Equals("/alert"):
						if task.Result.Request.ContentType.Equals("application/json"):
							stream = None
							streamReader = None
					
							try:
								stream = task.Result.Request.InputStream
								streamReader = StreamReader(stream)
								jsonArray = JsonDecoder.decode(streamReader.ReadToEnd())

								if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
									entryList = List[Entry]()

									for obj in jsonArray:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											entry = Entry()

											if obj.ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["resource"]):
												entry.Resource = Uri(obj["resource"])

											if obj.ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["title"]):
												entry.Title = obj["title"]

											if obj.ContainsKey("description") and clr.GetClrType(String).IsInstanceOfType(obj["description"]):
												entry.Description = obj["description"]
										
											if obj.ContainsKey("author") and clr.GetClrType(String).IsInstanceOfType(obj["author"]):
												entry.Author = obj["author"]

											if obj.ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["created"]):
												entry.Created = DateTime.Parse(obj["created"])

											if obj.ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["modified"]):
												entry.Modified = DateTime.Parse(obj["modified"])

											if obj.ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["image"]):
												entry.Image = Uri(obj["image"])

											if obj.ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["tags"]):
												for o in obj["tags"]:
													if clr.GetClrType(String).IsInstanceOfType(o):
														entry.Tags.Add(o)

											entryList.Add(entry)

										else:
											task.Result.Response.StatusCode = Convert.ToInt32(HttpStatusCode.BadRequest)
												
											return None

									return entryList

								else:
									task.Result.Response.StatusCode = Convert.ToInt32(HttpStatusCode.BadRequest)
				
							finally:
								if streamReader is not None:
									streamReader.Close()

								if stream is not None:
									stream.Close()

						else:
							task.Result.Response.StatusCode = Convert.ToInt32(HttpStatusCode.UnsupportedMediaType)

					else:
						task.Result.Response.StatusCode = Convert.ToInt32(HttpStatusCode.Forbidden)
				
				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

				finally:
					task.Result.Response.Close()

			return None
		
		try:
			httpListener = HttpListener()
			httpListener.Prefixes.Add(String.Format("http://localhost:{0}/", UInt32.Parse("B0B", NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture))) # localhost:2827
			httpListener.Start()
			httpListener.GetContextAsync().ContinueWith[List[Entry]](Func[Task[HttpListenerContext], List[Entry]](onDispatch), TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[List[Entry]]](onCompleted), context)
		
		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

def onExit(sender, args):
	global sessionEnding, httpListener

	if not sessionEnding and httpListener is not None:
		httpListener.Close()

def onSessionEnding(sender, args):
	global sessionEnding, httpListener

	if httpListener is not None:
		httpListener.Close()

	sessionEnding = True

sessionEnding = False
httpListener = None
Application.Current.Startup += onStartup
Application.Current.Exit += onExit
Application.Current.SessionEnding += onSessionEnding