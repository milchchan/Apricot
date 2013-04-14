# -*- coding: utf-8 -*-
# nazr.in.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot") 

from System import Object, UInt32, Double, Char, String, Uri, UriKind, Array, Convert, BitConverter, Action
from System.Collections.Generic import List, Stack, Dictionary
from System.IO import Stream, StreamReader
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, NumberStyles
from System.Text import StringBuilder, Encoding
from System.Text.RegularExpressions import Regex, Match, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, Clipboard, DataObject, DataFormats
from System.Windows.Controls import MenuItem, Separator
from Apricot import Agent, Script

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

def onOpened(s, e):
	global menuItem, uriList

	menuItem.Items.Clear()

	if uriList.Count > 0:
		maxLength = 50
		stack = Stack[Uri](uriList)

		while stack.Count > 0:
			uri = stack.Pop()
			
			def onClick(sender, args):
				def onUpdate():
					shortenedUri = None

					if NetworkInterface.GetIsNetworkAvailable():
						try:
							request = WebRequest.Create(Uri(String.Concat("http://nazr.in/api/shorten.json?url=", urlEncode(uri.ToString()))))
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
										if json.ContainsKey("url"):
											shortenedUri = Uri(json["url"])

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

					return shortenedUri

				def onCompleted(task):
					if task.Result is not None:
						Clipboard.SetDataObject(DataObject(DataFormats.UnicodeText, task.Result.ToString(), True))

				Task.Factory.StartNew[Uri](onUpdate, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[Uri]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

			header = uri.ToString()
			
			if header.Length > maxLength:
				header = String.Concat(header.Remove(maxLength, header.Length - maxLength), "...")

			mi = MenuItem()
			mi.Click += onClick
			mi.Header = header
			mi.Tag = uri

			menuItem.Items.Add(mi)

	else:
		mi = MenuItem()
		mi.IsEnabled = False
		mi.Header = "N/A"

		menuItem.Items.Add(mi)

def onDrawClipboard(s, e):
	global uriList

	if s == Application.Current.MainWindow:
		text = tryGetClipboardText()

		if text is not None:
			match = Regex.Match(text, "(\\S+)://([^:/]+)(:(\\d+))?(/[^#\\s]*)(#(\\S+))?", RegexOptions.CultureInvariant);

			if match.Success:
				success, uri = Uri.TryCreate(match.Value, UriKind.RelativeOrAbsolute)

				if success:
					if uri.IsAbsoluteUri and uriList.Contains(uri) == False:
						if not uri.Host.Equals("nazr.in"):
							uriList.Add(uri)

							if uriList.Count > 10:
								uriList.RemoveRange(0, uriList.Count - 10)
							
def tryGetClipboardText():
	try:
		return Clipboard.GetText() if Clipboard.ContainsText() else None

	except:
		return None

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
	global menuItem, separator
	
	for window in Application.Current.Windows:
		if clr.GetClrType(Agent).IsInstanceOfType(window):
			window.DrawClipboard += onDrawClipboard

		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)

def onStop(s, e):
	for window in Application.Current.Windows:
		if clr.GetClrType(Agent).IsInstanceOfType(window):
			window.DrawClipboard -= onDrawClipboard

uriList = List[Uri]()
menuItem = MenuItem()
menuItem.Header = "nazr.in"
separator = None
Script.Instance.Start += onStart
Script.Instance.Stop += onStop