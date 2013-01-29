# -*- coding: utf-8 -*-
# Social.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, ValueType, Nullable, Boolean, Byte, Char, UInt32, Int32, Double, String, StringComparison, Uri, DateTime, TimeSpan, Array, Convert, BitConverter, Type, Environment, Math, Random, Action
from System.IO import Stream, FileStream, StreamReader, StreamWriter, Directory, File, FileMode, FileAccess, FileShare
from System.Collections.Generic import List, LinkedList, Dictionary, SortedDictionary, KeyValuePair, Queue, HashSet
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Globalization import CultureInfo, NumberStyles, DateTimeStyles
from System.Linq import Enumerable
from System.Diagnostics import Process, Trace
from System.Reflection import Assembly
from System.Security.Cryptography import HMACSHA1
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, RegexOptions, Match, MatchEvaluator
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, HttpRequestHeader, WebRequestMethods, HttpStatusCode
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, WindowStyle, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Point, Rect, Thickness, SystemColors, PropertyPath, CornerRadius, FontSizeConverter, FontWeights
from System.Windows.Controls import ContentControl, MenuItem, Separator, Border, Label, Button, StackPanel, Orientation, DockPanel, Canvas, TextBlock, TextBox, WebBrowser
from System.Windows.Media import Color, Colors, ColorConverter, Brushes, SolidColorBrush, LinearGradientBrush, GradientStop, RenderOptions, ClearTypeHint, ScaleTransform, StreamGeometry, FillRule, StreamGeometryContext, BitmapCache, ImageBrush, TileMode, BrushMappingMode, Stretch
from System.Windows.Media.Animation import Storyboard, HandoffBehavior, Clock, ClockState, DoubleAnimation, SineEase, EasingMode
from System.Windows.Media.Effects import DropShadowEffect
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
from System.Windows.Shapes import Rectangle
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Agent, Balloon, Script, Entry, Message, Word, Sequence

autoUpdate = False
oauthToken = None
oauthTokenSecret = None
consumerKey = "5Y4BpcdcEwkZeIRqIMdJyg"
consumerSecret = "AMsengQQpxsvKEnuEX8oagKTLjrcujY7hBzVKlo72O0"

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
		if json is not None:
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
		elif value is None:
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
					if s.StartsWith(word, StringComparison.Ordinal) and word.Length > (0 if selectedWord is None else selectedWord.Length):
						selectedWord = word

			if String.IsNullOrEmpty(selectedWord):
				stringBuilder.Remove(0, 1)
			else:
				selectedWordList.Add(selectedWord)
				stringBuilder.Remove(0, selectedWord.Length)

		return selectedWordList

