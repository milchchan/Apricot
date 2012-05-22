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

from System import Object, Byte, UInt32, Double, Char, String, Uri, DateTime, TimeSpan, Array, Convert, Random, Environment, StringComparison, Guid, Math, BitConverter, Action
from System.IO import Stream, FileStream, StreamReader, StreamWriter, Path, Directory, File, DirectoryInfo, FileInfo, FileMode, FileAccess, FileShare
from System.Collections.Generic import List, LinkedList, Queue, Stack, Dictionary, SortedDictionary, KeyValuePair, HashSet
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Diagnostics import Process, Trace
from System.Globalization import CultureInfo, NumberStyles, DateTimeStyles
from System.Linq import Enumerable
from System.Reflection import Assembly
from System.Security.Cryptography import HMACSHA1
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, Match, Capture, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Net import ServicePointManager, WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, WebRequestMethods, HttpRequestHeader
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Point, Rect, Thickness, RoutedEventHandler, SystemColors
from System.Windows.Controls import MenuItem, Separator, StackPanel, Border, Label, TextBox, ComboBox, ComboBoxItem, Button, CheckBox, WebBrowser, Orientation
from System.Windows.Media import Color, ColorConverter, Colors, SolidColorBrush, LinearGradientBrush, GradientStop, ImageBrush, TileMode, BrushMappingMode, Stretch, RenderOptions, ClearTypeHint
from System.Windows.Media.Effects import DropShadowEffect
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
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
		if json != None:
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

class Countup(Object):
	def __init__(self, initialCount):
		self.__currentCount = initialCount

	def currentCount(self):
		return self.__currentCount

	def addCount(self):
		self.__currentCount += 1

class NaiveBayes(Object):
	def __init__(self, words):
		self.wordDictionary = Dictionary[Char, List[String]]()

		for word in words:
			if word.Length > 0:
				if not self.wordDictionary.ContainsKey(word[0]):
					self.wordDictionary.Add(word[0], List[String]())

				self.wordDictionary[word[0]].Add(word)

		self.trainingDataDictionary = Dictionary[String, KeyValuePair[Dictionary[String, Countup], Countup]]()

	def train(self, document, category):
		if self.trainingDataDictionary.ContainsKey(category):
			self.trainingDataDictionary[category].Value.addCount()
		else:
			self.trainingDataDictionary.Add(category, KeyValuePair[Dictionary[String, Countup], Countup](Dictionary[String, Countup](), Countup(1)))

		for word in self.getWordList(self.wordDictionary, document):
			if self.trainingDataDictionary[category].Key.ContainsKey(word):
				self.trainingDataDictionary[category].Key[word].addCount()
			else:
				self.trainingDataDictionary[category].Key.Add(word, Countup(1))

	def classify(self, document):
		a = 1.0
		best = None
		max = Double.MinValue
		hashSet = HashSet[String]()

		for kvp in self.trainingDataDictionary.Values:
			for s in kvp.Key.Keys:
				if not hashSet.Contains(s):
					hashSet.Add(s)

		wordList = self.getWordList(self.wordDictionary, document)
		sum = 0.0

		for kvp in self.trainingDataDictionary.Values:
			sum += kvp.Value.currentCount()

		for kvp in self.trainingDataDictionary:
			# NaiveBayes
			# P(X|Y1,...,Yn)=P(Y1,...,Yn|X)P(X)
			probability = Math.Log(kvp.Value.Value.currentCount() / sum, Math.E)

			for word in wordList:
				s = 0.0

				for countup in kvp.Value.Key.Values:
					s += countup.currentCount()

				probability += Math.Log(((kvp.Value.Key[word].currentCount() if kvp.Value.Key.ContainsKey(word) else 0) + a) / (s + a * hashSet.Count), Math.E)

			if max < probability:
				max = probability
				best = kvp.Key

		return best

	def getWordList(self, dictionary, text):
		stringBuilder = StringBuilder(text)
		selectedWordList = List[String]()

		while stringBuilder.Length > 0:
			s = stringBuilder.ToString()
			selectedWord = None

			if dictionary.ContainsKey(s[0]):
				for word in dictionary[s[0]]:
					if s.StartsWith(word, StringComparison.Ordinal) and word.Length > (0 if selectedWord == None else selectedWord.Length):
						selectedWord = word

			if String.IsNullOrEmpty(selectedWord):
				stringBuilder.Remove(0, 1)
			else:
				selectedWordList.Add(selectedWord)
				stringBuilder.Remove(0, selectedWord.Length)

		return selectedWordList