def ask(text):
	global consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	wordList = List[String]()
	documentDictionary = Dictionary[String, List[String]]()
	wordDictionary = Dictionary[String, List[String]]()
	attributeHashSet = HashSet[String]()
	stringBuilder = StringBuilder()
	updateWebClient = WebClient()

	sortedDictionary = createCommonParameters(consumerKey)
	sortedDictionary.Add("oauth_token", oauthToken)

	hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
	signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

	sortedDictionary.Add("realm", "http://api.twitter.com/")
	sortedDictionary.Add("oauth_signature", signature)

	verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
	verifyRequest.PreAuthenticate = True
	verifyRequest.Method = WebRequestMethods.Http.Get
	verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
	verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")

	likesRequestList = List[WebRequest]()
	verifyDictionary = Dictionary[String, String]()
	userDictionary = Dictionary[String, Double]()
	currentLikesDictionary = Dictionary[String, Int32]()
	context = TaskScheduler.FromCurrentSynchronizationContext()
	
	def onLoad():
		fs1 = None
		sr1 = None
		fs2 = None
		sr2 = None

		try:
			fs1 = FileStream("Words.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			sr1 = StreamReader(fs1, UTF8Encoding(False), True)
			json = Json.decode(sr1.ReadToEnd())
			
			if json is not None:
				if clr.GetClrType(Array).IsInstanceOfType(json):
					for obj in json:
						if obj is not None:
							if clr.GetClrType(String).IsInstanceOfType(obj):
								wordList.Add(obj)

			fs2 = FileStream("Training.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			sr2 = StreamReader(fs2, UTF8Encoding(False), True)
			json = Json.decode(sr2.ReadToEnd())
			
			if json is not None:
				if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
					for kvp in json:
						if kvp.Value is not None:
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
			if sr2 is not None:
				sr2.Close()

			if fs2 is not None:
				fs2.Close()

			if sr1 is not None:
				sr1.Close()

			if fs1 is not None:
				fs1.Close()
	
	def onLoaded(task):
		tempWordDictionary = Dictionary[Char, List[String]]()

		for word in wordList:
			if word.Length > 0:
				if not tempWordDictionary.ContainsKey(word[0]):
					tempWordDictionary.Add(word[0], List[String]())

				tempWordDictionary[word[0]].Add(word)

		blockTermList = getTermList(tempWordDictionary, text)
		
		for word in Script.Instance.Words:
			wordList.Add(word.Name)

			if not blockTermList.Contains(word.Name):
				if not wordDictionary.ContainsKey(word.Name):
					wordDictionary.Add(word.Name, List[String]())
				
				for attribute in word.Attributes:
					wordDictionary[word.Name].Add(attribute)

					if not attributeHashSet.Contains(attribute):
						attributeHashSet.Add(attribute)

		for list in documentDictionary.Values:
			for i in range(0, list.Count):
				list[i] = Regex.Replace(list[i], "(?<1>(?<Open>\\{{2})*)\\{\\*}(?<2>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", MatchEvaluator(lambda x: String.Concat(Regex.Replace(x.Groups[1].Value, "\\{\\{", "{", RegexOptions.CultureInvariant), "{", String.Join("|", attributeHashSet), "}", Regex.Replace(x.Groups[2].Value, "}}", "}", RegexOptions.CultureInvariant))), RegexOptions.CultureInvariant)

		for character in Script.Instance.Characters:
			wordList.Add(character.Name)

			if not wordDictionary.ContainsKey(character.Name):
				wordDictionary.Add(character.Name, List[String]())

			wordDictionary[character.Name].Add("自分")

		if not attributeHashSet.Contains("自分"):
			attributeHashSet.Add("自分")
		
	def onTrain(task):
		tempStringBuilder = StringBuilder(text)
		termHashSet = HashSet[String]()
		termDictionary = Dictionary[Char, List[String]]()
		usageDictionary = Dictionary[String, List[String]]()
		cacheDictionary = Dictionary[String, String]()
		isEmpty = True
		naiveBayes = NaiveBayes(wordList)
		
		for word in wordList:
			if word.Length > 0:
				if not termDictionary.ContainsKey(word[0]):
					termDictionary.Add(word[0], List[String]())

				termDictionary[word[0]].Add(word)

		while tempStringBuilder.Length > 0:
			s = tempStringBuilder.ToString()
			selectedTerm = None

			if termDictionary.ContainsKey(s[0]):
				for term in termDictionary[s[0]]:
					if s.StartsWith(term, StringComparison.Ordinal) and term.Length > (0 if selectedTerm is None else selectedTerm.Length):
						selectedTerm = term
		
			if String.IsNullOrEmpty(selectedTerm):
				tempStringBuilder.Remove(0, 1)
			else:
				if not termHashSet.Contains(selectedTerm):
					termHashSet.Add(selectedTerm)

				tempStringBuilder.Remove(0, selectedTerm.Length)

		for value in wordDictionary.Values:
			for attribute in value:
				if not attributeHashSet.Contains(attribute):
					attributeHashSet.Add(attribute)
		
		for value in documentDictionary.Values:
			for i in range(value.Count):
				for match in Regex.Matches(value[i], "(?<Open>\\{{2})*\\{(?<1>(?:[^{}]|\\{{2}|}{2})+)}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
					pattern = Regex.Replace(match.Groups[1].Value, "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant)
					
					for attribute in attributeHashSet:
						if Regex.IsMatch(attribute, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline):
							if not usageDictionary.ContainsKey(attribute):
								usageDictionary.Add(attribute, List[String]())

							usageDictionary[attribute].Add(pattern)

		for value in documentDictionary.Values:
			for i in range(value.Count):
				index = 0
				sb = StringBuilder()

				for match in Regex.Matches(value[i], "(?<1>(?<Open>\\{{2})*)\\{(?<2>(?:[^{}]|\\{{2}|}{2})+)}(?<3>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
					if match.Index - index > 0:
						sb.Append(Regex.Replace(value[i].Substring(index, match.Index - index), "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant))

					sb.Append(Regex.Replace(match.Groups[1].Value, "\\{\\{", "{", RegexOptions.CultureInvariant))

					if cacheDictionary.ContainsKey(match.Value):
						sb.Append(Regex.Replace(match.Groups[1].Value, "\\{\\{", "{", RegexOptions.CultureInvariant))
						sb.Append(cacheDictionary[match.Value])
						sb.Append(Regex.Replace(match.Groups[3].Value, "}}", "}", RegexOptions.CultureInvariant))

					else:
						pattern = Regex.Replace(match.Groups[2].Value, "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant)
						max1 = 0
						word1 = None
						max2 = 0
						word2 = None

						for word in wordList:
							if wordDictionary.ContainsKey(word):
								for attribute in wordDictionary[word]:
									if usageDictionary.ContainsKey(attribute):
										if usageDictionary[attribute].Contains(pattern):
											if termHashSet.Contains(word):
												if usageDictionary[attribute].Count > max1:
													max1 = usageDictionary[attribute].Count
													word1 = word

											else:
												if usageDictionary[attribute].Count > max2:
													max2 = usageDictionary[attribute].Count
													word2 = word

						if word1 is None:
							if word2 is None:
								sb.Append(match.Value)

							else:
								sb.Append(Regex.Replace(match.Groups[1].Value, "\\{\\{", "{", RegexOptions.CultureInvariant))
								sb.Append(word2)
								sb.Append(Regex.Replace(match.Groups[3].Value, "}}", "}", RegexOptions.CultureInvariant))
								cacheDictionary.Add(match.Value, word2)

						else:
							sb.Append(Regex.Replace(match.Groups[1].Value, "\\{\\{", "{", RegexOptions.CultureInvariant))
							sb.Append(word1)
							sb.Append(Regex.Replace(match.Groups[3].Value, "}}", "}", RegexOptions.CultureInvariant))
							cacheDictionary.Add(match.Value, word1)

					index = match.Index + match.Length

				if value[i].Length - index > 0:
					sb.Append(Regex.Replace(value[i].Substring(index, value[i].Length - index), "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant))

				value[i] = sb.ToString()

			if value.Exists(lambda x: getTermList(termDictionary, x).Exists(lambda y: termHashSet.Contains(y))):
				isEmpty = False

		if not isEmpty:
			for kvp in documentDictionary:
				for s in kvp.Value:
					if not Regex.IsMatch(s, "(?<Open>\\{{2})*\\{([^{}]|\\{{2}|}{2})+}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
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

		if category is None:
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

	def onVerify(task):
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				response = None
				stream = None
				streamReader = None

				try:
					response = verifyRequest.GetResponse()
						
					if response.StatusCode == HttpStatusCode.OK:
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = Json.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
								if json.ContainsKey("user"):
									user = json["user"]
										
									if user is not None:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
											if user.ContainsKey("name"):
												if clr.GetClrType(String).IsInstanceOfType(user["name"]):
													verifyDictionary.Add("name", user["name"])

											if user.ContainsKey("password"):
												if clr.GetClrType(String).IsInstanceOfType(user["password"]):
													verifyDictionary.Add("password", user["password"])

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

	def onReady(task):
		global username, password

		for kvp in verifyDictionary:
			if kvp.Key.Equals("name"):
				username = kvp.Value
			elif kvp.Key.Equals("password"):
				password = kvp.Value

		if username is not None and password is not None and not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)
			sortedDictionary.Add("status", urlEncode(stringBuilder.ToString()))

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri(String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(stringBuilder.ToString()))), sortedDictionary))))
			
			sortedDictionary.Add("oauth_signature", signature)
			sortedDictionary.Remove("status")

			updateWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
			updateWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")

			for character in Script.Instance.Characters:
				likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
				likesRequest.Method = WebRequestMethods.Http.Get

				likesRequestList.Add(likesRequest)

			return True

		return False

	def onUpdate(task):
		if NetworkInterface.GetIsNetworkAvailable() and stringBuilder.Length > 0 and task.Result:
			try:
				json = Json.decode(Encoding.UTF8.GetString(updateWebClient.UploadData(Uri("https://api.twitter.com/1.1/statuses/update.json"), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Concat("status=", urlEncode(stringBuilder.ToString()))))))

				if json is not None and verifyDictionary.ContainsKey("name") and verifyDictionary.ContainsKey("password"):
					if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
						if json.ContainsKey("created_at") and json.ContainsKey("id_str") and json.ContainsKey("user"):
							if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json["user"]):
								if json["user"].ContainsKey("screen_name"):
									sb = StringBuilder(String.Format("http://social.apricotan.net/ping?resource={0}&title={1}&created={2}&modified={3}", urlEncode(String.Format("https://twitter.com/{0}/status/{1}", json["user"]["screen_name"], json["id_str"])), urlEncode(stringBuilder.ToString()), urlEncode(DateTime.ParseExact(json["created_at"], "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))))

									if json["user"].ContainsKey("profile_image_url"):
										imageUrl = json["user"]["profile_image_url"]

										if imageUrl is not None:
											sb.AppendFormat("&image={0}", urlEncode(imageUrl))

									pingWebClient = WebClient()
									pingWebClient.Headers.Add(HttpRequestHeader.Authorization, String.Concat("Basic ", Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(verifyDictionary["name"], ":", verifyDictionary["password"])))))
									pingWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")
									pingWebClient.UploadData(Uri(sb.ToString()), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Empty))

									request = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
									request.Method = WebRequestMethods.Http.Get
									response = None
									stream = None
									streamReader = None

									try:
										response = request.GetResponse()
										stream = response.GetResponseStream()
										streamReader = StreamReader(stream)
										json = Json.decode(streamReader.ReadToEnd())

										if json is not None:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
												if json.ContainsKey("user"):
													user = json["user"]
										
													if user is not None:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
															for kvp in user:
																if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
																	userDictionary.Add(kvp.Key, kvp.Value)

									finally:
										if streamReader is not None:
											streamReader.Close()

										if stream is not None:
											stream.Close()
								
										if response is not None:
											response.Close()

									dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

									for likesRequest in likesRequestList:
										response = None
										stream = None
										streamReader = None

										try:
											response = likesRequest.GetResponse()
											stream = response.GetResponseStream()
											streamReader = StreamReader(stream)
											json = Json.decode(streamReader.ReadToEnd())

											if json is not None:
												if clr.GetClrType(Array).IsInstanceOfType(json):
													for obj in json:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
															if obj.ContainsKey("like"):
																like = obj["like"]
													
																if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
																	if like.ContainsKey("character") and like.ContainsKey("created"):
																		character = like["character"]

																		if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
																			created = DateTime.Parse(like["created"])
																
																			if character.ContainsKey("name") and created > dt:
																				if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																					if currentLikesDictionary.ContainsKey(character["name"]):
																						currentLikesDictionary[character["name"]] += 1
																					else:
																						currentLikesDictionary.Add(character["name"], 1)

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
		global remainingCount, likesDictionary

		if userDictionary.ContainsKey("points"):
			remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))

		if currentLikesDictionary.Count > 0:
			likesDictionary.Clear()

			for kvp in currentLikesDictionary:
				likesDictionary.Add(kvp.Key, kvp.Value)

			for kvp in likesDictionary:
				sequenceList = List[Sequence]()
								
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
						sequenceList.Add(sequence)

				Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.ToString(CultureInfo.InvariantCulture)))

	Task.Factory.StartNew(onLoad, TaskCreationOptions.LongRunning).ContinueWith(onLoaded, context).ContinueWith[NaiveBayes](onTrain, TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[NaiveBayes]](onTrained), context).ContinueWith(onVerify, TaskContinuationOptions.LongRunning).ContinueWith[Boolean](onReady, context).ContinueWith(Action[Task[Boolean]](onUpdate), TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

def onTick(timer, e):
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret
	entryList = List[Entry]()
	wordList = List[Word]()

	if (username is not None and password is not None) or String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
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

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
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
						if streamReader is not None:
							streamReader.Close()

						if stream is not None:
							stream.Close()
			
						if response is not None:
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

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
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
			global autoUpdate, dateTime, recentEntryList, recentWordList

			if entryList.Count > 0:
				newEntryList = List[Entry]()
				nowDateTime = DateTime.Now
				dt = DateTime(0)

				for entry in entryList:
					if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title) and entry.Modified > dateTime and entry.Modified <= nowDateTime:
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
					if not hashSet.Contains(entry.Resource) and hashSet.Count < 10:
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

	else:
		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("oauth_token", oauthToken)

		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

		sortedDictionary.Add("realm", "http://api.twitter.com/")
		sortedDictionary.Add("oauth_signature", signature)

		verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
		verifyRequest.PreAuthenticate = True
		verifyRequest.Method = WebRequestMethods.Http.Get
		verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
		verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
		likesRequestList = List[WebRequest]()
		verifyDictionary = Dictionary[String, String]()
		userDictionary = Dictionary[String, Double]()
		currentLikesDictionary = Dictionary[String, Int32]()
		context = TaskScheduler.FromCurrentSynchronizationContext()

		def onVerify():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					response = None
					stream = None
					streamReader = None

					try:
						response = verifyRequest.GetResponse()
						
						if response.StatusCode == HttpStatusCode.OK:
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("user"):
										user = json["user"]
										
										if user is not None:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
												if user.ContainsKey("name"):
													if clr.GetClrType(String).IsInstanceOfType(user["name"]):
														verifyDictionary.Add("name", user["name"])

												if user.ContainsKey("password"):
													if clr.GetClrType(String).IsInstanceOfType(user["password"]):
														verifyDictionary.Add("password", user["password"])

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

		def onReady(task):
			global username, password

			for kvp in verifyDictionary:
				if kvp.Key.Equals("name"):
					username = kvp.Value
				elif kvp.Key.Equals("password"):
					password = kvp.Value

			if username is not None:
				for character in Script.Instance.Characters:
					likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
					likesRequest.Method = WebRequestMethods.Http.Get

					likesRequestList.Add(likesRequest)

		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					if verifyDictionary.ContainsKey("name"):
						request = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
						request.Method = WebRequestMethods.Http.Get
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("user"):
										user = json["user"]
										
										if user is not None:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
												for kvp in user:
													if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
														userDictionary.Add(kvp.Key, kvp.Value)

						finally:
							if streamReader is not None:
								streamReader.Close()

							if stream is not None:
								stream.Close()
								
							if response is not None:
								response.Close()

					dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

					for likesRequest in likesRequestList:
						response = None
						stream = None
						streamReader = None

						try:
							response = likesRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											if obj.ContainsKey("like"):
												like = obj["like"]
													
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
													if like.ContainsKey("character") and like.ContainsKey("created"):
														character = like["character"]

														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
															created = DateTime.Parse(like["created"])
																
															if character.ContainsKey("name") and created > dt:
																if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																	if currentLikesDictionary.ContainsKey(character["name"]):
																		currentLikesDictionary[character["name"]] += 1
																	else:
																		currentLikesDictionary.Add(character["name"], 1)

						finally:
							if streamReader is not None:
								streamReader.Close()

							if stream is not None:
								stream.Close()
			
							if response is not None:
								response.Close()

					request = WebRequest.Create("http://social.apricotan.net/entries?format=json&limit=25")
					response = None
					stream = None
					streamReader = None

					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						json = Json.decode(streamReader.ReadToEnd())

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
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
						if streamReader is not None:
							streamReader.Close()

						if stream is not None:
							stream.Close()
			
						if response is not None:
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

						if json is not None:
							if clr.GetClrType(Array).IsInstanceOfType(json):
								for obj in json:
									if obj is not None:
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
			global remainingCount, likesDictionary, autoUpdate, dateTime, recentEntryList, recentWordList

			if userDictionary.ContainsKey("points"):
				remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))

			if currentLikesDictionary.Count > 0:
				likesDictionary.Clear()

				for kvp in currentLikesDictionary:
					likesDictionary.Add(kvp.Key, kvp.Value)

				for kvp in likesDictionary:
					sequenceList = List[Sequence]()
								
					for sequence in Script.Instance.Sequences:
						if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
							sequenceList.Add(sequence)

					Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.ToString(CultureInfo.InvariantCulture)))
				
			if entryList.Count > 0:
				newEntryList = List[Entry]()
				nowDateTime = DateTime.Now
				dt = DateTime(0)

				for entry in entryList:
					if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title) and entry.Modified > dateTime and entry.Modified <= nowDateTime:
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
					if not hashSet.Contains(entry.Resource) and hashSet.Count < 10:
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
			
		Task.Factory.StartNew(onVerify, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(10)
	timer.Start()

def onPing(s, e):
	global username, password, consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	entry = s.Tag
		
	if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title):
		stringBuilder = StringBuilder()
		newWordList = List[Dictionary[String, Object]]()
		entryList = List[Entry]()
		wordList = List[Word]()

		if entry.Author is None:
			stringBuilder.AppendFormat("http://social.apricotan.net/ping?resource={0}&title={1}&created={2}&modified={3}", urlEncode(entry.Resource.ToString()), urlEncode(entry.Title), urlEncode(entry.Created.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")))
		else:
			stringBuilder.AppendFormat("http://social.apricotan.net/ping?resource={0}&title={1}&author={2}&created={3}&modified={4}", urlEncode(entry.Resource.ToString()), urlEncode(entry.Title), urlEncode(entry.Author), urlEncode(entry.Created.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")))
			
		if entry.Image is not None:
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

		if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
			def onUpdate():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						bytes = None

						if newWordList.Count > 0:
							json = Json.encode(newWordList.ToArray())

							if json is not None:
								bytes = Encoding.UTF8.GetBytes(json)

						client = WebClient()
						client.Headers.Add(HttpRequestHeader.ContentType, "application/json")
					
						if bytes is None:
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

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if obj is not None:
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
							if streamReader is not None:
								streamReader.Close()

							if stream is not None:
								stream.Close()
			
							if response is not None:
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

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if obj is not None:
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
				global autoUpdate, dateTime, recentEntryList, recentWordList

				if entryList.Count > 0:
					newEntryList = List[Entry]()
					nowDateTime = DateTime.Now
					dt = DateTime(0)

					for entry in entryList:
						if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title) and entry.Modified > dateTime and entry.Modified <= nowDateTime:
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
						if not hashSet.Contains(entry.Resource) and hashSet.Count < 10:
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

		else:
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

			sortedDictionary.Add("realm", "http://api.twitter.com/")
			sortedDictionary.Add("oauth_signature", signature)

			verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
			verifyRequest.PreAuthenticate = True
			verifyRequest.Method = WebRequestMethods.Http.Get
			verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
			verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
			pingRequest = WebRequest.Create(stringBuilder.ToString())
			pingRequest.PreAuthenticate = True
			pingRequest.Method = WebRequestMethods.Http.Post
			pingRequest.ContentType = "application/json"

			verifyDictionary = Dictionary[String, String]()
			userDictionary = Dictionary[String, Double]()
			context = TaskScheduler.FromCurrentSynchronizationContext()

			def onVerify():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						response = None
						stream = None
						streamReader = None

						try:
							response = verifyRequest.GetResponse()
						
							if response.StatusCode == HttpStatusCode.OK:
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("user"):
											user = json["user"]
										
											if user is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
													if user.ContainsKey("name"):
														if clr.GetClrType(String).IsInstanceOfType(user["name"]):
															verifyDictionary.Add("name", user["name"])

													if user.ContainsKey("password"):
														if clr.GetClrType(String).IsInstanceOfType(user["password"]):
															verifyDictionary.Add("password", user["password"])

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

			def onReady(task):
				global username, password

				for kvp in verifyDictionary:
					if kvp.Key.Equals("name"):
						username = kvp.Value
					elif kvp.Key.Equals("password"):
						password = kvp.Value

				if username is not None and password is not None:
					pingRequest.Headers.Add(HttpRequestHeader.Authorization, String.Concat("Basic ", Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(username, ":", password)))))

			def onUpdate(task):
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						response = None
						stream = None
						bytes = None

						if newWordList.Count > 0:
							json = Json.encode(newWordList.ToArray())

							if json is not None:
								bytes = Encoding.UTF8.GetBytes(json)

						try:
							if bytes is None:
								pingRequest.ContentLength = 0

							else:
								stream = pingRequest.GetRequestStream()
								stream.Write(bytes, 0, bytes.Length)
						
							response = pingRequest.GetResponse()

						finally:
							if stream is not None:
								stream.Close()
			
							if response is not None:
								response.Close()

						if verifyDictionary.ContainsKey("name"):
							request = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
							request.Method = WebRequestMethods.Http.Get
							response = None
							stream = None
							streamReader = None

							try:
								response = request.GetResponse()
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("user"):
											user = json["user"]
										
											if user is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
													for kvp in user:
														if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
															userDictionary.Add(kvp.Key, kvp.Value)

							finally:
								if streamReader is not None:
									streamReader.Close()

								if stream is not None:
									stream.Close()
								
								if response is not None:
									response.Close()

						request = WebRequest.Create("http://social.apricotan.net/entries?format=json&limit=25")
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if obj is not None:
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
							if streamReader is not None:
								streamReader.Close()

							if stream is not None:
								stream.Close()
			
							if response is not None:
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

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if obj is not None:
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
				global remainingCount, autoUpdate, dateTime, recentEntryList, recentWordList

				if userDictionary.ContainsKey("points"):
					remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))
			
				if entryList.Count > 0:
					newEntryList = List[Entry]()
					nowDateTime = DateTime.Now
					dt = DateTime(0)

					for entry in entryList:
						if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title) and entry.Modified > dateTime and entry.Modified <= nowDateTime:
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
						if not hashSet.Contains(entry.Resource) and hashSet.Count < 10:
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

			Task.Factory.StartNew(onVerify, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)
			
def onOpened(s, e):
	global autoUpdate, remainingCount, menuItem, recentEntryList, recentWordList

	menuItem.Items.Clear()

	likeMenuItem = MenuItem()
	stackPanel = StackPanel()
	stackPanel.HorizontalAlignment = HorizontalAlignment.Left
	stackPanel.VerticalAlignment = VerticalAlignment.Top
	stackPanel.Orientation = Orientation.Horizontal

	textBlock1 = TextBlock()
	textBlock1.HorizontalAlignment = HorizontalAlignment.Left
	
	stackPanel.Children.Add(textBlock1)

	textBlock2 = TextBlock()
	textBlock2.HorizontalAlignment = HorizontalAlignment.Right
	textBlock2.Margin = Thickness(10, 0, 0, 0)
	
	stackPanel.Children.Add(textBlock2)
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		textBlock1.Text = "好感度を上げる"
		textBlock2.Text = "N/A" if remainingCount is None else String.Concat("残り ", remainingCount.ToString())

	else:
		textBlock1.Text = "Like"
		textBlock2.Text = "N/A" if remainingCount is None else String.Concat(remainingCount.ToString(), " remaining")

	likeMenuItem.Header = stackPanel

	dashboardMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		dashboardMenuItem.Header = "ダッシュボードを表示..."
	else:
		dashboardMenuItem.Header = "Dashboard..."

	askMenuItem = None
	
	for sequence in Script.Instance.Sequences:
		if sequence.Name.Equals("Greet") or sequence.Name.Equals("Hate") or sequence.Name.Equals("Interest") or sequence.Name.Equals("Thank") or sequence.Name.Equals("Ignore"):
			askMenuItem = MenuItem()

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				askMenuItem.Header = "話しかける..."
			else:
				askMenuItem.Header = "Ask..."

			break

	if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
		def onSignInWithTwitterClick(sender1, args1):
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

						verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
						verifyRequest.PreAuthenticate = True
						verifyRequest.Method = WebRequestMethods.Http.Get
						verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
						verifyDictionary = Dictionary[String, String]()
						likesRequestList = List[WebRequest]()
						userDictionary = Dictionary[String, Double]()
						currentLikesDictionary = Dictionary[String, Int32]()

						def onOAuthAccessToken():
							from System.IO import Path

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
															if Regex.IsMatch(lines, "\\#\\s*Social.py", RegexOptions.CultureInvariant):
																lines = Regex.Replace(lines, "oauthToken\\s*=\\s*None", String.Format("oauthToken = \"{0}\"", d["oauth_token"]), RegexOptions.CultureInvariant)
																lines = Regex.Replace(lines, "oauthTokenSecret\\s*=\\s*None", String.Format("oauthTokenSecret = \"{0}\"", d["oauth_token_secret"]), RegexOptions.CultureInvariant)
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

							if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
								sortedDictionary = createCommonParameters(consumerKey)
								sortedDictionary.Add("oauth_token", oauthToken)

								hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
								signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

								sortedDictionary.Add("realm", "http://api.twitter.com/")
								sortedDictionary.Add("oauth_signature", signature)

								verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))

						def onVerify(task):
							if NetworkInterface.GetIsNetworkAvailable():
								try:
									response = None
									stream = None
									streamReader = None

									try:
										response = verifyRequest.GetResponse()
						
										if response.StatusCode == HttpStatusCode.OK:
											stream = response.GetResponseStream()
											streamReader = StreamReader(stream)
											json = Json.decode(streamReader.ReadToEnd())

											if json is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
													if json.ContainsKey("user"):
														user = json["user"]
										
														if user is not None:
															if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
																if user.ContainsKey("name"):
																	if clr.GetClrType(String).IsInstanceOfType(user["name"]):
																		verifyDictionary.Add("name", user["name"])

																if user.ContainsKey("password"):
																	if clr.GetClrType(String).IsInstanceOfType(user["password"]):
																		verifyDictionary.Add("password", user["password"])

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

						def onReady(task):
							global username, password

							for kvp in verifyDictionary:
								if kvp.Key.Equals("name"):
									username = kvp.Value
								elif kvp.Key.Equals("password"):
									password = kvp.Value

							if username is not None:
								for character in Script.Instance.Characters:
									likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
									likesRequest.Method = WebRequestMethods.Http.Get

									likesRequestList.Add(likesRequest)

						def onUpdate(task):
							if NetworkInterface.GetIsNetworkAvailable():
								try:
									if verifyDictionary.ContainsKey("name"):
										request = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
										request.Method = WebRequestMethods.Http.Get
										response = None
										stream = None
										streamReader = None

										try:
											response = request.GetResponse()
											stream = response.GetResponseStream()
											streamReader = StreamReader(stream)
											json = Json.decode(streamReader.ReadToEnd())

											if json is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
													if json.ContainsKey("user"):
														user = json["user"]
										
														if user is not None:
															if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
																for kvp in user:
																	if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
																		userDictionary.Add(kvp.Key, kvp.Value)

										finally:
											if streamReader is not None:
												streamReader.Close()

											if stream is not None:
												stream.Close()
								
											if response is not None:
												response.Close()

									dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

									for likesRequest in likesRequestList:
										response = None
										stream = None
										streamReader = None

										try:
											response = likesRequest.GetResponse()
											stream = response.GetResponseStream()
											streamReader = StreamReader(stream)
											json = Json.decode(streamReader.ReadToEnd())

											if json is not None:
												if clr.GetClrType(Array).IsInstanceOfType(json):
													for obj in json:
														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
															if obj.ContainsKey("like"):
																like = obj["like"]
													
																if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
																	if like.ContainsKey("character") and like.ContainsKey("created"):
																		character = like["character"]

																		if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
																			created = DateTime.Parse(like["created"])
																
																			if character.ContainsKey("name") and created > dt:
																				if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																					if currentLikesDictionary.ContainsKey(character["name"]):
																						currentLikesDictionary[character["name"]] += 1
																					else:
																						currentLikesDictionary.Add(character["name"], 1)

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
							global remainingCount, likesDictionary

							if userDictionary.ContainsKey("points"):
								remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))

							if currentLikesDictionary.Count > 0:
								likesDictionary.Clear()

								for kvp in currentLikesDictionary:
									likesDictionary.Add(kvp.Key, kvp.Value)

								for kvp in likesDictionary:
									sequenceList = List[Sequence]()
								
									for sequence in Script.Instance.Sequences:
										if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
											sequenceList.Add(sequence)

									Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.ToString(CultureInfo.InvariantCulture)))
						
						Task.Factory.StartNew(onOAuthAccessToken, TaskCreationOptions.LongRunning).ContinueWith(onOAuthAccessTokenCompleted, context).ContinueWith(onVerify, TaskContinuationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

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

		signInWithTwitterMenuItem = MenuItem()
	
		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			signInWithTwitterMenuItem.Header = "Twitterアカウントでサインイン..."
		else:
			signInWithTwitterMenuItem.Header = "Sign in with Twitter..."

		signInWithTwitterMenuItem.Click += onSignInWithTwitterClick

		menuItem.Items.Add(signInWithTwitterMenuItem)
		menuItem.Items.Add(Separator())

		likeMenuItem.IsEnabled = False

		menuItem.Items.Add(likeMenuItem)
		menuItem.Items.Add(Separator())

		dashboardMenuItem.IsEnabled = False

		menuItem.Items.Add(dashboardMenuItem)
		menuItem.Items.Add(Separator())

		if askMenuItem is not None:
			askMenuItem.IsEnabled = False

			menuItem.Items.Add(askMenuItem)
			menuItem.Items.Add(Separator())

	else:
		def onLikeClick(sender1, args1):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
		
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

			sortedDictionary.Add("realm", "http://api.twitter.com/")
			sortedDictionary.Add("oauth_signature", signature)

			verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
			verifyRequest.PreAuthenticate = True
			verifyRequest.Method = WebRequestMethods.Http.Get
			verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
			verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
			likeRequestList = List[WebRequest]()
			likesRequestList = List[WebRequest]()
			verifyDictionary = Dictionary[String, String]()
			userDictionary = Dictionary[String, Double]()
			currentLikesDictionary = Dictionary[String, Int32]()
			context = TaskScheduler.FromCurrentSynchronizationContext()

			def onVerify():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						response = None
						stream = None
						streamReader = None

						try:
							response = verifyRequest.GetResponse()
						
							if response.StatusCode == HttpStatusCode.OK:
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("user"):
											user = json["user"]
										
											if user is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
													if user.ContainsKey("name"):
														if clr.GetClrType(String).IsInstanceOfType(user["name"]):
															verifyDictionary.Add("name", user["name"])

													if user.ContainsKey("password"):
														if clr.GetClrType(String).IsInstanceOfType(user["password"]):
															verifyDictionary.Add("password", user["password"])

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

			def onReady(task):
				global username, password

				for kvp in verifyDictionary:
					if kvp.Key.Equals("name"):
						username = kvp.Value
					elif kvp.Key.Equals("password"):
						password = kvp.Value

				if username is not None and password is not None:
					for character in Script.Instance.Characters:
						likeRequest = WebRequest.Create(String.Format("http://social.apricotan.net/like?character_name={0}", urlEncode(character.Name)))
						likeRequest.PreAuthenticate = True
						likeRequest.Method = WebRequestMethods.Http.Post
						likeRequest.Headers.Add(HttpRequestHeader.Authorization, String.Concat("Basic ", Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat(username, ":", password)))))
						likeRequest.ContentLength = 0

						likeRequestList.Add(likeRequest)

						likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
						likesRequest.Method = WebRequestMethods.Http.Get

						likesRequestList.Add(likesRequest)

			def onUpdate(task):
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						response = None
						stream = None
						streamReader = None

						for likeRequest in likeRequestList:
							try:
								response = likeRequest.GetResponse()
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("like"):
											like = json["like"]
										
											if like is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
													if like.ContainsKey("user"):
														user = like["user"]

														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
															for kvp in user:
																if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
																	userDictionary.Add(kvp.Key, kvp.Value)

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

						dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

						for likesRequest in likesRequestList:
							try:
								response = likesRequest.GetResponse()
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Array).IsInstanceOfType(json):
										for obj in json:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
												if obj.ContainsKey("like"):
													like = obj["like"]
													
													if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
														if like.ContainsKey("character") and like.ContainsKey("created"):
															character = like["character"]

															if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
																created = DateTime.Parse(like["created"])
																
																if character.ContainsKey("name") and created > dt:
																	if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																		if currentLikesDictionary.ContainsKey(character["name"]):
																			currentLikesDictionary[character["name"]] += 1
																		else:
																			currentLikesDictionary.Add(character["name"], 1)

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
					
					except Exception, e:
						Trace.WriteLine(e.clsException.Message)
						Trace.WriteLine(e.clsException.StackTrace)

			def onCompleted(task):
				from System.Windows.Shapes import Path
				global remainingCount, likesDictionary

				try:
					if userDictionary.ContainsKey("points"):
						remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))
					
					if currentLikesDictionary.Count > 0:
						previous = 0
						current = 0
						likeSequenceList = List[Sequence]()

						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Like"):
								if sequence.State is not None:
									if likesDictionary.ContainsKey(sequence.Owner):
										for i in range(1, likesDictionary[sequence.Owner] + 1):
											if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
												previous += 1

												break

									if currentLikesDictionary.ContainsKey(sequence.Owner):
										for i in range(1, currentLikesDictionary[sequence.Owner] + 1):
											if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
												current += 1

												break

								likeSequenceList.Add(sequence)

						for kvp in currentLikesDictionary:
							sequenceList = List[Sequence]()
								
							for sequence in likeSequenceList:
								if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
									sequenceList.Add(sequence)

							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.ToString(CultureInfo.InvariantCulture)))

						likesDictionary.Clear()

						for kvp in currentLikesDictionary:
							likesDictionary.Add(kvp.Key, kvp.Value)

						window = Window()
						closeTimer = DispatcherTimer(DispatcherPriority.Background)
						contentControl = ContentControl()
						border1 = Border()
		
						def onClose(sender2, args2):
							closeTimer.Stop()

							storyboard = Storyboard()
							da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
							da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
							da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
							sineEase = SineEase()

							sineEase.EasingMode = EasingMode.EaseIn
							da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

							def onCurrentStateInvalidated(sender4, args4):
								if sender4.CurrentState == ClockState.Filling:
									contentControl.Opacity = 0
									contentControl.RenderTransform.ScaleX = 1.5
									contentControl.RenderTransform.ScaleY = 1.5
									storyboard.Remove(contentControl)
									window.Close()

							storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
							storyboard.Children.Add(da1)
							storyboard.Children.Add(da2)
							storyboard.Children.Add(da3)

							Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
							Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
							Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
							contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

						def onLoaded(sender3, args3):
							border1.Width = contentControl.ActualWidth if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight
							border1.Height = contentControl.ActualWidth if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight
							contentControl.Width = contentControl.ActualWidth * 1.5 if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight * 1.5 
							contentControl.Height = contentControl.ActualWidth * 1.5 if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight * 1.5
							contentControl.RenderTransform.CenterX = contentControl.Width / 2
							contentControl.RenderTransform.CenterY = contentControl.Height / 2

							storyboard = Storyboard()
							da1 = DoubleAnimation(contentControl.Opacity, 1, TimeSpan.FromMilliseconds(500))
							da2 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
							da3 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
							sineEase = SineEase()

							sineEase.EasingMode = EasingMode.EaseOut
							da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

							def onCurrentStateInvalidated(sender4, args4):
								if sender4.CurrentState == ClockState.Filling:
									contentControl.Opacity = 1
									contentControl.RenderTransform.ScaleX = 1
									contentControl.RenderTransform.ScaleY = 1
									storyboard.Remove(contentControl)
									closeTimer.Start()

							storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
							storyboard.Children.Add(da1)
							storyboard.Children.Add(da2)
							storyboard.Children.Add(da3)

							Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
							Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
							Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

							contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

						closeTimer.Tick += onClose
						closeTimer.Interval = TimeSpan.FromSeconds(3)

						contentControl.UseLayoutRounding = True
						contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
						contentControl.VerticalAlignment = VerticalAlignment.Stretch
						contentControl.Opacity = 0
						contentControl.RenderTransform = ScaleTransform(1, 1)

						solidColorBrush1 = SolidColorBrush(Colors.Black)
						solidColorBrush1.Opacity = 0.75

						if solidColorBrush1.CanFreeze:
							solidColorBrush1.Freeze()

						border1.HorizontalAlignment = HorizontalAlignment.Center
						border1.VerticalAlignment = VerticalAlignment.Center
						border1.Padding = Thickness(4)
						border1.CornerRadius = CornerRadius(4)
						border1.Background = solidColorBrush1

						stackPanel1 = StackPanel()
						stackPanel1.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel1.VerticalAlignment = VerticalAlignment.Center
						stackPanel1.Orientation = Orientation.Vertical
						stackPanel1.Background = Brushes.Transparent

						stackPanel2 = StackPanel()
						stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel2.VerticalAlignment = VerticalAlignment.Center
						stackPanel2.Margin = Thickness(0, 4, 0, 0)
						stackPanel2.Orientation = Orientation.Horizontal
						stackPanel2.Background = Brushes.Transparent

						solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 204, 0, 0))

						if solidColorBrush2.CanFreeze:
							solidColorBrush2.Freeze()

						fontSizeConverter = FontSizeConverter()

						for kvp in likesDictionary:
							stackPanel3 = StackPanel()
							stackPanel3.HorizontalAlignment = HorizontalAlignment.Center
							stackPanel3.VerticalAlignment = VerticalAlignment.Stretch
							stackPanel3.Margin = Thickness(4, 0, 4, 0)
							stackPanel3.Orientation = Orientation.Vertical
							stackPanel3.Background = Brushes.Transparent

							stackPanel4 = StackPanel()
							stackPanel4.HorizontalAlignment = HorizontalAlignment.Stretch
							stackPanel4.VerticalAlignment = VerticalAlignment.Center
							stackPanel4.Orientation = Orientation.Horizontal
							stackPanel4.Background = Brushes.Transparent

							likeGeometry = createLikeGeometry(Rect(0, 0, 20, 15))

							if likeGeometry.CanFreeze:
								likeGeometry.Freeze()

							path = Path()
							path.HorizontalAlignment = HorizontalAlignment.Stretch
							path.VerticalAlignment = VerticalAlignment.Center
							path.Fill = solidColorBrush2
							path.Data = likeGeometry

							stackPanel4.Children.Add(path)

							border2 = Border()
							border2.CacheMode = BitmapCache(1)
							border2.HorizontalAlignment = HorizontalAlignment.Stretch
							border2.VerticalAlignment = VerticalAlignment.Stretch
							border2.Margin = Thickness(0)
							border2.Padding = Thickness(0)
							border2.CornerRadius = CornerRadius(0)
							border2.Background = Brushes.Transparent
						
							dropShadowEffect = DropShadowEffect()
							dropShadowEffect.BlurRadius = 1
							dropShadowEffect.Color = Colors.Black
							dropShadowEffect.Direction = 270
							dropShadowEffect.Opacity = 0.5
							dropShadowEffect.ShadowDepth = 1

							if dropShadowEffect.CanFreeze:
								dropShadowEffect.Freeze()

							border2.Effect = dropShadowEffect

							label1 = Label()
							label1.HorizontalAlignment = HorizontalAlignment.Stretch
							label1.VerticalAlignment = VerticalAlignment.Stretch
							label1.FontSize = fontSizeConverter.ConvertFromString("24pt")
							label1.Foreground = Brushes.White
							label1.FontWeight = FontWeights.Bold
							label1.Content = kvp.Value.ToString()

							RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)

							border2.Child = label1
							stackPanel4.Children.Add(border2)

							border3 = Border()
							border3.CacheMode = BitmapCache(1)
							border3.HorizontalAlignment = HorizontalAlignment.Stretch
							border3.VerticalAlignment = VerticalAlignment.Stretch
							border3.Margin = Thickness(0)
							border3.Padding = Thickness(0)
							border3.CornerRadius = CornerRadius(0)
							border3.Background = Brushes.Transparent
							border3.Effect = dropShadowEffect
	
							label2 = Label()
							label2.HorizontalAlignment = HorizontalAlignment.Stretch
							label2.VerticalAlignment = VerticalAlignment.Stretch
							label2.FontSize = fontSizeConverter.ConvertFromString("8pt")
							label2.Foreground = Brushes.White
							label2.FontWeight = FontWeights.Normal
							label2.Content = kvp.Key

							RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)

							border3.Child = label2
	
							stackPanel3.Children.Add(stackPanel4)
							stackPanel3.Children.Add(border3)
							stackPanel2.Children.Add(stackPanel3)

						border4 = Border()
						border4.CacheMode = BitmapCache(1)
						border4.HorizontalAlignment = HorizontalAlignment.Center
						border4.VerticalAlignment = VerticalAlignment.Stretch
						border4.Margin = Thickness(0, 0, 0, 4)
						border4.Padding = Thickness(0)
						border4.CornerRadius = CornerRadius(0)
						border4.Background = Brushes.Transparent

						dropShadowEffect = DropShadowEffect()
						dropShadowEffect.BlurRadius = 1
						dropShadowEffect.Color = Colors.Black
						dropShadowEffect.Direction = 270
						dropShadowEffect.Opacity = 0.5
						dropShadowEffect.ShadowDepth = 1

						if dropShadowEffect.CanFreeze:
							dropShadowEffect.Freeze()

						border4.Effect = dropShadowEffect

						label4 = Label()
						label4.HorizontalAlignment = HorizontalAlignment.Stretch
						label4.VerticalAlignment = VerticalAlignment.Stretch
						label4.FontSize = fontSizeConverter.ConvertFromString("8pt")
						label4.Foreground = Brushes.White
						label4.FontWeight = FontWeights.Normal

						if previous < current:
							if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
								label4.Content = "アイテムのロックが解除されました！"
							else:
								label4.Content = "Unlocked new services."

						else:
							if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
								label4.Content = "好感度が上がりました！"
							else:
								label4.Content = "Likes increased."
					
						RenderOptions.SetClearTypeHint(label4, ClearTypeHint.Enabled)

						border4.Child = label4
					
						stackPanel1.Children.Add(stackPanel2)
						stackPanel1.Children.Add(border4)
						border1.Child = stackPanel1
						contentControl.Content = border1

						window.Owner = Application.Current.MainWindow
						window.Title = Application.Current.MainWindow.Title
						window.WindowStartupLocation = WindowStartupLocation.CenterOwner
						window.AllowsTransparency = True
						window.WindowStyle = WindowStyle.None
						window.ResizeMode = ResizeMode.NoResize
						window.ShowActivated = False
						window.Topmost = True
						window.SizeToContent = SizeToContent.WidthAndHeight
						window.Background = Brushes.Transparent
						window.Content = contentControl
						window.Loaded += onLoaded
						window.Show()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			Task.Factory.StartNew(onVerify, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

		if remainingCount is None:
			likeMenuItem.IsEnabled = False
		else:
			if remainingCount > 0:
				textBlock2.Foreground = SystemColors.GrayTextBrush
			else:
				likeMenuItem.IsEnabled = False

		likeMenuItem.Click += onLikeClick

		menuItem.Items.Add(likeMenuItem)
		menuItem.Items.Add(Separator())

		def onDashboardClick(sender1, args1):
			global consumerKey, consumerSecret, oauthToken, oauthTokenSecret
		
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

			sortedDictionary.Add("realm", "http://api.twitter.com/")
			sortedDictionary.Add("oauth_signature", signature)

			verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
			verifyRequest.PreAuthenticate = True
			verifyRequest.Method = WebRequestMethods.Http.Get
			verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
			verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
			likesRequestList = List[WebRequest]()
			verifyDictionary = Dictionary[String, String]()
			currentLikesDictionary = Dictionary[String, List[DateTime]]()
			userDictionary = Dictionary[String, Double]()
			recentLikesDictionary = Dictionary[String, Int32]()
			context = TaskScheduler.FromCurrentSynchronizationContext()

			def onVerify():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						response = None
						stream = None
						streamReader = None

						try:
							response = verifyRequest.GetResponse()
						
							if response.StatusCode == HttpStatusCode.OK:
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("user"):
											user = json["user"]
										
											if user is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
													if user.ContainsKey("name"):
														if clr.GetClrType(String).IsInstanceOfType(user["name"]):
															verifyDictionary.Add("name", user["name"])

													if user.ContainsKey("password"):
														if clr.GetClrType(String).IsInstanceOfType(user["password"]):
															verifyDictionary.Add("password", user["password"])

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

			def onReady(task):
				global username, password

				for kvp in verifyDictionary:
					if kvp.Key.Equals("name"):
						username = kvp.Value
					elif kvp.Key.Equals("password"):
						password = kvp.Value

				if username is not None:
					for character in Script.Instance.Characters:
						likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
						likesRequest.Method = WebRequestMethods.Http.Get

						likesRequestList.Add(likesRequest)

			def onUpdate(task):
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

						for likesRequest in likesRequestList:
							response = None
							stream = None
							streamReader = None
					
							try:
								response = likesRequest.GetResponse()
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Array).IsInstanceOfType(json):
										for obj in json:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
												if obj.ContainsKey("like"):
													like = obj["like"]

													if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
														if like.ContainsKey("character") and like.ContainsKey("created"):
															character = like["character"]

															if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
																created = DateTime.Parse(like["created"])
																
																if character.ContainsKey("name") and created > dt:
																	if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																		if not currentLikesDictionary.ContainsKey(character["name"]):
																			currentLikesDictionary.Add(character["name"], List[DateTime]())

																		currentLikesDictionary[character["name"]].Add(created)

							finally:
								if streamReader is not None:
									streamReader.Close()

								if stream is not None:
									stream.Close()
								
								if response is not None:
									response.Close()

						if verifyDictionary.ContainsKey("name"):
							userRequest = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
							userRequest.Method = WebRequestMethods.Http.Get
							response = None
							stream = None
							streamReader = None

							try:
								response = userRequest.GetResponse()
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								json = Json.decode(streamReader.ReadToEnd())

								if json is not None:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
										if json.ContainsKey("user"):
											user = json["user"]
										
											if user is not None:
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
													for kvp in user:
														if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
															userDictionary.Add(kvp.Key, kvp.Value)

							finally:
								if streamReader is not None:
									streamReader.Close()

								if stream is not None:
									stream.Close()
								
								if response is not None:
									response.Close()

						topLikesRequest = WebRequest.Create("http://social.apricotan.net/likes?limit=100")
						response = None
						stream = None
						streamReader = None
					
						try:
							response = topLikesRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											if obj.ContainsKey("like"):
												like = obj["like"]

												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
													if like.ContainsKey("character") and like.ContainsKey("created"):
														character = like["character"]

														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character):
															if character.ContainsKey("name"):
																if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																	if recentLikesDictionary.ContainsKey(character["name"]):
																		recentLikesDictionary[character["name"]] += 1
																	else:
																		recentLikesDictionary.Add(character["name"], 1)

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
				from System.IO import Path
				global remainingCount, likesDictionary, username

				dateTime = DateTime.Today - TimeSpan(7 * 2 - 3, 0, 0, 0)
				limitDictionary = Dictionary[String, Int32]()
				available = 0
				max = 0
				lockedAchievementDictionary = Dictionary[String, Int32]()
				achievementList = List[String]()

				if userDictionary.ContainsKey("points"):
					remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))
				
				if currentLikesDictionary.Count > 0:
					likesDictionary.Clear()

					for kvp in currentLikesDictionary:
						likesDictionary.Add(kvp.Key, kvp.Value.Count)

						for dt in kvp.Value:
							if dt <= dateTime:
								if limitDictionary.ContainsKey(kvp.Key):
									limitDictionary[kvp.Key] += 1
								else:
									limitDictionary.Add(kvp.Key, 1)

					if limitDictionary.Count == 0:
						for key in currentLikesDictionary.Keys:
							limitDictionary.Add(key, 0)
					
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Like"):
						if likesDictionary.ContainsKey(sequence.Owner):
							isLocked = True
							requiredLikes = 0
							list = List[Object]()

							if sequence.State is None:
								available += 1
								isLocked = False
							else:
								for i in range(likesDictionary[sequence.Owner] + 11):
									if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
										if i <= likesDictionary[sequence.Owner]:
											available += 1
											isLocked = False
										else:
											requiredLikes = i - likesDictionary[sequence.Owner]

										break

							for obj in sequence:
								list.Add(obj)

							for i in range(0, list.Count):
								if clr.GetClrType(Sequence).IsInstanceOfType(list[i]):
									if list[i].State is None and list[i].Any():
										for j in range(i + 1, list.Count):
											if clr.GetClrType(Sequence).IsInstanceOfType(list[j]):
												isAvailable = False

												if list[j].Any():
													queue = Queue[Sequence]()

													for obj in list[j]:
														if clr.GetClrType(Sequence).IsInstanceOfType(obj):
															queue.Enqueue(obj)

													while queue.Count > 0:
														tempSequence = queue.Dequeue()

														if tempSequence.Any():
															for obj in tempSequence:
																if clr.GetClrType(Sequence).IsInstanceOfType(obj):
																	queue.Enqueue(obj)

														elif list[i].Name.Equals(tempSequence.Name) and tempSequence.State is None:
															isAvailable = True

															break

												elif list[i].Name.Equals(list[j].Name) and list[j].State is None:
													isAvailable = True

												if isAvailable:
													if isLocked:
														if lockedAchievementDictionary.ContainsKey(list[i].Name):
															if requiredLikes > lockedAchievementDictionary[list[i].Name]:
																lockedAchievementDictionary[list[i].Name] = requiredLikes
														else:
															lockedAchievementDictionary.Add(list[i].Name, requiredLikes)

													if not achievementList.Contains(list[i].Name):
														achievementList.Add(list[i].Name)

													break

							max += 1

				achievementList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.CurrentCulture))

				config = None
				directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name)
				backgroundBrush = None
				textColor = SystemColors.ControlTextBrush
		
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
					if config.AppSettings.Settings["BackgroundImage"] is not None:
						fs = None
						bi = BitmapImage()

						try:
							fs = FileStream(config.AppSettings.Settings["BackgroundImage"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["BackgroundImage"].Value), FileMode.Open, FileAccess.Read, FileShare.Read)

							bi.BeginInit()
							bi.StreamSource = fs
							bi.CacheOption = BitmapCacheOption.OnLoad
							bi.CreateOptions = BitmapCreateOptions.None
							bi.EndInit()

						finally:
							if fs is not None:
								fs.Close()

						backgroundBrush = ImageBrush(bi)
						backgroundBrush.TileMode = TileMode.Tile
						backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
						backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
						backgroundBrush.Stretch = Stretch.None

						if backgroundBrush.CanFreeze:
							backgroundBrush.Freeze()

					if backgroundBrush is None and config.AppSettings.Settings["BackgroundColor"] is not None:
						if config.AppSettings.Settings["BackgroundColor"].Value.Length > 0:
							backgroundBrush = SolidColorBrush(ColorConverter.ConvertFromString(config.AppSettings.Settings["BackgroundColor"].Value))

							if backgroundBrush.CanFreeze:
								backgroundBrush.Freeze()

					if config.AppSettings.Settings["TextColor"] is not None:
						if config.AppSettings.Settings["TextColor"].Value.Length > 0:
							textColor = ColorConverter.ConvertFromString(config.AppSettings.Settings["TextColor"].Value)
				
				textBrush = SolidColorBrush(textColor)

				if textBrush.CanFreeze:
					textBrush.Freeze()

				window = Window()
				leftStackPanel = StackPanel()
				rightStackPanel = StackPanel()
				
				def onLoaded(sender2, args2):
					if rightStackPanel.Children.Count > 0:
						leftStackPanel.Width = rightStackPanel.Width = Math.Ceiling(Math.Max(leftStackPanel.ActualWidth, rightStackPanel.ActualWidth))
					else:
						rightStackPanel.Margin = Thickness(5, 0, 0, 0)
						leftStackPanel.Width = Math.Ceiling(leftStackPanel.ActualWidth)

				contentControl = ContentControl()
				contentControl.UseLayoutRounding = True
				contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
				contentControl.VerticalAlignment = VerticalAlignment.Stretch

				stackPanel1 = StackPanel()
				stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel1.Orientation = Orientation.Vertical

				stackPanel2 = StackPanel()
				stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
				stackPanel2.VerticalAlignment = VerticalAlignment.Center
				stackPanel2.Orientation = Orientation.Vertical
				stackPanel2.Background = SystemColors.ControlBrush if backgroundBrush is None else backgroundBrush

				solidColorBrush1 = SolidColorBrush(Colors.Black)
				solidColorBrush1.Opacity = 0.25

				if solidColorBrush1.CanFreeze:
					solidColorBrush1.Freeze()

				border1 = Border()
				border1.HorizontalAlignment = HorizontalAlignment.Stretch
				border1.VerticalAlignment = VerticalAlignment.Stretch
				border1.Margin = Thickness(0, 0, 0, 0)
				border1.BorderThickness = Thickness(0, 0, 0, 1)
				border1.BorderBrush = solidColorBrush1

				stackPanel3 = StackPanel()
				stackPanel3.HorizontalAlignment = HorizontalAlignment.Center
				stackPanel3.VerticalAlignment = VerticalAlignment.Center
				stackPanel3.Orientation = Orientation.Horizontal

				leftStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
				leftStackPanel.VerticalAlignment = VerticalAlignment.Stretch
				leftStackPanel.Margin = Thickness(10, 10, 5, 10)
				leftStackPanel.Orientation = Orientation.Vertical

				rightStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
				rightStackPanel.VerticalAlignment = VerticalAlignment.Stretch
				rightStackPanel.Margin = Thickness(5, 10, 10, 10)
				rightStackPanel.Orientation = Orientation.Vertical

				solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 204, 0, 0))

				if solidColorBrush2.CanFreeze:
					solidColorBrush2.Freeze()

				solidColorBrush3 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 102, 102, 102))

				if solidColorBrush3.CanFreeze:
					solidColorBrush3.Freeze()

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					leftStackPanel.Children.Add(createSectionStackPanel(textColor, "ステータス"))

					if max > 0:
						rate = 100.0 * available / max
						sp = createStackPanelWithProgressBar(textColor, "アイテムの解除率", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)

						for element in sp.Children:
							element.BorderThickness = Thickness(1, 1, 1, 1)

						leftStackPanel.Children.Add(sp)

					else:
						sp = createStackPanel(textColor, "アイテムの解除率", "N/A")

						for element in sp.Children:
							element.BorderThickness = Thickness(1, 1, 1, 1)

						leftStackPanel.Children.Add(sp)
					
					for achievement in achievementList:
						if lockedAchievementDictionary.ContainsKey(achievement):
							if lockedAchievementDictionary[achievement] > 0:
								leftStackPanel.Children.Add(createStackPanelWithHint(textColor, achievement, "未解除", String.Format("解除に必要な好感度は残り{0}", lockedAchievementDictionary[achievement].ToString())))
							else:
								leftStackPanel.Children.Add(createStackPanelWithHint(textColor, achievement, "未解除", String.Format("解除には10以上の好感度が必要", lockedAchievementDictionary[achievement].ToString())))
						else:
							leftStackPanel.Children.Add(createStackPanel(textColor, achievement, "解除済み"))

					for character in Script.Instance.Characters:
						leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("{0}の現在の好感度", character.Name), likesDictionary[character.Name].ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2))

					if limitDictionary.Count > 0:
						for kvp in limitDictionary:
							leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("3日以内に失う{0}の好感度", kvp.Key.ToString()), kvp.Value.ToString(), solidColorBrush3))
					else:
						for character in Script.Instance.Characters:
							leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("3日以内に失う{0}の好感度", character.Name), "N/A", solidColorBrush3))

					leftStackPanel.Children.Add(createStackPanel(textColor, "好感度を上げるための残りポイント", "N/A" if remainingCount is None else remainingCount.ToString()))
					leftStackPanel.Children.Add(createAnnotationStackPanel(textColor, "サインインしていません" if username is None else String.Format("{0}でサインイン中", username)))

					if recentLikesDictionary.Count > 0:
						sum = 0
						list = List[KeyValuePair[String, Double]]()

						for kvp in recentLikesDictionary:
							sum += kvp.Value
							list.Add(KeyValuePair[String, Double](kvp.Key, kvp.Value))

						list.Sort(lambda x, y: x.Value - y.Value)
						list.Reverse()

						rightStackPanel.Children.Add(createSectionStackPanel(textColor, "ランキング"))

						for kvp in list:
							sp = createStackPanelWithLike(textColor, kvp.Key.ToString(), String.Format("{0}%", (100 * kvp.Value / sum).ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2)
							
							if rightStackPanel.Children.Count == 1:
								for element in sp.Children:
									element.BorderThickness = Thickness(1, 1, 1, 1)

							rightStackPanel.Children.Add(sp)

						rightStackPanel.Children.Add(createAnnotationStackPanel(textColor, String.Format("{0}キャラクター", list.Count.ToString())))

				else:
					leftStackPanel.Children.Add(createSectionStackPanel(textColor, "Me"))

					if max > 0:
						rate = 100.0 * available / max
						sp = createStackPanelWithProgressBar(textColor, "Unlock rate of services", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)
					
						for element in sp.Children:
							element.BorderThickness = Thickness(1, 1, 1, 1)

						leftStackPanel.Children.Add(sp)

					else:
						sp = createStackPanel(textColor, "Unlock rate of services", "N/A")

						for element in sp.Children:
							element.BorderThickness = Thickness(1, 1, 1, 1)

						leftStackPanel.Children.Add(sp)

					for achievement in achievementList:
						if lockedAchievementDictionary.ContainsKey(achievement):
							leftStackPanel.Children.Add(createStackPanelWithHint(textColor, achievement, "Locked", String.Format("{0} {1} needed to unlock", lockedAchievementDictionary[achievement].ToString() if lockedAchievementDictionary[achievement] > 0 else "10+", "Likes" if lockedAchievementDictionary[achievement] > 1 else "Like")))
						else:
							leftStackPanel.Children.Add(createStackPanel(textColor, achievement, "Unlocked"))

					for character in Script.Instance.Characters:
						leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("Current Likes of {0}", character.Name), likesDictionary[character.Name].ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2))

					if limitDictionary.Count > 0:
						for kvp in limitDictionary:
							leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("Expired Likes of {0} within 3 days", kvp.Key.ToString()), kvp.Value.ToString(), solidColorBrush3))
					else:
						for character in Script.Instance.Characters:
							leftStackPanel.Children.Add(createStackPanelWithLike(textColor, String.Format("Expired Likes of {0} within 3 days", character.Name), "N/A", solidColorBrush3))

					leftStackPanel.Children.Add(createStackPanel(textColor, "Remaining points for Like action", "N/A" if remainingCount is None else remainingCount.ToString()))
					leftStackPanel.Children.Add(createAnnotationStackPanel(textColor, "You are not Signed in" if username is None else String.Format("Signed in as {0}", username)))

					if recentLikesDictionary.Count > 0:
						sum = 0
						list = List[KeyValuePair[String, Double]]()

						for kvp in recentLikesDictionary:
							sum += kvp.Value
							list.Add(KeyValuePair[String, Double](kvp.Key, kvp.Value))

						list.Sort(lambda x, y: x.Value - y.Value)
						list.Reverse()

						rightStackPanel.Children.Add(createSectionStackPanel(textColor, "Trends"))

						for kvp in list:
							sp = createStackPanelWithLike(textColor, kvp.Key.ToString(), String.Format("{0}%", (100 * kvp.Value / sum).ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2)

							if rightStackPanel.Children.Count == 1:
								for element in sp.Children:
									element.BorderThickness = Thickness(1, 1, 1, 1)

							rightStackPanel.Children.Add(sp)

						rightStackPanel.Children.Add(createAnnotationStackPanel(textColor, String.Format("{0} characters", list.Count.ToString()) if list.Count > 1 else String.Format("{0} character", list.Count.ToString())))

				stackPanel3.Children.Add(leftStackPanel)
				stackPanel3.Children.Add(rightStackPanel)
				border1.Child = stackPanel3
				stackPanel2.Children.Add(border1)
				stackPanel1.Children.Add(stackPanel2)

				def onCloseClick(sender2, args2):
					window.Close()

				solidColorBrush4 = SolidColorBrush(Colors.White)
				solidColorBrush4.Opacity = 0.5

				if solidColorBrush4.CanFreeze:
					solidColorBrush4.Freeze()

				border2 = Border()
				border2.HorizontalAlignment = HorizontalAlignment.Stretch
				border2.VerticalAlignment = VerticalAlignment.Stretch
				border2.Margin = Thickness(0, 0, 0, 0)
				border2.BorderThickness = Thickness(0, 1, 0, 0)
				border2.BorderBrush = solidColorBrush4

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
				stackPanel1.Children.Add(border2)
				contentControl.Content = stackPanel1
			
				window.Owner = Application.Current.MainWindow
				window.Title = Application.Current.MainWindow.Title
				window.WindowStartupLocation = WindowStartupLocation.CenterScreen
				window.ResizeMode = ResizeMode.NoResize
				window.SizeToContent = SizeToContent.WidthAndHeight
				window.Background = SystemColors.ControlBrush
				window.Content = contentControl
				window.Loaded += onLoaded
				window.Show()

			Task.Factory.StartNew(onVerify, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

		dashboardMenuItem.Click += onDashboardClick

		menuItem.Items.Add(dashboardMenuItem)
		menuItem.Items.Add(Separator())

		if askMenuItem is not None:
			def onClick(sender, args):
				from System.IO import Path

				config = None
				directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name)
				backgroundBrush = None
				textColor = SystemColors.ControlTextBrush
			
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
					if config.AppSettings.Settings["BackgroundImage"] is not None:
						fs = None
						bi = BitmapImage()

						try:
							fs = FileStream(config.AppSettings.Settings["BackgroundImage"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["BackgroundImage"].Value), FileMode.Open, FileAccess.Read, FileShare.Read)

							bi.BeginInit()
							bi.StreamSource = fs
							bi.CacheOption = BitmapCacheOption.OnLoad
							bi.CreateOptions = BitmapCreateOptions.None
							bi.EndInit()

						finally:
							if fs is not None:
								fs.Close()

						backgroundBrush = ImageBrush(bi)
						backgroundBrush.TileMode = TileMode.Tile
						backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
						backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
						backgroundBrush.Stretch = Stretch.None

						if backgroundBrush.CanFreeze:
							backgroundBrush.Freeze()

					if backgroundBrush is None and config.AppSettings.Settings["BackgroundColor"] is not None:
						if config.AppSettings.Settings["BackgroundColor"].Value.Length > 0:
							backgroundBrush = SolidColorBrush(ColorConverter.ConvertFromString(config.AppSettings.Settings["BackgroundColor"].Value))

							if backgroundBrush.CanFreeze:
								backgroundBrush.Freeze()

					if config.AppSettings.Settings["TextColor"] is not None:
						if config.AppSettings.Settings["TextColor"].Value.Length > 0:
							textColor = ColorConverter.ConvertFromString(config.AppSettings.Settings["TextColor"].Value)

				textBrush = SolidColorBrush(textColor)

				if textBrush.CanFreeze:
					textBrush.Freeze()

				window = Window()

				stackPanel1 = StackPanel()
				stackPanel1.UseLayoutRounding = True
				stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel1.Orientation = Orientation.Vertical

				stackPanel2 = StackPanel()
				stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel2.Orientation = Orientation.Vertical
				stackPanel2.Background = SystemColors.ControlBrush if backgroundBrush is None else backgroundBrush

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

				solidColorBrush1 = SolidColorBrush(Colors.Black)
				solidColorBrush1.Opacity = 0.25

				if solidColorBrush1.CanFreeze:
					solidColorBrush1.Freeze()

				border1 = Border()
				border1.HorizontalAlignment = HorizontalAlignment.Stretch
				border1.VerticalAlignment = VerticalAlignment.Stretch
				border1.BorderThickness = Thickness(0, 0, 0, 1)
				border1.BorderBrush = solidColorBrush1

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
				label.Foreground = textBrush

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					label.Content = "話しかける一言"
				else:
					label.Content = "Message"

				RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
			
				textColor = SystemColors.ControlText if textBrush is None else textBrush.Color

				dropShadowEffect = DropShadowEffect()
				dropShadowEffect.BlurRadius = 1
				dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(textColor.R, textColor.G), textColor.B) > Byte.MaxValue / 2 else Colors.White
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
			
				border1.Child = stackPanel4

				stackPanel3.Children.Add(border1)
				stackPanel2.Children.Add(stackPanel3)
				stackPanel1.Children.Add(stackPanel2)

				def onAskClick(source, rea):
					if not String.IsNullOrEmpty(textBox.Text):
						ask(textBox.Text)

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

				border2.Child = askButton
				stackPanel1.Children.Add(border2)
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
	
	pingMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		pingMenuItem.Header = "クラウドのお気に入りへシェア"
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
	
	maxLength = 20
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
						if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title) and not pingEntryList.Exists(lambda x: x.Resource.Equals(entry.Resource)):
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