def ask(text):
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	wordList = List[String]()
	documentDictionary = Dictionary[String, List[String]]()
	reverseDictionary = Dictionary[String, LinkedList[String]]()
	wordDictionary = Dictionary[Char, List[String]]()
	context = TaskScheduler.FromCurrentSynchronizationContext()
	
	bodyStringBuilder = StringBuilder()
	authWebClient = WebClient()
	dictionary = Dictionary[String, String]()
	stringBuilder = StringBuilder()
	updateWebClient = WebClient()

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

	def onLoad():
		fs1 = None
		sr1 = None
		fs2 = None
		sr2 = None

		try:
			fs1 = FileStream("Words.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			sr1 = StreamReader(fs1, UTF8Encoding(False), True)
			json = JsonDecoder.decode(sr1.ReadToEnd())
			
			if json != None:
				if clr.GetClrType(Array).IsInstanceOfType(json):
					for obj in json:
						if obj != None:
							if clr.GetClrType(String).IsInstanceOfType(obj):
								wordList.Add(obj)

			fs2 = FileStream("Training.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			sr2 = StreamReader(fs2, UTF8Encoding(False), True)
			json = JsonDecoder.decode(sr2.ReadToEnd())
			
			if json != None:
				if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
					for kvp in json:
						if kvp.Value != None:
							if clr.GetClrType(Array).IsInstanceOfType(kvp.Value):
								list = List[String]()

								for s in kvp.Value:
									if clr.GetClrType(String).IsInstanceOfType(s):
										list.Add(s)
						
								documentDictionary.Add(kvp.Key, list)

		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

		finally:
			if sr2 != None:
				sr2.Close()

			if fs2 != None:
				fs2.Close()

			if sr1 != None:
				sr1.Close()

			if fs1 != None:
				fs1.Close()
	
	def onLoaded(task):
		reverseDictionary.Add("自分", LinkedList[String]())
	
		for character in Script.Instance.Characters:
			wordList.Add(character.Name)
			reverseDictionary["自分"].AddLast(character.Name)

		tempWordDictionary = Dictionary[Char, List[String]]()

		for word in wordList:
			if word.Length > 0:
				if not tempWordDictionary.ContainsKey(word[0]):
					tempWordDictionary.Add(word[0], List[String]())

				tempWordDictionary[word[0]].Add(word)

		blockTermList = getTermList(tempWordDictionary, text)
		hashSet = HashSet[String]()
	
		for word in Script.Instance.Words:
			wordList.Add(word.Name)

			if not blockTermList.Contains(word.Name):
				for attribute in word.Attributes:
					if not reverseDictionary.ContainsKey(attribute):
						reverseDictionary.Add(attribute, LinkedList[String]())

					reverseDictionary[attribute].AddLast(word.Name)

					if not hashSet.Contains(attribute):
						hashSet.Add(attribute)
	
		for word in wordList:
			if word.Length > 0:
				if not wordDictionary.ContainsKey(word[0]):
					wordDictionary.Add(word[0], List[String]())

				wordDictionary[word[0]].Add(word)
	
		for list in documentDictionary.Values:
			for i in range(0, list.Count):
				list[i] = Regex.Replace(list[i], "\\$\\{\\*}", String.Concat("${", String.Join("|", hashSet), "}"), RegexOptions.CultureInvariant)

	def onTrain(task):
		termList = getTermList(wordDictionary, text)
		naiveBayes = NaiveBayes(wordList)
		cacheDictionary = Dictionary[String, String]()
		isEmpty = True

		for kvp in reverseDictionary:
			n = kvp.Value.Count
			array = Array.CreateInstance(String, n)
			kvp.Value.CopyTo(array, 0)
			r = Random(Environment.TickCount)

			while n > 1:
				k = r.Next(n)
				n -= 1
				temp = array[n]
				array[n] = array[k]
				array[k] = temp

			kvp.Value.Clear()

			for s in array:
				kvp.Value.AddLast(s)
		
		for value in documentDictionary.Values:
			for i in range(value.Count):
				index = 0
				sb = StringBuilder()
				
				for match in Regex.Matches(value[i], "\\$\\{(?<1>.+?)(?:\\|(?<1>.+?))*}", RegexOptions.CultureInvariant | RegexOptions.Singleline):
					if match.Index - index > 0:
						sb.Append(value[i].Substring(index, match.Index - index))

					index = match.Index

					if cacheDictionary.ContainsKey(match.Value):
						sb.Append(cacheDictionary[match.Value])
						index = match.Index + match.Length
					
					else:
						for capture in match.Groups[1].Captures:
							if reverseDictionary.ContainsKey(capture.Value):
								if reverseDictionary[capture.Value].Count > 0:
									isReplaced = False

									for term in termList:
										linkedListNode = reverseDictionary[capture.Value].Find(term)

										if linkedListNode != None:
											sb.Append(linkedListNode.Value)
											index = match.Index + match.Length
											cacheDictionary.Add(match.Value, linkedListNode.Value)
											reverseDictionary[capture.Value].Remove(linkedListNode)
											reverseDictionary[capture.Value].AddLast(linkedListNode)
											isReplaced = True

											break

									if isReplaced:
										break

						else:
							for capture in match.Groups[1].Captures:
								if reverseDictionary.ContainsKey(capture.Value):
									if reverseDictionary[capture.Value].Count > 0:
										linkedListNode = reverseDictionary[capture.Value].First
										sb.Append(linkedListNode.Value)
										index = match.Index + match.Length
										cacheDictionary.Add(match.Value, linkedListNode.Value)
										reverseDictionary[capture.Value].Remove(linkedListNode)
										reverseDictionary[capture.Value].AddLast(linkedListNode)

										break

				if value[i].Length - index > 0:
					sb.Append(value[i].Substring(index, value[i].Length - index))

				value[i] = sb.ToString()

			if value.Exists(lambda x: getTermList(wordDictionary, x).Exists(lambda y: termList.Exists(lambda z: y.Equals(z)))):
				isEmpty = False

		if not isEmpty:
			for kvp in documentDictionary:
				for s in kvp.Value:
					if not Regex.IsMatch(s, "\\$\\{.+?}", RegexOptions.CultureInvariant | RegexOptions.Singleline):
						naiveBayes.train(s, kvp.Key)

		return naiveBayes

	def onTrained(task):
		category = task.Result.classify(text)
		tempDictionary = Dictionary[Char, List[String]]()
		sequenceList = List[Sequence]()
		isCompleted = True

		for word in Script.Instance.Words:
			if word.Name.Length > 0:
				if not tempDictionary.ContainsKey(word.Name[0]):
					tempDictionary.Add(word.Name[0], List[String]())

				tempDictionary[word.Name[0]].Add(word.Name)

		availableTermList = getTermList(tempDictionary, text)

		if category == None:
			if availableTermList.Count > 0:
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Activate"):
						sequenceList.Add(sequence)

				preparedSequences = Script.Instance.Prepare(sequenceList, None, availableTermList)

				if not Script.Instance.TryEnqueue(preparedSequences):
					sequenceList.Clear()

					for sequence in Script.Instance.Sequences:
						if sequence.Name.Equals("Ignore"):
							sequenceList.Add(sequence)

					preparedSequences = Script.Instance.Prepare(sequenceList, text, availableTermList)
					
					if not Script.Instance.TryEnqueue(preparedSequences):
						preparedSequences = Script.Instance.Prepare(sequenceList, text, Enumerable.Empty[String]())

						if not Script.Instance.TryEnqueue(preparedSequences):
							isCompleted = False

			else:
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Ignore"):
						sequenceList.Add(sequence)

				preparedSequences = Script.Instance.Prepare(sequenceList, text, Enumerable.Empty[String]())

				if not Script.Instance.TryEnqueue(preparedSequences):
					isCompleted = False

		else:
			for sequence in Script.Instance.Sequences:
				if sequence.Name.Equals(category):
					sequenceList.Add(sequence)

			preparedSequences = Script.Instance.Prepare(sequenceList, text, availableTermList)

			if availableTermList.Count > 0:
				if not Script.Instance.TryEnqueue(preparedSequences):
					preparedSequences = Script.Instance.Prepare(sequenceList, text, Enumerable.Empty[String]())

					if not Script.Instance.TryEnqueue(preparedSequences):
						sequenceList.Clear()

						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Activate"):
								sequenceList.Add(sequence)

						preparedSequences = Script.Instance.Prepare(sequenceList, None, availableTermList)

						if not Script.Instance.TryEnqueue(preparedSequences):
							sequenceList.Clear()

							for sequence in Script.Instance.Sequences:
								if sequence.Name.Equals("Ignore"):
									sequenceList.Add(sequence)

							preparedSequences = Script.Instance.Prepare(sequenceList, text, availableTermList)
							
							if not Script.Instance.TryEnqueue(preparedSequences):
								preparedSequences = Script.Instance.Prepare(sequenceList, text, Enumerable.Empty[String]())

								if not Script.Instance.TryEnqueue(preparedSequences):
									isCompleted = False

			elif not Script.Instance.TryEnqueue(preparedSequences):
				sequenceList.Clear()

				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Ignore"):
						sequenceList.Add(sequence)

				preparedSequences = Script.Instance.Prepare(sequenceList, text, Enumerable.Empty[String]())

				if not Script.Instance.TryEnqueue(preparedSequences):
					isCompleted = False
				
		if isCompleted:
			d = Dictionary[String, StringBuilder]()
				
			for sequence in preparedSequences:
				for o in sequence:
					if clr.GetClrType(Message).IsInstanceOfType(o):
						if d.ContainsKey(sequence.Owner):
							d[sequence.Owner].Append(o.Text)
						else:
							for kvp in d:
								stringBuilder.AppendFormat(" \"{0}: {1}\"", kvp.Key, kvp.Value.ToString())

							d.Clear()
							d.Add(sequence.Owner, StringBuilder(o.Text))

			for kvp in d:
				stringBuilder.AppendFormat(" \"{0}: {1}\"", kvp.Key, kvp.Value.ToString())

			if stringBuilder.Length > 0:
				stringBuilder.Insert(0, text)
				stringBuilder.Append(" #apricotan")

	def onPrepare(task):
		try:
			if NetworkInterface.GetIsNetworkAvailable() and (String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret)) and bodyStringBuilder.Length > 0:
				bytes = authWebClient.UploadData(Uri("https://api.twitter.com/oauth/access_token"), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(bodyStringBuilder.ToString()))

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
	
		if stringBuilder.Length > 0:
			for kvp in dictionary:
				if kvp.Key.Equals("oauth_token"):
					oauthToken = kvp.Value
				elif kvp.Key.Equals("oauth_token_secret"):
					oauthTokenSecret = kvp.Value

			if String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)
				sortedDictionary.Add("status", urlEncode(stringBuilder.ToString()))

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri(String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(stringBuilder.ToString()))), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				updateWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				updateWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")

				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("http://api.twitter.com/1/statuses/home_timeline.xml"), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)
		
	def onUpdate(task):
		if NetworkInterface.GetIsNetworkAvailable() and String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False and stringBuilder.Length > 0:
			try:
				updateWebClient.UploadData(Uri(String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(stringBuilder.ToString()))), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Empty))

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

	Task.Factory.StartNew(onLoad, TaskCreationOptions.LongRunning).ContinueWith(onLoaded, context).ContinueWith[NaiveBayes](onTrain, TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[NaiveBayes]](onTrained), context).ContinueWith(onPrepare, TaskContinuationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning)