def createStackPanel(color, text1, text2):
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

	stackPanel = StackPanel()
	stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel.Orientation = Orientation.Vertical
	stackPanel.Background = linearGradientBrush
	
	solidColorBrush1 = SolidColorBrush(Colors.Black)
	solidColorBrush1.Opacity = 0.25

	if solidColorBrush1.CanFreeze:
		solidColorBrush1.Freeze()

	solidColorBrush2 = SolidColorBrush(Colors.White)
	solidColorBrush2.Opacity = 0.5

	if solidColorBrush2.CanFreeze:
		solidColorBrush2.Freeze()

	border1 = Border()
	border1.HorizontalAlignment = HorizontalAlignment.Stretch
	border1.VerticalAlignment = VerticalAlignment.Stretch
	border1.Padding = Thickness(0)
	border1.BorderThickness = Thickness(1, 0, 1, 1)
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	dockPanel.Effect = dropShadowEffect

	solidColorBrush3 = SolidColorBrush(color)

	if solidColorBrush3.CanFreeze:
		solidColorBrush3.Freeze()

	label1 = Label()
	label1.Margin = Thickness(0, 0, 50, 0)
	label1.HorizontalAlignment = HorizontalAlignment.Left
	label1.VerticalAlignment = VerticalAlignment.Stretch
	label1.Foreground = solidColorBrush3
	label1.Content = text1

	RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label1)

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Right
	label2.VerticalAlignment = VerticalAlignment.Stretch
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label2)

	border2.Child = dockPanel
	border1.Child = border2

	stackPanel.Children.Add(border1)

	return stackPanel

def createSectionStackPanel(color, text):
	stackPanel = StackPanel()
	stackPanel.HorizontalAlignment = HorizontalAlignment.Left
	stackPanel.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel.Margin = Thickness(10, 0, 10, 5)
	stackPanel.Orientation = Orientation.Vertical
	
	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	dockPanel.Effect = dropShadowEffect

	solidColorBrush = SolidColorBrush(color)

	if solidColorBrush.CanFreeze:
		solidColorBrush.Freeze()

	label = Label()
	label.Margin = Thickness(0)
	label.HorizontalAlignment = HorizontalAlignment.Left
	label.VerticalAlignment = VerticalAlignment.Stretch
	label.Foreground = solidColorBrush
	label.FontWeight = FontWeights.Bold
	label.Content = text

	RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label)
	stackPanel.Children.Add(dockPanel)

	return stackPanel

def createAnnotationStackPanel(color, text):
	stackPanel = StackPanel()
	stackPanel.HorizontalAlignment = HorizontalAlignment.Right
	stackPanel.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel.Margin = Thickness(10, 5, 10, 0)
	stackPanel.Orientation = Orientation.Vertical
	
	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	dockPanel.Effect = dropShadowEffect

	solidColorBrush = SolidColorBrush(color)

	if solidColorBrush.CanFreeze:
		solidColorBrush.Freeze()

	label = Label()
	label.Margin = Thickness(0)
	label.HorizontalAlignment = HorizontalAlignment.Left
	label.VerticalAlignment = VerticalAlignment.Stretch
	label.Foreground = solidColorBrush
	label.Content = text

	RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label)
	stackPanel.Children.Add(dockPanel)

	return stackPanel