def update():
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
	bodyStringBuilder = StringBuilder()
	webClient = WebClient()
	dictionary = Dictionary[String, String]()
	request = WebRequest.Create("http://api.twitter.com/1/statuses/home_timeline.xml")
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

		if String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("http://api.twitter.com/1/statuses/home_timeline.xml"), sortedDictionary))))
			
			sortedDictionary.Add("oauth_signature", signature)

			request.Method = WebRequestMethods.Http.Get
			request.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
			request.ContentType = "application/x-www-form-urlencoded"

	def onUpdate(task):
		if NetworkInterface.GetIsNetworkAvailable() and String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
			try:
				response = None
				stream = None

				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
			
					for statusXmlNode in doc.SelectNodes("/statuses/status"):
						entry = Entry()
						id = String.Empty
						author = String.Empty

						for xmlNode in statusXmlNode.ChildNodes:
							if xmlNode.Name.Equals("id"):
								id = xmlNode.InnerText
							elif xmlNode.Name.Equals("text"):
								entry.Title = Regex.Replace(xmlNode.InnerText, "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
							elif xmlNode.Name.Equals("created_at"):
								entry.Created = entry.Modified = DateTime.ParseExact(xmlNode.InnerText, "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)
							elif xmlNode.Name.Equals("user"):
								for childXmlNode in xmlNode.ChildNodes:
									if childXmlNode.Name.Equals("screen_name"):
										author = childXmlNode.InnerText
									elif childXmlNode.Name.Equals("profile_image_url"):
										entry.Image = Uri(childXmlNode.InnerText)
				
						entry.Resource = Uri(String.Concat("http://twitter.com/", author, "/statuses/", id))
						entry.Author = author
						entryList.Add(entry)

				finally:
					if stream != None:
						stream.Close()
				
					if response != None:
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

				if entry.Created > dateTime and String.IsNullOrEmpty(entry.Title) == False:
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

	Task.Factory.StartNew(onAuth, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

def search(query):
	entryList = List[Entry]()

	def onSearch():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				request = WebRequest.Create(String.Concat("http://search.twitter.com/search.atom?q=", urlEncode(query)))
				request.Method = WebRequestMethods.Http.Get
				request.ContentType = "application/x-www-form-urlencoded"
				response = None
				stream = None

				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
			
					for childNode in doc.DocumentElement.ChildNodes:
						if childNode.Name.Equals("entry"):
							entry = Entry()
				
							for xmlNode in childNode.ChildNodes:
								if xmlNode.Name.Equals("link"):
									type = None

									for attribute in xmlNode.Attributes:
										if attribute.Name.Equals("type"):
											type = attribute.Value
									
									if not String.IsNullOrEmpty(type):
										for attribute in xmlNode.Attributes:
											if attribute.Name.Equals("href"):
												if type.Equals("text/html"):
													entry.Resource = Uri(attribute.Value)
												elif type.StartsWith("image", StringComparison.Ordinal):
													entry.Image = Uri(attribute.Value)
								elif xmlNode.Name.Equals("title"):
									text = Regex.Replace(xmlNode.InnerText, "\\#apricotan", String.Empty, RegexOptions.CultureInvariant).Trim()

									if text.StartsWith("\"", StringComparison.Ordinal) and text.EndsWith("\"", StringComparison.Ordinal):
										text = text.Trim("\"".ToCharArray())

									entry.Title = text
								elif xmlNode.Name.Equals("published"):
									entry.Created = DateTime.Parse(xmlNode.InnerText)
								elif xmlNode.Name.Equals("updated"):
									entry.Modified = DateTime.Parse(xmlNode.InnerText)
								elif xmlNode.Name.Equals("author"):
									for node in xmlNode.ChildNodes:
										if node.Name.Equals("name"):
											entry.Author = node.InnerText
				
							if entry.Resource != None and String.IsNullOrEmpty(entry.Title) == False:
								if not String.IsNullOrEmpty(entry.Author):
									if entry.Author.StartsWith("apricotan", StringComparison.Ordinal):
										continue
						
								entryList.Add(entry)
			
				finally:
					if stream != None:
						stream.Close()
				
					if response != None:
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

	if (String.IsNullOrEmpty(username) == False and String.IsNullOrEmpty(password) == False) or (String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False):
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
	timelineRequest = WebRequest.Create("http://api.twitter.com/1/statuses/home_timeline.xml")
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

				if entry.Created > dateTime and String.IsNullOrEmpty(entry.Title) == False:
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

			if String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False and isLoadable:
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1/account/verify_credentials.json"), sortedDictionary))))

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
				uploadRequest.AllowWriteStreamBuffering = True
				uploadRequest.ContentType = String.Format("multipart/form-data; boundary={0}", boundary)
				uploadRequest.Method = WebRequestMethods.Http.Post
				uploadRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
				uploadRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1/account/verify_credentials.json")
				uploadRequest.ContentLength = byteList.Count

		def onUpload(task):
			if NetworkInterface.GetIsNetworkAvailable() and isLoadable and String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
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
						if responseStream != None:
							responseStream.Close()

						if response != None:
							response.Close()

						if requestStream != None:
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

			if String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)
				sortedDictionary.Add("status", urlEncode(sb.ToString()))

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri(String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(sb.ToString()))), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				updateWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				updateWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")

				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("http://api.twitter.com/1/statuses/home_timeline.xml"), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)
		
				timelineRequest.Method = WebRequestMethods.Http.Get
				timelineRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				timelineRequest.ContentType = "application/x-www-form-urlencoded"
		
		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable() and String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
				try:
					updateWebClient.UploadData(Uri(String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(sb.ToString()))), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Empty))
				
					response = None
					stream = None

					try:
						response = timelineRequest.GetResponse()
						stream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(stream)
			
						for statusXmlNode in doc.SelectNodes("/statuses/status"):
							entry = Entry()
							id = String.Empty
							author = String.Empty
				
							for xmlNode in statusXmlNode.ChildNodes:
								if xmlNode.Name.Equals("id"):
									id = xmlNode.InnerText
								elif xmlNode.Name.Equals("text"):
									entry.Title = Regex.Replace(xmlNode.InnerText, "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
								elif xmlNode.Name.Equals("created_at"):
									entry.Created = entry.Modified = DateTime.ParseExact(xmlNode.InnerText, "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)
								elif xmlNode.Name.Equals("user"):
									for childXmlNode in xmlNode.ChildNodes:
										if childXmlNode.Name.Equals("screen_name"):
											author = childXmlNode.InnerText
										elif childXmlNode.Name.Equals("profile_image_url"):
											entry.Image = Uri(childXmlNode.InnerText)
				
							entry.Resource = Uri(String.Concat("http://twitter.com/", author, "/statuses/", id))
							entry.Author = author
							entryList.Add(entry)

					finally:
						if stream != None:
							stream.Close()
				
						if response != None:
							response.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

		Task.Factory.StartNew(onPrepare, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpload, TaskContinuationOptions.LongRunning).ContinueWith(onUploaded, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)
	else:
		updateRequest = WebRequest.Create("https://upload.twitter.com/1/statuses/update_with_media.json" if isLoadable else String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(text)))
		
		def onReady(task):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	
			for kvp in dictionary:
				if kvp.Key.Equals("oauth_token"):
					oauthToken = kvp.Value
				elif kvp.Key.Equals("oauth_token_secret"):
					oauthTokenSecret = kvp.Value

			if String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
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
					updateRequest.ContentType = "application/x-www-form-urlencoded"
					sortedDictionary.Add("status", urlEncode(text))

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri("https://upload.twitter.com/1/statuses/update_with_media.json" if isLoadable else String.Concat("http://api.twitter.com/1/statuses/update.xml?status=", urlEncode(text))), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				updateRequest.PreAuthenticate = True
				updateRequest.AllowWriteStreamBuffering = True
				updateRequest.Method = WebRequestMethods.Http.Post
				updateRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				updateRequest.ContentLength = byteList.Count

				sortedDictionary = createCommonParameters(consumerKey)
				sortedDictionary.Add("oauth_token", oauthToken)

				hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
				signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("http://api.twitter.com/1/statuses/home_timeline.xml"), sortedDictionary))))
			
				sortedDictionary.Add("oauth_signature", signature)

				timelineRequest.Method = WebRequestMethods.Http.Get
				timelineRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
				timelineRequest.ContentType = "application/x-www-form-urlencoded"

		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable() and String.IsNullOrEmpty(oauthToken) == False and String.IsNullOrEmpty(oauthTokenSecret) == False:
				try:
					requestStream = None

					try:
						requestStream = updateRequest.GetRequestStream()
						requestStream.Write(byteList.ToArray(), 0, byteList.Count)

					finally:
						if requestStream != None:
							requestStream.Close()

					response = None
					stream = None

					try:
						response = timelineRequest.GetResponse()
						stream = response.GetResponseStream()
						doc = XmlDocument()
						doc.Load(stream)
			
						for statusXmlNode in doc.SelectNodes("/statuses/status"):
							entry = Entry()
							id = String.Empty
							author = String.Empty
				
							for xmlNode in statusXmlNode.ChildNodes:
								if xmlNode.Name.Equals("id"):
									id = xmlNode.InnerText
								elif xmlNode.Name.Equals("text"):
									entry.Title = Regex.Replace(xmlNode.InnerText, "[\r\n]", String.Empty, RegexOptions.CultureInvariant)
								elif xmlNode.Name.Equals("created_at"):
									entry.Created = entry.Modified = DateTime.ParseExact(xmlNode.InnerText, "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None)
								elif xmlNode.Name.Equals("user"):
									for childXmlNode in xmlNode.ChildNodes:
										if childXmlNode.Name.Equals("screen_name"):
											author = childXmlNode.InnerText
										elif childXmlNode.Name.Equals("profile_image_url"):
											entry.Image = Uri(childXmlNode.InnerText)
				
							entry.Resource = Uri(String.Concat("http://twitter.com/", author, "/statuses/", id))
							entry.Author = author
							entryList.Add(entry)

					finally:
						if stream != None:
							stream.Close()
				
						if response != None:
							response.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

		Task.Factory.StartNew(onPrepare, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

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
							if streamReader != None:
								streamReader.Close()
										
							if stream != None:
								stream.Close()
				
							if response != None:
								response.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			def onOAuthRequestTokenCompleted(task):
				if dictionary.ContainsKey("oauth_token") and dictionary.ContainsKey("oauth_token_secret"):
					window = Window()
					pinTextBox = TextBox()
					
					stackPanel1 = StackPanel()
					stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
					stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
					stackPanel1.Orientation = Orientation.Vertical

					webBrowser = WebBrowser()
					webBrowser.HorizontalAlignment = HorizontalAlignment.Stretch
					webBrowser.VerticalAlignment = VerticalAlignment.Stretch
					webBrowser.Width = 640
					webBrowser.Height = 650
			
					stackPanel1.Children.Add(webBrowser)

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
	
											if config == None:
												config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
												directoryInfo = None

											if config.HasFile:
												if config.AppSettings.Settings["Scripts"] != None:
													di = DirectoryInfo(config.AppSettings.Settings["Scripts"].Value if directoryInfo == None else System.IO.Path.Combine(directoryInfo.FullName, config.AppSettings.Settings["Scripts"].Value));
													
													for fileInfo in di.GetFiles("*.py"):
														fs1 = None
														sr = None
														lines = None

														try:
															fs1 = FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)
															sr = StreamReader(fs1, UTF8Encoding(False), True)
															lines = sr.ReadToEnd()

														finally:
															if sr != None:
																sr.Close()

															if fs1 != None:
																fs1.Close()

														if lines != None:
															if Regex.IsMatch(lines, "\\# Twitter.py", RegexOptions.CultureInvariant):
																lines = Regex.Replace(lines, "oauthToken\\s*=\\s*None", String.Format("oauthToken = \"{0}\"", d["oauth_token"]), RegexOptions.CultureInvariant)
																lines = Regex.Replace(lines, "oauthTokenSecret\\s*=\\s*None", String.Format("oauthTokenSecret = \"{0}\"", d["oauth_token_secret"]), RegexOptions.CultureInvariant)
																fs2 = None
																sw = None

																try:
																	fs2 = FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)
																	sw = StreamWriter(fs2, UTF8Encoding(False))
																	sw.Write(lines)

																finally:
																	if sw != None:
																		sw.Close()

																	if fs2 != None:
																		fs2.Close()

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

					solidColorBrush = SolidColorBrush(Colors.White)
					solidColorBrush.Opacity = 0.5

					if solidColorBrush.CanFreeze:
						solidColorBrush.Freeze()

					border = Border()
					border.HorizontalAlignment = HorizontalAlignment.Stretch
					border.VerticalAlignment = VerticalAlignment.Stretch
					border.BorderThickness = Thickness(0, 1, 0, 0)
					border.BorderBrush = solidColorBrush

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
					border.Child = stackPanel2
					stackPanel1.Children.Add(border)
			
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
			signInMenuItem.Header = "サインイン"
		else:
			signInMenuItem.Header = "Sign in"

		signInMenuItem.Click += onSignInClick

		menuItem.Items.Add(signInMenuItem)

	else:
		isAskable = False

		for sequence in Script.Instance.Sequences:
			if sequence.Name.Equals("Greet") or sequence.Name.Equals("Hate") or sequence.Name.Equals("Interest") or sequence.Name.Equals("Thank") or sequence.Name.Equals("Ignore"):
				isAskable = True

				break
	
		sb = StringBuilder()

		for character in Script.Instance.Characters:
			if sb.Length > 0:
				sb.Append("/")

			sb.Append(character.Name)

		def onMouseUp(sender, mbea):
			mbea.Handled = True

		def onKeyDown(sender, kea):
			kea.Handled = True

		if isAskable:
			askMenuItem = MenuItem()
		
			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				askMenuItem.Header = "話しかける..."
			else:
				askMenuItem.Header = "Ask..."

			def onClick(sender, args):
				config = None
				directoryInfo = DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name))
				imageBrush = None
				textBrush = None
			
				if directoryInfo.Exists:
					fileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location)
		
					for fileInfo in directoryInfo.EnumerateFiles("*.config"):
						if fileName.Equals(Path.GetFileNameWithoutExtension(fileInfo.Name)):
							exeConfigurationFileMap = ExeConfigurationFileMap()
				
							exeConfigurationFileMap.ExeConfigFilename = fileInfo.FullName
							config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
				if config == None:
					config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
					directoryInfo = None

				if config.HasFile:
					if config.AppSettings.Settings["BackgroundImage"] != None:
						fileInfo = FileInfo(config.AppSettings.Settings["BackgroundImage"].Value if directoryInfo == None else System.IO.Path.Combine(directoryInfo.FullName, config.AppSettings.Settings["BackgroundImage"].Value));
						fs = None
						bi = BitmapImage()

						try:
							fs = FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)

							bi.BeginInit()
							bi.StreamSource = fs
							bi.CacheOption = BitmapCacheOption.OnLoad
							bi.CreateOptions = BitmapCreateOptions.None
							bi.EndInit()

						finally:
							if fs != None:
								fs.Close()

						imageBrush = ImageBrush(bi)
						imageBrush.TileMode = TileMode.Tile
						imageBrush.ViewportUnits = BrushMappingMode.Absolute
						imageBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
						imageBrush.Stretch = Stretch.None

						if imageBrush.CanFreeze:
							imageBrush.Freeze()

					if config.AppSettings.Settings["TextColor"] != None:
						if config.AppSettings.Settings["TextColor"].Value.Length > 0:
							textBrush = SolidColorBrush(ColorConverter.ConvertFromString(config.AppSettings.Settings["TextColor"].Value))

							if textBrush.CanFreeze:
								textBrush.Freeze()

				window = Window()

				stackPanel1 = StackPanel()
				stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel1.Orientation = Orientation.Vertical

				stackPanel2 = StackPanel()
				stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel2.Orientation = Orientation.Vertical
				stackPanel2.Background = SystemColors.WindowBrush if imageBrush == None else imageBrush

				linearGradientBrush = LinearGradientBrush()
				gradientStop1 = GradientStop(Color.FromArgb(0, 0, 0, 0), 0)
				gradientStop2 = GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1)

				linearGradientBrush.StartPoint = Point(0.5, 0)
				linearGradientBrush.EndPoint = Point(0.5, 1)
				linearGradientBrush.Opacity = 0.1
				linearGradientBrush.GradientStops.Add(gradientStop1)
				linearGradientBrush.GradientStops.Add(gradientStop2)

				if linearGradientBrush.CanFreeze:
					linearGradientBrush.Freeze()

				stackPanel3 = StackPanel()
				stackPanel3.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel3.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel3.Orientation = Orientation.Vertical
				stackPanel3.Background = linearGradientBrush

				stackPanel4 = StackPanel()
				stackPanel4.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel4.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel4.Orientation = Orientation.Vertical
				stackPanel4.Margin = Thickness(10, 10, 10, 20)

				stackPanel5 = StackPanel()
				stackPanel5.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel5.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel5.Orientation = Orientation.Vertical

				label = Label()
				label.Foreground = SystemColors.ControlTextBrush if textBrush == None else textBrush

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					label.Content = "話しかける一言"
				else:
					label.Content = "Message"

				RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
			
				textColor = SystemColors.ControlText if textBrush == None else textBrush.Color

				dropShadowEffect = DropShadowEffect()
				dropShadowEffect.BlurRadius = 1
				dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(textColor.R, textColor.G), textColor.B) > Byte.MaxValue / 2 else Colors.White;
				dropShadowEffect.Direction = 270
				dropShadowEffect.Opacity = 0.5
				dropShadowEffect.ShadowDepth = 1

				if dropShadowEffect.CanFreeze:
					dropShadowEffect.Freeze()

				stackPanel5.Effect = dropShadowEffect
				stackPanel5.Children.Add(label)

				textBox = TextBox()
				textBox.Width = 240
			
				stackPanel4.Children.Add(stackPanel5)
				stackPanel4.Children.Add(textBox)
			
				stackPanel3.Children.Add(stackPanel4)
				stackPanel2.Children.Add(stackPanel3)
				stackPanel1.Children.Add(stackPanel2)

				def onAskClick(source, rea):
					if not String.IsNullOrEmpty(textBox.Text):
						ask(textBox.Text)

					window.Close()

				solidColorBrush = SolidColorBrush(Colors.White)
				solidColorBrush.Opacity = 0.5

				if solidColorBrush.CanFreeze:
					solidColorBrush.Freeze()

				border = Border()
				border.HorizontalAlignment = HorizontalAlignment.Stretch
				border.VerticalAlignment = VerticalAlignment.Stretch
				border.BorderThickness = Thickness(0, 1, 0, 0)
				border.BorderBrush = solidColorBrush

				askButton = Button()
				askButton.HorizontalAlignment = HorizontalAlignment.Right
				askButton.VerticalAlignment = VerticalAlignment.Center
				askButton.Margin = Thickness(10, 10, 10, 10)
				askButton.Padding = Thickness(10, 2, 10, 2)
				askButton.IsDefault = True

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					askButton.Content = "話しかける"
				else:
					askButton.Content = "Ask"

				askButton.Click += onAskClick

				border.Child = askButton
				stackPanel1.Children.Add(border)
				textBox.Focus()
			
				window.Owner = Application.Current.MainWindow
				window.Title = Application.Current.MainWindow.Title
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner
				window.ResizeMode = ResizeMode.NoResize
				window.SizeToContent = SizeToContent.WidthAndHeight
				window.Background = SystemColors.ControlBrush
				window.Content = stackPanel1
				window.Show()

			askMenuItem.Click += onClick

			menuItem.Items.Add(askMenuItem)
			menuItem.Items.Add(Separator())
		
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
					stack.Push(String.Format("\"{0}: {1}\" #apricotan", sb.ToString(), message.Text))

		while stack.Count > 0:
			comboBoxItem = ComboBoxItem()
			comboBoxItem.Content = stack.Pop()
			comboBox.Items.Add(comboBoxItem)

			if comboBox.SelectedItem == None:
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
				if s.StartsWith(term, StringComparison.Ordinal) and term.Length > (0 if selectedTerm == None else selectedTerm.Length):
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
		if window == Application.Current.MainWindow and window.ContextMenu != None:
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