def createStackPanelWithProgressBar(color, text1, text2, brush, progressPercentage):
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

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush
	
	solidColorBrush1 = SolidColorBrush(Colors.Black)
	solidColorBrush1.Opacity = 0.25

	if solidColorBrush1.CanFreeze:
		solidColorBrush1.Freeze()

	solidColorBrush2 = SolidColorBrush(Colors.White)
	solidColorBrush2.Opacity = 0.5

	if solidColorBrush2.CanFreeze:
		solidColorBrush2.Freeze()

	border1 = Border()
	border1.HorizontalAlignment = HorizontalAlignment.Stretch
	border1.VerticalAlignment = VerticalAlignment.Stretch
	border1.Padding = Thickness(0)
	border1.BorderThickness = Thickness(1, 0, 1, 1)
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Vertical

	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	dockPanel.Effect = dropShadowEffect

	solidColorBrush3 = SolidColorBrush(color)

	if solidColorBrush3.CanFreeze:
		solidColorBrush3.Freeze()

	label1 = Label()
	label1.Margin = Thickness(0, 0, 50, 0)
	label1.HorizontalAlignment = HorizontalAlignment.Left
	label1.VerticalAlignment = VerticalAlignment.Stretch
	label1.Foreground = solidColorBrush3
	label1.Content = text1

	RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label1)

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Right
	label2.VerticalAlignment = VerticalAlignment.Stretch
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label2)
	stackPanel2.Children.Add(dockPanel)

	solidColorBrush4 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 102, 102, 102))

	if solidColorBrush4.CanFreeze:
		solidColorBrush4.Freeze()

	canvas = Canvas()
	canvas.HorizontalAlignment = HorizontalAlignment.Left
	canvas.VerticalAlignment = VerticalAlignment.Top
	canvas.Margin = Thickness(5, 10, 5, 10)
	canvas.Background = solidColorBrush4
	canvas.Height = 2

	rectangle = Rectangle()
	rectangle.HorizontalAlignment = HorizontalAlignment.Left
	rectangle.VerticalAlignment = VerticalAlignment.Top
	rectangle.Height = 2
	rectangle.Fill = brush

	def onLoaded(sender, args):
		width = dockPanel.ActualWidth - canvas.Margin.Left - canvas.Margin.Right
		canvas.Width = width
		rectangle.Width = width * progressPercentage / 100
				
	dockPanel.Loaded += onLoaded

	canvas.Children.Add(rectangle)
	stackPanel2.Children.Add(canvas)

	border2.Child = stackPanel2
	border1.Child = border2

	stackPanel1.Children.Add(border1)

	return stackPanel1

def createStackPanelWithHint(color, text1, text2, text3):
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

	stackPanel = StackPanel()
	stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel.Orientation = Orientation.Vertical
	stackPanel.Background = linearGradientBrush
	
	solidColorBrush1 = SolidColorBrush(Colors.Black)
	solidColorBrush1.Opacity = 0.25

	if solidColorBrush1.CanFreeze:
		solidColorBrush1.Freeze()

	solidColorBrush2 = SolidColorBrush(Colors.White)
	solidColorBrush2.Opacity = 0.5

	if solidColorBrush2.CanFreeze:
		solidColorBrush2.Freeze()

	border1 = Border()
	border1.HorizontalAlignment = HorizontalAlignment.Stretch
	border1.VerticalAlignment = VerticalAlignment.Stretch
	border1.Padding = Thickness(0)
	border1.BorderThickness = Thickness(1, 0, 1, 1)
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	dockPanel.Effect = dropShadowEffect

	solidColorBrush3 = SolidColorBrush(color)

	if solidColorBrush3.CanFreeze:
		solidColorBrush3.Freeze()

	label1 = Label()
	label1.Margin = Thickness(0, 0, 50, 0)
	label1.HorizontalAlignment = HorizontalAlignment.Left
	label1.VerticalAlignment = VerticalAlignment.Stretch
	label1.Foreground = solidColorBrush3
	label1.Content = text1

	RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label1)

	sp = StackPanel()
	sp.HorizontalAlignment = HorizontalAlignment.Right
	sp.VerticalAlignment = VerticalAlignment.Stretch
	sp.Orientation = Orientation.Vertical

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Right
	label2.VerticalAlignment = VerticalAlignment.Stretch
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)

	label3 = Label()
	label3.HorizontalAlignment = HorizontalAlignment.Right
	label3.VerticalAlignment = VerticalAlignment.Stretch
	label3.Foreground = solidColorBrush3
	label3.Content = text3
	
	RenderOptions.SetClearTypeHint(label3, ClearTypeHint.Enabled)

	sp.Children.Add(label2)
	sp.Children.Add(label3)
	dockPanel.Children.Add(sp)

	border2.Child = dockPanel
	border1.Child = border2

	stackPanel.Children.Add(border1)

	return stackPanel

def createStackPanelWithLike(color, text1, text2, brush):
	from System.Windows.Shapes import Path

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

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush
	
	solidColorBrush1 = SolidColorBrush(Colors.Black)
	solidColorBrush1.Opacity = 0.25

	if solidColorBrush1.CanFreeze:
		solidColorBrush1.Freeze()

	solidColorBrush2 = SolidColorBrush(Colors.White)
	solidColorBrush2.Opacity = 0.5

	if solidColorBrush2.CanFreeze:
		solidColorBrush2.Freeze()

	border1 = Border()
	border1.HorizontalAlignment = HorizontalAlignment.Stretch
	border1.VerticalAlignment = VerticalAlignment.Stretch
	border1.Padding = Thickness(0)
	border1.BorderThickness = Thickness(1, 0, 1, 1)
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Colors.White
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	border3 = Border()
	border3.HorizontalAlignment = HorizontalAlignment.Stretch
	border3.VerticalAlignment = VerticalAlignment.Stretch
	border3.Margin = Thickness(0)
	border3.Padding = Thickness(0)
	border3.CornerRadius = CornerRadius(0)
	border3.Background = Brushes.Transparent
	border3.Effect = dropShadowEffect
	
	solidColorBrush3 = SolidColorBrush(color)

	if solidColorBrush3.CanFreeze:
		solidColorBrush3.Freeze()

	label1 = Label()
	label1.Margin = Thickness(0, 0, 50, 0)
	label1.HorizontalAlignment = HorizontalAlignment.Left
	label1.VerticalAlignment = VerticalAlignment.Stretch
	label1.Foreground = solidColorBrush3
	label1.Content = text1

	RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)
	
	border3.Child = label1
	dockPanel.Children.Add(border3)

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Horizontal
	stackPanel2.Background = Brushes.Transparent

	likeGeometry = createLikeGeometry(Rect(0, 0, 8, 6))

	if likeGeometry.CanFreeze:
		likeGeometry.Freeze()

	path = Path()
	path.HorizontalAlignment = HorizontalAlignment.Stretch
	path.VerticalAlignment = VerticalAlignment.Center
	path.Fill = brush
	path.Data = likeGeometry

	stackPanel2.Children.Add(path)

	border4 = Border()
	border4.HorizontalAlignment = HorizontalAlignment.Stretch
	border4.VerticalAlignment = VerticalAlignment.Stretch
	border4.Margin = Thickness(0)
	border4.Padding = Thickness(0)
	border4.CornerRadius = CornerRadius(0)
	border4.Background = Brushes.Transparent
	border4.Effect = dropShadowEffect

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Stretch
	label2.VerticalAlignment = VerticalAlignment.Center
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	border4.Child = label2
	stackPanel2.Children.Add(border4)

	dockPanel.Children.Add(stackPanel2)
	border2.Child = dockPanel
	border1.Child = border2

	stackPanel1.Children.Add(border1)

	return stackPanel1

def createLikeGeometry(rect):
	streamGeometry = StreamGeometry()
	streamGeometry.FillRule = FillRule.Nonzero

	streamGeometryContext = None

	try:
		streamGeometryContext = streamGeometry.Open()
		streamGeometryContext.BeginFigure(Point(rect.X + rect.Width / 2, rect.Y + rect.Height), True, True)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X, rect.Y + rect.Height * 2 / 3), Point(rect.X, rect.Y + rect.Height / 3), True, False)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X, rect.Y + rect.Height / 24), Point(rect.X + rect.Width / 4, rect.Y), True, False)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X + rect.Width * 5 / 12, rect.Y), Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 8), True, False)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X + rect.Width * 7 / 12, rect.Y), Point(rect.X + rect.Width * 3 / 4, rect.Y), True, False)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X + rect.Width, rect.Y + rect.Height / 24), Point(rect.X + rect.Width, rect.Y + rect.Height / 3), True, False)
		streamGeometryContext.QuadraticBezierTo(Point(rect.X + rect.Width, rect.Y + rect.Height * 2 / 3), Point(rect.X + rect.Width / 2, rect.Y + rect.Height), True, False)
	
	finally:
		if streamGeometryContext is not None:
			streamGeometryContext.Dispose()

	return streamGeometry

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
	sb = StringBuilder(32)

	for i in range(0, 32):
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
	
def onStart(s, e):
	global username, password, oauthToken, oauthTokenSecret, menuItem, separator, timer

	username = None
	password = None

	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)
	
	if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("oauth_token", oauthToken)

		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

		sortedDictionary.Add("realm", "http://api.twitter.com/")
		sortedDictionary.Add("oauth_signature", signature)

		verifyRequest = WebRequest.Create("http://social.apricotan.net/verify")
		verifyRequest.PreAuthenticate = True
		verifyRequest.Method = WebRequestMethods.Http.Get
		verifyRequest.Headers.Add("X-Verify-Credentials-Authorization", createHttpAuthorizationHeader(sortedDictionary))
		verifyRequest.Headers.Add("X-Auth-Service-Provider", "https://api.twitter.com/1.1/account/verify_credentials.json")
		likesRequestList = List[WebRequest]()
		verifyDictionary = Dictionary[String, String]()
		userDictionary = Dictionary[String, Double]()
		currentLikesDictionary = Dictionary[String, Int32]()
		context = TaskScheduler.FromCurrentSynchronizationContext()

		def onVerify():
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					response = None
					stream = None
					streamReader = None

					try:
						response = verifyRequest.GetResponse()
						
						if response.StatusCode == HttpStatusCode.OK:
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("user"):
										user = json["user"]
										
										if user is not None:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
												if user.ContainsKey("name"):
													if clr.GetClrType(String).IsInstanceOfType(user["name"]):
														verifyDictionary.Add("name", user["name"])

												if user.ContainsKey("password"):
													if clr.GetClrType(String).IsInstanceOfType(user["password"]):
														verifyDictionary.Add("password", user["password"])

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

		def onReady(task):
			global username, password

			for kvp in verifyDictionary:
				if kvp.Key.Equals("name"):
					username = kvp.Value
				elif kvp.Key.Equals("password"):
					password = kvp.Value

			if username is not None:
				for character in Script.Instance.Characters:
					likesRequest = WebRequest.Create(String.Format("http://social.apricotan.net/likes?character_name={0}&user_name={1}&limit=50", urlEncode(character.Name), urlEncode(username)))
					likesRequest.Method = WebRequestMethods.Http.Get

					likesRequestList.Add(likesRequest)

		def onUpdate(task):
			if NetworkInterface.GetIsNetworkAvailable():
				try:
					if verifyDictionary.ContainsKey("name"):
						request = WebRequest.Create(String.Concat("http://social.apricotan.net/user?name=", verifyDictionary["name"]))
						request.Method = WebRequestMethods.Http.Get
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(json):
									if json.ContainsKey("user"):
										user = json["user"]
										
										if user is not None:
											if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(user):
												for kvp in user:
													if clr.GetClrType(Double).IsInstanceOfType(kvp.Value):
														userDictionary.Add(kvp.Key, kvp.Value)

						finally:
							if streamReader is not None:
								streamReader.Close()

							if stream is not None:
								stream.Close()
								
							if response is not None:
								response.Close()

					dt = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)

					for likesRequest in likesRequestList:
						response = None
						stream = None
						streamReader = None

						try:
							response = likesRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							json = Json.decode(streamReader.ReadToEnd())

							if json is not None:
								if clr.GetClrType(Array).IsInstanceOfType(json):
									for obj in json:
										if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
											if obj.ContainsKey("like"):
												like = obj["like"]
													
												if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(like):
													if like.ContainsKey("character") and like.ContainsKey("created"):
														character = like["character"]

														if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(character) and clr.GetClrType(String).IsInstanceOfType(like["created"]):
															created = DateTime.Parse(like["created"])
																
															if character.ContainsKey("name") and created > dt:
																if clr.GetClrType(String).IsInstanceOfType(character["name"]):
																	if currentLikesDictionary.ContainsKey(character["name"]):
																		currentLikesDictionary[character["name"]] += 1
																	else:
																		currentLikesDictionary.Add(character["name"], 1)

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
			global remainingCount, likesDictionary

			if userDictionary.ContainsKey("points"):
				remainingCount = Nullable[Int32](Convert.ToInt32(userDictionary["points"]))

			if currentLikesDictionary.Count > 0:
				likesDictionary.Clear()

				for kvp in currentLikesDictionary:
					likesDictionary.Add(kvp.Key, kvp.Value)

				for kvp in likesDictionary:
					sequenceList = List[Sequence]()
								
					for sequence in Script.Instance.Sequences:
						if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
							sequenceList.Add(sequence)

					Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.ToString(CultureInfo.InvariantCulture)))
				
		Task.Factory.StartNew(onVerify, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith(onUpdate, TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

	timer.Start()

def onStop(s, e):
	global timer

	timer.Stop()

username = None
password = None
likesDictionary = Dictionary[String, Int32]()
remainingCount = None
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