# -*- coding: utf-8 -*-
# Play.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("System.IO.Compression")
clr.AddReferenceByPartialName("System.Xml.Linq")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, ValueType, Nullable, Boolean, Byte, Char, UInt32, Int32, Int64, Double, String, StringComparison, Uri, DateTime, TimeSpan, Array, Convert, BitConverter, Type, Environment, Math, Random, Action, Func
from System.IO import Stream, FileStream, BufferedStream, MemoryStream, SeekOrigin, StreamReader, StreamWriter, Directory, File, FileMode, FileAccess, FileShare, SearchOption
from System.IO.Compression import ZipArchive
from System.Collections.Generic import List, LinkedList, Dictionary, SortedDictionary, KeyValuePair, Queue, HashSet
from System.Collections.ObjectModel import Collection
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Globalization import CultureInfo, NumberStyles, DateTimeStyles
from System.Linq import Enumerable
from System.Media import SoundPlayer
from System.Diagnostics import Process, Trace
from System.Reflection import Assembly
from System.Security.Cryptography import HMACSHA1, SHA1CryptoServiceProvider
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, Match, MatchEvaluator, RegexOptions, Capture
from System.Threading import CountdownEvent
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Net import WebRequest, WebResponse, HttpWebRequest, HttpWebResponse, WebClient, HttpRequestHeader, WebRequestMethods, HttpStatusCode
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, WindowStyle, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Point, Size, Rect, Thickness, SystemColors, SystemParameters, PropertyPath, CornerRadius, FontSizeConverter, FontWeights, DependencyPropertyChangedEventArgs, TextAlignment, TextWrapping
from System.Windows.Controls import ContentControl, MenuItem, Separator, Border, Label, Button, StackPanel, Orientation, Grid, DockPanel, Dock, Canvas, Image, TextBlock, TextBox, WebBrowser
from System.Windows.Input import Keyboard, ModifierKeys, MouseButtonEventArgs, MouseButton
from System.Windows.Media import Color, Colors, ColorConverter, Brushes, SolidColorBrush, LinearGradientBrush, GradientStopCollection, GradientStop, RenderOptions, ClearTypeHint, ScaleTransform, RectangleGeometry, EllipseGeometry, StreamGeometry, FillRule, StreamGeometryContext, BitmapCache, ImageBrush, TileMode, BrushMappingMode, Stretch, AlignmentX, AlignmentY, DrawingGroup, DrawingContext, DrawingImage
from System.Windows.Media.Animation import Storyboard, HandoffBehavior, Clock, ClockState, DoubleAnimation, RectAnimation, ColorAnimation, SineEase, EasingMode
from System.Windows.Media.Effects import DropShadowEffect
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
from System.Windows.Shapes import Rectangle, Ellipse
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System.Xml.Linq import XDocument, XElement, XAttribute
from Apricot import Agent, Balloon, Script, Character, Entry, Message, Motion, Word, Sequence

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

				if codepoint >= 32 and codepoint <= 126:
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
			s1 = stringBuilder.ToString()
			selectedWord1 = None

			if dictionary.ContainsKey(s1[0]):
				for word in dictionary[s1[0]]:
					if s1.StartsWith(word, StringComparison.Ordinal) and word.Length > (0 if selectedWord1 is None else selectedWord1.Length):
						selectedWord1 = word

			if String.IsNullOrEmpty(selectedWord1):
				stringBuilder.Remove(0, 1)
			else:
				sb = StringBuilder(stringBuilder.ToString(1, stringBuilder.Length - 1))
				selectedWord2 = None
				i = 0
				max = 0

				while sb.Length > 0 and i < selectedWord1.Length:
					s2 = sb.ToString()

					if dictionary.ContainsKey(s2[0]):
						for word in dictionary[s2[0]]:
							if s2.StartsWith(word, StringComparison.Ordinal) and word.Length > (0 if selectedWord2 is None else selectedWord2.Length):
								selectedWord2 = word
								max = i + selectedWord2.Length

					sb.Remove(0, 1)
					i += 1

				if not String.IsNullOrEmpty(selectedWord2) and selectedWord1.Length < selectedWord2.Length:
					selectedWordList.Add(selectedWord2)
					stringBuilder.Remove(0, max)

				else:
					selectedWordList.Add(selectedWord1)
					stringBuilder.Remove(0, selectedWord1.Length)

		return selectedWordList

def ask(text):
	global consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	wordList = List[String]()
	documentDictionary = Dictionary[String, List[String]]()
	wordDictionary = Dictionary[String, List[String]]()
	attributeHashSet = HashSet[String]()
	stringBuilder = StringBuilder()
	updateWebClient = WebClient()
	context = TaskScheduler.FromCurrentSynchronizationContext()
	
	def onLoad():
		fileStream1 = None
		streamReader1 = None
		fileStream2 = None
		streamReader2 = None

		try:
			fileStream1 = FileStream("Words.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			streamReader1 = StreamReader(fileStream1, UTF8Encoding(False), True)
			jsonArray = Json.decode(streamReader1.ReadToEnd())
			
			if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
				for obj in jsonArray:
					if obj is not None and clr.GetClrType(String).IsInstanceOfType(obj):
						wordList.Add(obj)

			fileStream2 = FileStream("Training.json", FileMode.Open, FileAccess.Read, FileShare.Read)
			streamReader2 = StreamReader(fileStream2, UTF8Encoding(False), True)
			jsonDictionary = Json.decode(streamReader2.ReadToEnd())
			
			if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
				for kvp in jsonDictionary:
					if kvp.Value is not None and clr.GetClrType(Array).IsInstanceOfType(kvp.Value):
						list = List[String]()

						for s in kvp.Value:
							if clr.GetClrType(String).IsInstanceOfType(s):
								list.Add(s)
						
						documentDictionary.Add(kvp.Key, list)

		except Exception, e:
			Trace.WriteLine(e.clsException.Message)
			Trace.WriteLine(e.clsException.StackTrace)

		finally:
			if streamReader2 is not None:
				streamReader2.Close()

			if fileStream2 is not None:
				fileStream2.Close()

			if streamReader1 is not None:
				streamReader1.Close()

			if fileStream1 is not None:
				fileStream1.Close()
	
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
				list[i] = Regex.Replace(list[i], "(?<1>(?<Open>\\{{2})*)\\{\\*}(?<2>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", MatchEvaluator(lambda x: String.Concat(x.Groups[1].Value.Substring(x.Groups[1].Length / 2), "{", String.Join("|", attributeHashSet), "}", x.Groups[2].Value.Substring(x.Groups[2].Length / 2)) if x.Groups[1].Success and x.Groups[2].Success else String.Concat(x.Groups[1].Value.Substring(x.Groups[1].Length / 2), "{", String.Join("|", attributeHashSet), "}") if x.Groups[1].Success else String.Concat("{", String.Join("|", attributeHashSet), "}", x.Groups[2].Value.Substring(x.Groups[2].Length / 2)) if x.Groups[2].Success else String.Concat("{", String.Join("|", attributeHashSet), "}")), RegexOptions.CultureInvariant)

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

		for value in documentDictionary.Values:
			for i in range(value.Count):
				for match in Regex.Matches(value[i], "(?<Open>\\{{2})*\\{(?<1>(?:[^{}]|(?<2>(?:(?:\\{|}){2})+))+)}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
					if match.Groups[2].Success:
						j = match.Groups[1].Index
						sb = StringBuilder()

						for capture in match.Groups[2].Captures:
							if capture.Index > j:
								sb.Append(value[i].Substring(j, capture.Index - j))

							sb.Append(capture.Value.Substring(capture.Length / 2))
							j = capture.Index + capture.Length

						if match.Groups[1].Index + match.Groups[1].Length > j:
							sb.Append(value[i].Substring(j, match.Groups[1].Index + match.Groups[1].Length - j))

						pattern = sb.ToString()

					else:
						pattern = match.Groups[1].Value

					for attribute in attributeHashSet:
						if Regex.IsMatch(attribute, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline):
							if not usageDictionary.ContainsKey(attribute):
								usageDictionary.Add(attribute, List[String]())

							usageDictionary[attribute].Add(match.Groups[1].Value)

		for value in documentDictionary.Values:
			for i in range(value.Count):
				index = 0
				sb = StringBuilder()
				
				for match in Regex.Matches(value[i], "(?<1>(?<Open>\\{{2})*)\\{(?<2>(?:[^{}]|(?:(?:\\{|}){2})+)+)}(?<3>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
					if match.Index - index > 0:
						sb.Append(Regex.Replace(value[i].Substring(index, match.Index - index), "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant))

					if cacheDictionary.ContainsKey(match.Value):
						if match.Groups[1].Success:
							sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

						sb.Append(cacheDictionary[match.Value])

						if match.Groups[3].Success:
							sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))

					else:
						max1 = 0
						word1 = None
						max2 = 0
						word2 = None

						for word in wordList:
							if wordDictionary.ContainsKey(word):
								for attribute in wordDictionary[word]:
									if usageDictionary.ContainsKey(attribute):
										if usageDictionary[attribute].Contains(match.Groups[2].Value):
											if termHashSet.Contains(word):
												if usageDictionary[attribute].Count > max1:
													max1 = usageDictionary[attribute].Count
													word1 = word

											elif usageDictionary[attribute].Count > max2:
												max2 = usageDictionary[attribute].Count
												word2 = word

						if word1 is None:
							if word2 is None:
								sb.Append(match.Value)

							else:
								if match.Groups[1].Success:
									sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

								sb.Append(word2)

								if match.Groups[3].Success:
									sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))

								cacheDictionary.Add(match.Value, word2)

						else:
							if match.Groups[1].Success:
								sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

							sb.Append(word1)

							if match.Groups[3].Success:
								sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))

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
					if not Regex.IsMatch(s, "(?<Open>\\{{2})*\\{([^{}]|((\\{|}){2})+)+}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
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

				if not String.IsNullOrEmpty(oauthToken) and not String.IsNullOrEmpty(oauthTokenSecret):
					sortedDictionary = createCommonParameters(consumerKey)
					sortedDictionary.Add("oauth_token", oauthToken)
					sortedDictionary.Add("status", urlEncode(stringBuilder.ToString()))
					sortedDictionary.Add("oauth_signature", Convert.ToBase64String(HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret))).ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Post, Uri(String.Concat("https://api.twitter.com/1.1/statuses/update.json?status=", urlEncode(stringBuilder.ToString()))), sortedDictionary)))))
					sortedDictionary.Remove("status")

					updateWebClient.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
					updateWebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded")

					return True

		return False

	def onUpdate(task):
		if NetworkInterface.GetIsNetworkAvailable() and task.Result:
			try:
				updateWebClient.UploadData(Uri("https://api.twitter.com/1.1/statuses/update.json"), WebRequestMethods.Http.Post, Encoding.ASCII.GetBytes(String.Concat("status=", urlEncode(stringBuilder.ToString()))))

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)
			
	def onCompleted(task):
		global remainingCount

		if remainingCount < 5:
			remainingCount += 1

	Task.Factory.StartNew(onLoad, TaskCreationOptions.LongRunning).ContinueWith(onLoaded, context).ContinueWith[NaiveBayes](onTrain, TaskContinuationOptions.LongRunning).ContinueWith[Boolean](Func[Task[NaiveBayes], Boolean](onTrained), context).ContinueWith(Action[Task[Boolean]](onUpdate), TaskContinuationOptions.LongRunning).ContinueWith(onCompleted, context)

def onTick(timer, args):
	global consumerKey, consumerSecret, oauthToken, oauthTokenSecret

	entryList = List[Entry]()
	wordList = List[Word]()

	if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
		if File.Exists("Likes.json"):
			nameList = List[String]()
			currentLikesDictionary = Dictionary[String, List[DateTime]]()

			for character in Script.Instance.Characters:
				nameList.Add(character.Name)

			def onUpdate():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						request = WebRequest.Create("http://api.apricotan.net/entries?format=json&limit=25")
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("entry") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
										entry = Entry()
										
										if obj["entry"].ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
											entry.Resource = Uri(obj["entry"]["resource"])

										if obj["entry"].ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
											entry.Title = obj["entry"]["title"]

										if obj["entry"].ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
											entry.Created = DateTime.Parse(obj["entry"]["created"])

										if obj["entry"].ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
											entry.Modified = DateTime.Parse(obj["entry"]["modified"])

										if obj["entry"].ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
											entry.Image = Uri(obj["entry"]["image"])

										if obj["entry"].ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
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

						request = WebRequest.Create("http://api.apricotan.net/words?format=json&limit=50")
						response = None
						stream = None
						streamReader = None
				
						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("word") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
										word = Word()
											
										if obj["word"].ContainsKey("name") and clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
											word.Name = obj["word"]["name"]

										if obj["word"].ContainsKey("attributes") and clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
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

				try:
					fileStream = None
					streamReader = None
				
					try:
						dt1 = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)
						fileStream = FileStream("Likes.json", FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
						encoding = UTF8Encoding(False)
						streamReader = StreamReader(fileStream, encoding, True)
						jsonDictionary = Json.decode(streamReader.ReadToEnd())

						if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
							keyHashSet = HashSet[String](jsonDictionary.Keys)

							for key in nameList.ConvertAll[String](lambda x: Convert.ToBase64String(Encoding.UTF8.GetBytes(x))):
								if not keyHashSet.Contains(key):
									keyHashSet.Add(key)
						
							for key in keyHashSet:
								name = Encoding.UTF8.GetString(Convert.FromBase64String(key))
							
								currentLikesDictionary.Add(name, List[DateTime]())

								if jsonDictionary.ContainsKey(key):
									list = List[String]()

									if jsonDictionary[key] is not None:
										if clr.GetClrType(Array).IsInstanceOfType(jsonDictionary[key]):
											for value in jsonDictionary[key]:
												if clr.GetClrType(String).IsInstanceOfType(value):
													dt2 = DateTime(Int64.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value))))

													if dt2 > dt1:
														currentLikesDictionary[name].Add(dt2)
														list.Add(value)

										else:
											currentLikesDictionary.Clear()

											return

									if list.Count > 0 or nameList.Contains(name):
										jsonDictionary[key] = list.ToArray()
									else:
										jsonDictionary.Remove(key)

							if jsonDictionary.Count > 0:
								json = Json.encode(jsonDictionary)

								if json is not None:
									fileStream.SetLength(0)
									streamWriter = None
						
									try:
										streamWriter = StreamWriter(fileStream, encoding)
										streamWriter.Write(json)

									finally:
										if streamWriter is not None:
											streamWriter.Close()

							else:
								streamReader.Close()
								streamReader = None
								fileStream.Close()
								fileStream = None
								File.Delete("Likes.json")
						
					finally:
						if streamReader is not None:
							streamReader.Close()
							
						if fileStream is not None:
							fileStream.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			def onCompleted(task):
				global likesDictionary, autoUpdate, dateTime, recentEntryList, recentWordList
		
				if currentLikesDictionary.Count > 0:
					likesDictionary.Clear()

					for kvp in currentLikesDictionary:
						likesDictionary.Add(kvp.Key, kvp.Value)

						for name in nameList:
							if name.Equals(kvp.Key):
								sequenceList = List[Sequence]()
				
								for sequence in Script.Instance.Sequences:
									if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
										sequenceList.Add(sequence)

								Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.Count.ToString(CultureInfo.InvariantCulture)))

								break
					
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
							Script.Instance.Suggest("クラウド", newEntryList)
						else:
							Script.Instance.Suggest("Clouds", newEntryList)
				
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
			def onUpdate():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						request = WebRequest.Create("http://api.apricotan.net/entries?format=json&limit=25")
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("entry") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
										entry = Entry()

										if obj["entry"].ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
											entry.Resource = Uri(obj["entry"]["resource"])

										if obj["entry"].ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
											entry.Title = obj["entry"]["title"]

										if obj["entry"].ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
											entry.Created = DateTime.Parse(obj["entry"]["created"])

										if obj["entry"].ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
											entry.Modified = DateTime.Parse(obj["entry"]["modified"])

										if obj["entry"].ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
											entry.Image = Uri(obj["entry"]["image"])

										if obj["entry"].ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
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

						request = WebRequest.Create("http://api.apricotan.net/words?format=json&limit=50")
						response = None
						stream = None
						streamReader = None
				
						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("word") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
										word = Word()
											
										if obj["word"].ContainsKey("name") and clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
											word.Name = obj["word"]["name"]

										if obj["word"].ContainsKey("attributes") and clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
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
							Script.Instance.Suggest("クラウド", newEntryList)
						else:
							Script.Instance.Suggest("Clouds", newEntryList)
				
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
		hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			
		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("oauth_token", oauthToken)

		signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

		sortedDictionary.Add("realm", "http://api.twitter.com/")
		sortedDictionary.Add("oauth_signature", signature)

		verifyRequest = WebRequest.Create("https://api.twitter.com/1.1/account/verify_credentials.json")
		verifyRequest.PreAuthenticate = True
		verifyRequest.Method = WebRequestMethods.Http.Get
		verifyRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))

		sortedDictionary = createCommonParameters(consumerKey)
		sortedDictionary.Add("oauth_token", oauthToken)
		sortedDictionary.Add("q", urlEncode("#apricotan"))
		sortedDictionary.Add("result_type", "mixed")
		sortedDictionary.Add("oauth_signature", Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode("#apricotan"), "&result_type=mixed")), sortedDictionary)))))

		searchRequest = WebRequest.Create(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode("#apricotan"), "&result_type=mixed"))
		searchRequest.Method = WebRequestMethods.Http.Get
		searchRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
		searchRequest.ContentType = "application/json"

		recentLikesDictionary = Dictionary[String, Int32]()

		if File.Exists("Likes.json"):
			nameList = List[String]()
			currentLikesDictionary = Dictionary[String, List[DateTime]]()

			for character in Script.Instance.Characters:
				nameList.Add(character.Name)

			def onUpdate():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						request = WebRequest.Create("http://api.apricotan.net/entries?format=json&limit=25")
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("entry") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
										entry = Entry()
										
										if obj["entry"].ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
											entry.Resource = Uri(obj["entry"]["resource"])

										if obj["entry"].ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
											entry.Title = obj["entry"]["title"]

										if obj["entry"].ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
											entry.Created = DateTime.Parse(obj["entry"]["created"])

										if obj["entry"].ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
											entry.Modified = DateTime.Parse(obj["entry"]["modified"])

										if obj["entry"].ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
											entry.Image = Uri(obj["entry"]["image"])

										if obj["entry"].ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
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

						request = WebRequest.Create("http://api.apricotan.net/words?format=json&limit=50")
						response = None
						stream = None
						streamReader = None
				
						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("word") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
										word = Word()
											
										if obj["word"].ContainsKey("name") and clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
											word.Name = obj["word"]["name"]

										if obj["word"].ContainsKey("attributes") and clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
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

						response = None
						stream = None
						streamReader = None

						try:
							response = verifyRequest.GetResponse()
                                                
							if response.StatusCode == HttpStatusCode.OK:
								stream = response.GetResponseStream()
								streamReader = StreamReader(stream)
								jsonDictionary = Json.decode(streamReader.ReadToEnd())

								if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("screen_name") and jsonDictionary["screen_name"] is not None and clr.GetClrType(String).IsInstanceOfType(jsonDictionary["screen_name"]):
									screenName = jsonDictionary["screen_name"]

						finally:
							if streamReader is not None:
								streamReader.Close()
						
							if stream is not None:
								stream.Close()
						
							if response is not None:
								response.Close()

						response = None
						stream = None
						streamReader = None

						try:
							response = searchRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonDictionary = Json.decode(streamReader.ReadToEnd())

							if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("statuses") and clr.GetClrType(Array).IsInstanceOfType(jsonDictionary["statuses"]):
								for status in jsonDictionary["statuses"]:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(status) and status.ContainsKey("text") and status["text"] is not None:
										hs = HashSet[String]()
										match = Regex.Match(status["text"], "\"(.+?):.+?\"", RegexOptions.CultureInvariant | RegexOptions.Singleline)

										while match.Success:
											if not hs.Contains(match.Groups[1].Value):
												hs.Add(match.Groups[1].Value)

											match = match.NextMatch()

										for name in hs:
											if recentLikesDictionary.ContainsKey(name):
												recentLikesDictionary[name] += 1
											else:
												recentLikesDictionary.Add(name, 1)

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

				try:
					fileStream = None
					streamReader = None
				
					try:
						dt1 = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)
						fileStream = FileStream("Likes.json", FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
						encoding = UTF8Encoding(False)
						streamReader = StreamReader(fileStream, encoding, True)
						jsonDictionary = Json.decode(streamReader.ReadToEnd())

						if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
							keyHashSet = HashSet[String](jsonDictionary.Keys)

							for key in nameList.ConvertAll[String](lambda x: Convert.ToBase64String(Encoding.UTF8.GetBytes(x))):
								if not keyHashSet.Contains(key):
									keyHashSet.Add(key)
						
							for key in keyHashSet:
								name = Encoding.UTF8.GetString(Convert.FromBase64String(key))
							
								currentLikesDictionary.Add(name, List[DateTime]())

								if jsonDictionary.ContainsKey(key):
									list = List[String]()

									if jsonDictionary[key] is not None:
										if clr.GetClrType(Array).IsInstanceOfType(jsonDictionary[key]):
											for value in jsonDictionary[key]:
												if clr.GetClrType(String).IsInstanceOfType(value):
													dt2 = DateTime(Int64.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value))))

													if dt2 > dt1:
														currentLikesDictionary[name].Add(dt2)
														list.Add(value)

										else:
											currentLikesDictionary.Clear()

											return

									if list.Count > 0 or nameList.Contains(name):
										jsonDictionary[key] = list.ToArray()
									else:
										jsonDictionary.Remove(key)

							if jsonDictionary.Count > 0:
								json = Json.encode(jsonDictionary)

								if json is not None:
									fileStream.SetLength(0)
									streamWriter = None
						
									try:
										streamWriter = StreamWriter(fileStream, encoding)
										streamWriter.Write(json)

									finally:
										if streamWriter is not None:
											streamWriter.Close()

							else:
								streamReader.Close()
								streamReader = None
								fileStream.Close()
								fileStream = None
								File.Delete("Likes.json")
						
					finally:
						if streamReader is not None:
							streamReader.Close()
							
						if fileStream is not None:
							fileStream.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			def onCompleted(task):
				global likesDictionary, backingDictionary, autoUpdate, dateTime, recentEntryList, recentWordList

				backingDictionary.Clear()

				for kvp in recentLikesDictionary:
					backingDictionary.Add(kvp.Key, kvp.Value)
		
				if currentLikesDictionary.Count > 0:
					likesDictionary.Clear()

					for kvp in currentLikesDictionary:
						likesDictionary.Add(kvp.Key, kvp.Value)

						for name in nameList:
							if name.Equals(kvp.Key):
								sequenceList = List[Sequence]()
				
								for sequence in Script.Instance.Sequences:
									if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
										sequenceList.Add(sequence)

								if backingDictionary.ContainsKey(name):
									Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, (backingDictionary[name] + kvp.Value.Count).ToString(CultureInfo.InvariantCulture)))
								else:
									Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.Count.ToString(CultureInfo.InvariantCulture)))

								break
					
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
							Script.Instance.Suggest("クラウド", newEntryList)
						else:
							Script.Instance.Suggest("Clouds", newEntryList)
				
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
			def onUpdate():
				if NetworkInterface.GetIsNetworkAvailable():
					try:
						request = WebRequest.Create("http://api.apricotan.net/entries?format=json&limit=25")
						response = None
						stream = None
						streamReader = None

						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("entry") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
										entry = Entry()

										if obj["entry"].ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
											entry.Resource = Uri(obj["entry"]["resource"])

										if obj["entry"].ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
											entry.Title = obj["entry"]["title"]

										if obj["entry"].ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
											entry.Created = DateTime.Parse(obj["entry"]["created"])

										if obj["entry"].ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
											entry.Modified = DateTime.Parse(obj["entry"]["modified"])

										if obj["entry"].ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
											entry.Image = Uri(obj["entry"]["image"])

										if obj["entry"].ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
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

						request = WebRequest.Create("http://api.apricotan.net/words?format=json&limit=50")
						response = None
						stream = None
						streamReader = None
				
						try:
							response = request.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonArray = Json.decode(streamReader.ReadToEnd())

							if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
								for obj in jsonArray:
									if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("word") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
										word = Word()
											
										if obj["word"].ContainsKey("name") and clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
											word.Name = obj["word"]["name"]

										if obj["word"].ContainsKey("attributes") and clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
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

						response = None
						stream = None
						streamReader = None

						try:
							response = searchRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonDictionary = Json.decode(streamReader.ReadToEnd())

							if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("statuses") and clr.GetClrType(Array).IsInstanceOfType(jsonDictionary["statuses"]):
								for status in jsonDictionary["statuses"]:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(status) and status.ContainsKey("text") and status["text"] is not None:
										hs = HashSet[String]()
										match = Regex.Match(status["text"], "\"(.+?):.+?\"", RegexOptions.CultureInvariant | RegexOptions.Singleline)

										while match.Success:
											if not hs.Contains(match.Groups[1].Value):
												hs.Add(match.Groups[1].Value)

											match = match.NextMatch()

										for name in hs:
											if recentLikesDictionary.ContainsKey(name):
												recentLikesDictionary[name] += 1
											else:
												recentLikesDictionary.Add(name, 1)

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
				global backingDictionary, autoUpdate, dateTime, recentEntryList, recentWordList

				backingDictionary.Clear()

				for kvp in recentLikesDictionary:
					backingDictionary.Add(kvp.Key, kvp.Value)

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
							Script.Instance.Suggest("クラウド", newEntryList)
						else:
							Script.Instance.Suggest("Clouds", newEntryList)
				
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

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(10)
	timer.Start()

def onPing(sender, args):
	entry = sender.Tag
		
	if entry.Resource is not None and not String.IsNullOrEmpty(entry.Title):
		stringBuilder = StringBuilder()
		newWordList = List[Dictionary[String, Object]]()
		entryList = List[Entry]()
		wordList = List[Word]()

		if entry.Author is None:
			stringBuilder.AppendFormat("http://api.apricotan.net/ping?resource={0}&title={1}&created={2}&modified={3}", urlEncode(entry.Resource.ToString()), urlEncode(entry.Title), urlEncode(entry.Created.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")))
		else:
			stringBuilder.AppendFormat("http://api.apricotan.net/ping?resource={0}&title={1}&author={2}&created={3}&modified={4}", urlEncode(entry.Resource.ToString()), urlEncode(entry.Title), urlEncode(entry.Author), urlEncode(entry.Created.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")), urlEncode(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")))
			
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

					request = WebRequest.Create("http://api.apricotan.net/entries?format=json&limit=25")
					response = None
					stream = None
					streamReader = None

					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						jsonArray = Json.decode(streamReader.ReadToEnd())

						if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
							for obj in jsonArray:
								if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("entry") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["entry"]):
									entry = Entry()

									if obj["entry"].ContainsKey("resource") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["resource"]):
										entry.Resource = Uri(obj["entry"]["resource"])

									if obj["entry"].ContainsKey("title") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["title"]):
										entry.Title = obj["entry"]["title"]

									if obj["entry"].ContainsKey("created") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["created"]):
										entry.Created = DateTime.Parse(obj["entry"]["created"])

									if obj["entry"].ContainsKey("modified") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["modified"]):
										entry.Modified = DateTime.Parse(obj["entry"]["modified"])

									if obj["entry"].ContainsKey("image") and clr.GetClrType(String).IsInstanceOfType(obj["entry"]["image"]):
										entry.Image = Uri(obj["entry"]["image"])

									if obj["entry"].ContainsKey("tags") and clr.GetClrType(Array).IsInstanceOfType(obj["entry"]["tags"]):
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

					request = WebRequest.Create("http://api.apricotan.net/words?format=json&limit=50")
					response = None
					stream = None
					streamReader = None
				
					try:
						response = request.GetResponse()
						stream = response.GetResponseStream()
						streamReader = StreamReader(stream)
						jsonArray = Json.decode(streamReader.ReadToEnd())

						if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
							for obj in jsonArray:
								if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj) and obj.ContainsKey("word") and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["word"]):
									word = Word()

									if obj["word"].ContainsKey("name") and clr.GetClrType(String).IsInstanceOfType(obj["word"]["name"]):
										word.Name = obj["word"]["name"]

									if obj["word"].ContainsKey("attributes") and clr.GetClrType(Array).IsInstanceOfType(obj["word"]["attributes"]):
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

			if remainingCount < 5:
				remainingCount += 1

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
						Script.Instance.Suggest("クラウド", newEntryList)
					else:
						Script.Instance.Suggest("Clouds", newEntryList)
				
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

def onOpened(sender, args):
	global autoUpdate, remainingCount, menuItem, recentEntryList, recentWordList

	menuItem.Items.Clear()

	dashboardMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		dashboardMenuItem.Header = "ダッシュボードを表示..."
	else:
		dashboardMenuItem.Header = "Dashboard..."

	def onDashboardClick(sender, args):
		from System.IO import Path
		global consumerKey, consumerSecret, oauthToken, oauthTokenSecret, remainingCount, likesDictionary
		
		if String.IsNullOrEmpty(oauthToken) or String.IsNullOrEmpty(oauthTokenSecret):
			limitDateTime = DateTime.Today - TimeSpan(7 * 2 - 3, 0, 0, 0)
			limitDictionary = Dictionary[String, Int32]()
			available = 0
			max = 0
			lockedAchievementDictionary = Dictionary[String, Int32]()
			achievementList = List[String]()

			for kvp in likesDictionary:
				for dt in kvp.Value:
					if dt <= limitDateTime:
						if limitDictionary.ContainsKey(kvp.Key):
							limitDictionary[kvp.Key] += 1
						else:
							limitDictionary.Add(kvp.Key, 1)

			if limitDictionary.Count == 0:
				for key in likesDictionary.Keys:
					limitDictionary.Add(key, 0)
					
			for sequence in Script.Instance.Sequences:
				if sequence.Name.Equals("Like") and sequence.State is not None:
					isLocked = True
					requiredLikes = 0
					list = List[Object]()
					likes = likesDictionary[sequence.Owner].Count if likesDictionary.ContainsKey(sequence.Owner) else 0

					for i in range(likes + 11):
						if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
							if i <= likes:
								available += 1
								isLocked = False

							else:
								requiredLikes = i - likes

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
			directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)
			backgroundBrush = None
			textColor = SystemColors.ControlTextBrush
		
			if Directory.Exists(directory):
				fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
				for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
					if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
						exeConfigurationFileMap = ExeConfigurationFileMap()
						exeConfigurationFileMap.ExeConfigFilename = fileName2
						config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
			if config is None:
				config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				directory = None

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
			contentStackPanel = StackPanel()
				
			def onLoaded(sender, args):
				contentStackPanel.Width = Math.Ceiling(contentStackPanel.ActualWidth)

			def onCloseClick(sender, args):
				window.Close()

			window.Owner = Application.Current.MainWindow
			window.Title = Application.Current.MainWindow.Title
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen
			window.ResizeMode = ResizeMode.NoResize
			window.SizeToContent = SizeToContent.WidthAndHeight
			window.Background = SystemColors.WindowBrush 
			window.Loaded += onLoaded

			contentControl = ContentControl()
			contentControl.UseLayoutRounding = True
			contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
			contentControl.VerticalAlignment = VerticalAlignment.Stretch

			window.Content = contentControl

			stackPanel1 = StackPanel()
			stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel1.Orientation = Orientation.Vertical
				
			contentControl.Content = stackPanel1

			stackPanel2 = StackPanel()
			stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
			stackPanel2.VerticalAlignment = VerticalAlignment.Center
			stackPanel2.Orientation = Orientation.Vertical
			stackPanel2.Background = SystemColors.WindowBrush if backgroundBrush is None else backgroundBrush

			stackPanel1.Children.Add(stackPanel2)

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

			stackPanel2.Children.Add(border1)

			contentStackPanel.HorizontalAlignment = HorizontalAlignment.Center
			contentStackPanel.VerticalAlignment = VerticalAlignment.Center
			contentStackPanel.Margin = Thickness(10, 10, 10, 10)
			contentStackPanel.Orientation = Orientation.Vertical

			border1.Child = contentStackPanel

			solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 244, 0, 9))

			if solidColorBrush2.CanFreeze:
				solidColorBrush2.Freeze()

			solidColorBrush3 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 102, 102, 102))

			if solidColorBrush3.CanFreeze:
				solidColorBrush3.Freeze()

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				attachSectionStackPanel(contentStackPanel, textColor, "ステータス")

				if max > 0:
					rate = 100.0 * available / max
						
					attachStackPanelWithProgressBar(contentStackPanel, Thickness(1, 1, 1, 1), textColor, "実績の解除率", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)

				else:
					attachStackPanel(contentStackPanel, Thickness(1, 1, 1, 1), textColor, "実績の解除率", "N/A")
					
				for achievement in achievementList:
					if lockedAchievementDictionary.ContainsKey(achievement):
						if lockedAchievementDictionary[achievement] > 0:
							attachStackPanelWithHint(contentStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "未解除", String.Format("解除に必要な好感度は残り{0}", lockedAchievementDictionary[achievement].ToString()))
							
						else:
							attachStackPanelWithHint(contentStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "未解除", String.Format("解除には10以上の好感度が必要", lockedAchievementDictionary[achievement].ToString()))
						
					else:
						attachStackPanel(contentStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "解除済み")

				for character in Script.Instance.Characters:
					attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("{0}の現在の好感度", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2)

					if limitDictionary.ContainsKey(character.Name):
						attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("3日以内に失う{0}の好感度", character.Name.ToString()), limitDictionary[character.Name].ToString(), solidColorBrush3)

					else:
						attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("3日以内に失う{0}の好感度", character.Name), "N/A", solidColorBrush3)

				attachStackPanel(contentStackPanel, Thickness(1, 0, 1, 1), textColor, "好感度を上げるための残りポイント", remainingCount.ToString())
				attachAnnotationStackPanel(contentStackPanel, textColor, "サインインしていません")
					
			else:
				attachSectionStackPanel(contentStackPanel, textColor, "Me")

				if max > 0:
					rate = 100.0 * available / max
						
					attachStackPanelWithProgressBar(contentStackPanel, Thickness(1, 1, 1, 1), textColor, "Unlock rate of achievements", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)

				else:
					attachStackPanel(contentStackPanel, Thickness(1, 1, 1, 1), textColor, "Unlock rate of achievements", "N/A")

				for achievement in achievementList:
					if lockedAchievementDictionary.ContainsKey(achievement):
						attachStackPanelWithHint(contentStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "Locked", String.Format("{0} {1} needed to unlock", lockedAchievementDictionary[achievement].ToString() if lockedAchievementDictionary[achievement] > 0 else "10+", "Likes" if lockedAchievementDictionary[achievement] > 1 else "Like"))
						
					else:
						attachStackPanel(contentStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "Unlocked")

				for character in Script.Instance.Characters:
					attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Current Likes of {0}", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2)

					if limitDictionary.ContainsKey(character.Name):
						attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Expired Likes of {0} within 3 days", character.Name.ToString()), limitDictionary[character.Name].ToString(), solidColorBrush3)

					else:
						attachStackPanelWithHeart(contentStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Expired Likes of {0} within 3 days", character.Name), "N/A", solidColorBrush3)

				attachStackPanel(contentStackPanel, Thickness(1, 0, 1, 1), textColor, "Remaining points for Like action", remainingCount.ToString())
				attachAnnotationStackPanel(contentStackPanel, textColor, "You are not Signed in")

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

			stackPanel1.Children.Add(border2)

			closeButton = Button()
			closeButton.HorizontalAlignment = HorizontalAlignment.Right
			closeButton.VerticalAlignment = VerticalAlignment.Center
			closeButton.Margin = Thickness(10, 10, 10, 10)
			closeButton.Padding = Thickness(10, 5, 10, 5)
			closeButton.IsDefault = True

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				closeButton.Content = "閉じる"
			else:
				closeButton.Content = "Close"

			closeButton.Click += onCloseClick

			border2.Child = closeButton

			window.Show()

		else:
			hmacsha1 = HMACSHA1(Encoding.ASCII.GetBytes(String.Concat(consumerSecret, "&", oauthTokenSecret)))
			
			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)

			signature = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri("https://api.twitter.com/1.1/account/verify_credentials.json"), sortedDictionary))))

			sortedDictionary.Add("realm", "http://api.twitter.com/")
			sortedDictionary.Add("oauth_signature", signature)

			verifyRequest = WebRequest.Create("https://api.twitter.com/1.1/account/verify_credentials.json")
			verifyRequest.PreAuthenticate = True
			verifyRequest.Method = WebRequestMethods.Http.Get
			verifyRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))

			sortedDictionary = createCommonParameters(consumerKey)
			sortedDictionary.Add("oauth_token", oauthToken)
			sortedDictionary.Add("q", urlEncode("#apricotan"))
			sortedDictionary.Add("result_type", "mixed")
			sortedDictionary.Add("oauth_signature", Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(createSignatureBase(WebRequestMethods.Http.Get, Uri(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode("#apricotan"), "&result_type=mixed")), sortedDictionary)))))

			searchRequest = WebRequest.Create(String.Concat("https://api.twitter.com/1.1/search/tweets.json?q=", urlEncode("#apricotan"), "&result_type=mixed"))
			searchRequest.Method = WebRequestMethods.Http.Get
			searchRequest.Headers.Add(HttpRequestHeader.Authorization, createHttpAuthorizationHeader(sortedDictionary))
			searchRequest.ContentType = "application/json"

			recentLikesDictionary = Dictionary[String, Int32]()

			def onUpdate():
				screenName = None

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
								jsonDictionary = Json.decode(streamReader.ReadToEnd())

								if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("screen_name") and jsonDictionary["screen_name"] is not None and clr.GetClrType(String).IsInstanceOfType(jsonDictionary["screen_name"]):
									screenName = jsonDictionary["screen_name"]

						finally:
							if streamReader is not None:
								streamReader.Close()
						
							if stream is not None:
								stream.Close()
						
							if response is not None:
								response.Close()

						response = None
						stream = None
						streamReader = None

						try:
							response = searchRequest.GetResponse()
							stream = response.GetResponseStream()
							streamReader = StreamReader(stream)
							jsonDictionary = Json.decode(streamReader.ReadToEnd())

							if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("statuses") and clr.GetClrType(Array).IsInstanceOfType(jsonDictionary["statuses"]):
								for status in jsonDictionary["statuses"]:
									if clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(status) and status.ContainsKey("text") and status["text"] is not None:
										hs = HashSet[String]()
										match = Regex.Match(status["text"], "\"(.+?):.+?\"", RegexOptions.CultureInvariant | RegexOptions.Singleline)

										while match.Success:
											if not hs.Contains(match.Groups[1].Value):
												hs.Add(match.Groups[1].Value)

											match = match.NextMatch()

										for name in hs:
											if recentLikesDictionary.ContainsKey(name):
												recentLikesDictionary[name] += 1
											else:
												recentLikesDictionary.Add(name, 1)

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

				return screenName

			def onCompleted(task):
				global remainingCount, likesDictionary, backingDictionary

				limitDateTime = DateTime.Today - TimeSpan(7 * 2 - 3, 0, 0, 0)
				limitDictionary = Dictionary[String, Int32]()
				available = 0
				max = 0
				lockedAchievementDictionary = Dictionary[String, Int32]()
				achievementList = List[String]()

				backingDictionary.Clear()

				for kvp in recentLikesDictionary:
					backingDictionary.Add(kvp.Key, kvp.Value)

				for kvp in likesDictionary:
					for dt in kvp.Value:
						if dt <= limitDateTime:
							if limitDictionary.ContainsKey(kvp.Key):
								limitDictionary[kvp.Key] += 1
							else:
								limitDictionary.Add(kvp.Key, 1)

				if limitDictionary.Count == 0:
					for key in likesDictionary.Keys:
						limitDictionary.Add(key, 0)
					
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Like") and sequence.State is not None:
						isLocked = True
						requiredLikes = 0
						list = List[Object]()
						likes = (likesDictionary[sequence.Owner].Count + backingDictionary[sequence.Owner] if backingDictionary.ContainsKey(sequence.Owner) else likesDictionary[sequence.Owner].Count) if likesDictionary.ContainsKey(sequence.Owner) else backingDictionary[sequence.Owner] if backingDictionary.ContainsKey(sequence.Owner) else 0

						for i in range(likes + 11):
							if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
								if i <= likes:
									available += 1
									isLocked = False

								else:
									requiredLikes = i - likes

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
				directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)
				backgroundBrush = None
				textColor = SystemColors.ControlTextBrush
		
				if Directory.Exists(directory):
					fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
					for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
						if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
							exeConfigurationFileMap = ExeConfigurationFileMap()
							exeConfigurationFileMap.ExeConfigFilename = fileName2
							config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
				if config is None:
					config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
					directory = None

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
				
				def onLoaded(sender, args):
					if rightStackPanel.Children.Count > 0:
						leftStackPanel.Width = rightStackPanel.Width = Math.Ceiling(Math.Max(leftStackPanel.ActualWidth, rightStackPanel.ActualWidth))
					else:
						rightStackPanel.Margin = Thickness(5, 0, 0, 0)
						leftStackPanel.Width = Math.Ceiling(leftStackPanel.ActualWidth)

				def onCloseClick(sender, args):
					window.Close()

				window.Owner = Application.Current.MainWindow
				window.Title = Application.Current.MainWindow.Title
				window.WindowStartupLocation = WindowStartupLocation.CenterScreen
				window.ResizeMode = ResizeMode.NoResize
				window.SizeToContent = SizeToContent.WidthAndHeight
				window.Background = SystemColors.WindowBrush
				window.Loaded += onLoaded

				contentControl = ContentControl()
				contentControl.UseLayoutRounding = True
				contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
				contentControl.VerticalAlignment = VerticalAlignment.Stretch

				window.Content = contentControl

				stackPanel1 = StackPanel()
				stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
				stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
				stackPanel1.Orientation = Orientation.Vertical
				
				contentControl.Content = stackPanel1

				stackPanel2 = StackPanel()
				stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
				stackPanel2.VerticalAlignment = VerticalAlignment.Center
				stackPanel2.Orientation = Orientation.Vertical
				stackPanel2.Background = SystemColors.WindowBrush if backgroundBrush is None else backgroundBrush

				stackPanel1.Children.Add(stackPanel2)

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

				stackPanel2.Children.Add(border1)

				stackPanel3 = StackPanel()
				stackPanel3.HorizontalAlignment = HorizontalAlignment.Center
				stackPanel3.VerticalAlignment = VerticalAlignment.Center
				stackPanel3.Orientation = Orientation.Horizontal

				border1.Child = stackPanel3

				leftStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
				leftStackPanel.VerticalAlignment = VerticalAlignment.Stretch
				leftStackPanel.Margin = Thickness(10, 10, 5, 10)
				leftStackPanel.Orientation = Orientation.Vertical

				rightStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch
				rightStackPanel.VerticalAlignment = VerticalAlignment.Stretch
				rightStackPanel.Margin = Thickness(5, 10, 10, 10)
				rightStackPanel.Orientation = Orientation.Vertical

				stackPanel3.Children.Add(leftStackPanel)
				stackPanel3.Children.Add(rightStackPanel)

				solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 244, 0, 9))

				if solidColorBrush2.CanFreeze:
					solidColorBrush2.Freeze()

				solidColorBrush3 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 102, 102, 102))

				if solidColorBrush3.CanFreeze:
					solidColorBrush3.Freeze()

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					attachSectionStackPanel(leftStackPanel, textColor, "ステータス")

					if max > 0:
						rate = 100.0 * available / max
						
						attachStackPanelWithProgressBar(leftStackPanel, Thickness(1, 1, 1, 1), textColor, "実績の解除率", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)

					else:
						attachStackPanel(leftStackPanel, Thickness(1, 1, 1, 1), textColor, "実績の解除率", "N/A")
					
					for achievement in achievementList:
						if lockedAchievementDictionary.ContainsKey(achievement):
							if lockedAchievementDictionary[achievement] > 0:
								attachStackPanelWithHint(leftStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "未解除", String.Format("解除に必要な好感度は残り{0}", lockedAchievementDictionary[achievement].ToString()))
							
							else:
								attachStackPanelWithHint(leftStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "未解除", String.Format("解除には10以上の好感度が必要", lockedAchievementDictionary[achievement].ToString()))
						
						else:
							attachStackPanel(leftStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "解除済み")

					for character in Script.Instance.Characters:
						if backingDictionary.ContainsKey(character.Name):
							attachStackPanelWithHeartAndAnnotation(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("{0}の現在の好感度", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", String.Concat("+", backingDictionary[character.Name].ToString()), solidColorBrush2)
						else:
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("{0}の現在の好感度", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2)

						if limitDictionary.ContainsKey(character.Name):
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("3日以内に失う{0}の好感度", character.Name.ToString()), limitDictionary[character.Name].ToString(), solidColorBrush3)
					
						else:
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("3日以内に失う{0}の好感度", character.Name), "N/A", solidColorBrush3)

					attachStackPanel(leftStackPanel, Thickness(1, 0, 1, 1), textColor, "好感度を上げるための残りポイント", remainingCount.ToString())
					attachAnnotationStackPanel(leftStackPanel, textColor, "サインインしていません" if task.Result is None else String.Format("{0}でサインイン中", task.Result))

					if recentLikesDictionary.Count > 0:
						sum = 0
						list = List[KeyValuePair[String, Double]]()

						for kvp in recentLikesDictionary:
							sum += kvp.Value
							list.Add(KeyValuePair[String, Double](kvp.Key, kvp.Value))

						def comparison(kvp1, kvp2):
							if kvp1.Value > kvp2.Value:
								return 1

							elif kvp1.Value < kvp2.Value:
								return -1

							return 0

						list.Sort(comparison)
						list.Reverse()

						attachSectionStackPanel(rightStackPanel, textColor, "ランキング")

						for kvp in list:
							attachStackPanelWithHeart(rightStackPanel, Thickness(1, 1, 1, 1) if rightStackPanel.Children.Count == 1 else Thickness(1, 0, 1, 1), textColor, kvp.Key.ToString(), String.Format("{0}%", (100 * kvp.Value / sum).ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2)

						attachAnnotationStackPanel(rightStackPanel, textColor, String.Format("{0}キャラクター", list.Count.ToString()))

				else:
					attachSectionStackPanel(leftStackPanel, textColor, "Me")

					if max > 0:
						rate = 100.0 * available / max
						
						attachStackPanelWithProgressBar(leftStackPanel, Thickness(1, 1, 1, 1), textColor, "Unlock rate of achievements", String.Format("{0}%", rate.ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2, rate)

					else:
						attachStackPanel(leftStackPanel, Thickness(1, 1, 1, 1), textColor, "Unlock rate of achievements", "N/A")

					for achievement in achievementList:
						if lockedAchievementDictionary.ContainsKey(achievement):
							attachStackPanelWithHint(leftStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "Locked", String.Format("{0} {1} needed to unlock", lockedAchievementDictionary[achievement].ToString() if lockedAchievementDictionary[achievement] > 0 else "10+", "Likes" if lockedAchievementDictionary[achievement] > 1 else "Like"))
						
						else:
							attachStackPanel(leftStackPanel, Thickness(1, 0, 1, 1), textColor, achievement, "Unlocked")

					for character in Script.Instance.Characters:
						if backingDictionary.ContainsKey(character.Name):
							attachStackPanelWithHeartAndAnnotation(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Current Likes of {0}", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", String.Concat("+", backingDictionary[character.Name].ToString()), solidColorBrush2)
						else:
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Current Likes of {0}", character.Name), likesDictionary[character.Name].Count.ToString() if likesDictionary.ContainsKey(character.Name) else "N/A", solidColorBrush2)

						if limitDictionary.ContainsKey(character.Name):
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Expired Likes of {0} within 3 days", character.Name.ToString()), limitDictionary[character.Name].ToString(), solidColorBrush3)
					
						else:
							attachStackPanelWithHeart(leftStackPanel, Thickness(1, 0, 1, 1), textColor, String.Format("Expired Likes of {0} within 3 days", character.Name), "N/A", solidColorBrush3)

					attachStackPanel(leftStackPanel, Thickness(1, 0, 1, 1), textColor, "Remaining points for Like action", remainingCount.ToString())
					attachAnnotationStackPanel(leftStackPanel, textColor, "You are not Signed in" if task.Result is None else String.Format("Signed in as {0}", task.Result))

					if recentLikesDictionary.Count > 0:
						sum = 0
						list = List[KeyValuePair[String, Double]]()

						for kvp in recentLikesDictionary:
							sum += kvp.Value
							list.Add(KeyValuePair[String, Double](kvp.Key, kvp.Value))

						def comparison(kvp1, kvp2):
							if kvp1.Value > kvp2.Value:
								return 1

							elif kvp1.Value < kvp2.Value:
								return -1

							return 0

						list.Sort(comparison)
						list.Reverse()

						attachSectionStackPanel(rightStackPanel, textColor, "Trends")

						for kvp in list:
							attachStackPanelWithHeart(rightStackPanel, Thickness(1, 1, 1, 1) if rightStackPanel.Children.Count == 1 else Thickness(1, 0, 1, 1), textColor, kvp.Key.ToString(), String.Format("{0}%", (100 * kvp.Value / sum).ToString("F1", CultureInfo.CurrentCulture), "%"), solidColorBrush2)

						attachAnnotationStackPanel(rightStackPanel, textColor, String.Format("{0} characters", list.Count.ToString()) if list.Count > 1 else String.Format("{0} character", list.Count.ToString()))

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

				stackPanel1.Children.Add(border2)

				closeButton = Button()
				closeButton.HorizontalAlignment = HorizontalAlignment.Right
				closeButton.VerticalAlignment = VerticalAlignment.Center
				closeButton.Margin = Thickness(10, 10, 10, 10)
				closeButton.Padding = Thickness(10, 5, 10, 5)
				closeButton.IsDefault = True

				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					closeButton.Content = "閉じる"
				else:
					closeButton.Content = "Close"

				closeButton.Click += onCloseClick

				border2.Child = closeButton

				window.Show()

			Task.Factory.StartNew[String](onUpdate, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[String]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

	dashboardMenuItem.Click += onDashboardClick

	menuItem.Items.Add(dashboardMenuItem)
	menuItem.Items.Add(Separator())
	
	likeMenuItem = MenuItem()
	
	if remainingCount > 0:
		def onLikeClick(sender, args):
			global remainingCount

			nameList = List[String]()
			currentLikesDictionary = Dictionary[String, List[DateTime]]()
			remainingCount -= 1

			for character in Script.Instance.Characters:
				nameList.Add(character.Name)

			def onUpdate():
				try:
					fileStream = None
					streamReader = None
				
					try:
						nowDateTime = DateTime.Now
						dt1 = nowDateTime - TimeSpan(7 * 2, 0, 0, 0)
						fileStream = FileStream("Likes.json", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
						encoding = UTF8Encoding(False)
						streamReader = StreamReader(fileStream, encoding, True)
						jsonDictionary = Json.decode(streamReader.ReadToEnd())

						if jsonDictionary is None:
							jsonDictionary = Dictionary[String, Object]()
						elif not clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
							return

						for key in nameList.ConvertAll[String](lambda x: Convert.ToBase64String(Encoding.UTF8.GetBytes(x))):
							if not jsonDictionary.ContainsKey(key):
								jsonDictionary.Add(key, None)

						for key in List[String](jsonDictionary.Keys):
							name = Encoding.UTF8.GetString(Convert.FromBase64String(key))
							list = List[String]()

							currentLikesDictionary.Add(name, List[DateTime]())
							
							if jsonDictionary[key] is not None:
								if clr.GetClrType(Array).IsInstanceOfType(jsonDictionary[key]):
									for value in jsonDictionary[key]:
										if clr.GetClrType(String).IsInstanceOfType(value):
											dt2 = DateTime(Int64.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value))))

											if dt2 > dt1:
												currentLikesDictionary[name].Add(dt2)
												list.Add(value)

								else:
									currentLikesDictionary.Clear()

									return

							if nameList.Contains(name):
								currentLikesDictionary[name].Insert(0, nowDateTime)
								list.Insert(0, Convert.ToBase64String(Encoding.UTF8.GetBytes(nowDateTime.Ticks.ToString(CultureInfo.InvariantCulture))))

							if list.Count > 0:
								jsonDictionary[key] = list.ToArray()
							else:
								jsonDictionary.Remove(key)

						json = Json.encode(jsonDictionary)

						if json is not None:
							if fileStream.Length > 0:
								fileStream.SetLength(0)

							streamWriter = None
							
							try:
								streamWriter = StreamWriter(fileStream, encoding)
								streamWriter.Write(json)

							finally:
								if streamWriter is not None:
									streamWriter.Close()

					finally:
						if streamReader is not None:
							streamReader.Close()
							
						if fileStream is not None:
							fileStream.Close()

				except Exception, e:
					Trace.WriteLine(e.clsException.Message)
					Trace.WriteLine(e.clsException.StackTrace)

			def onCompleted(task):
				from System.Windows.Shapes import Path
				global remainingCount, likesDictionary, backingDictionary

				if currentLikesDictionary.Count > 0:
					unlockedAchievementList = List[String]()
					likeSequenceList = List[Sequence]()

					for sequence in Script.Instance.Sequences:
						if sequence.Name.Equals("Like"):
							if sequence.State is not None:
								isLocked = True

								if likesDictionary.ContainsKey(sequence.Owner):
									for i in range(1, likesDictionary[sequence.Owner].Count + 1):
										if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
											isLocked = False

											break

								if isLocked and currentLikesDictionary.ContainsKey(sequence.Owner):
									for i in range(1, currentLikesDictionary[sequence.Owner].Count + 1):
										if Regex.IsMatch(i.ToString(CultureInfo.InvariantCulture), sequence.State, RegexOptions.CultureInvariant):
											list = List[Object]()
												
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
																	if not unlockedAchievementList.Contains(list[i].Name):
																		unlockedAchievementList.Add(list[i].Name)

																	break

											break

							likeSequenceList.Add(sequence)

					unlockedAchievementList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.CurrentCulture))
					
					likesDictionary.Clear()

					for kvp in currentLikesDictionary:
						likesDictionary.Add(kvp.Key, kvp.Value)

						for name in nameList:
							if name.Equals(kvp.Key):
								sequenceList = List[Sequence]()

								for sequence in likeSequenceList:
									if kvp.Key.Equals(sequence.Owner):
										sequenceList.Add(sequence)

								Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, likesDictionary[kvp.Key].Count.ToString(CultureInfo.InvariantCulture)))

								break
						
					window = Window()
					contentControl = ContentControl()
					border1 = Border()
					closeTimer = DispatcherTimer(DispatcherPriority.Background)
					
					def onLoaded(sender, args):
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

						def onCurrentStateInvalidated(sender, args):
							if sender.CurrentState == ClockState.Filling:
								contentControl.Opacity = 1
								contentControl.RenderTransform.ScaleX = 1
								contentControl.RenderTransform.ScaleY = 1
								storyboard.Remove(contentControl)
								closeTimer.Start()

								if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked:
									def onPlay():
										soundPlayer = None

										try:
											soundPlayer = SoundPlayer("Assets\\Like.wav")
											soundPlayer.Load()
											soundPlayer.PlaySync()

										finally:
											if soundPlayer is not None:
												soundPlayer.Dispose()
									
									Task.Factory.StartNew(onPlay)

						storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
						storyboard.Children.Add(da1)
						storyboard.Children.Add(da2)
						storyboard.Children.Add(da3)

						Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
						Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
						Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

						contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

					def onClose(sender, args):
						closeTimer.Stop()

						storyboard = Storyboard()
						da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
						da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
						da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
						sineEase = SineEase()

						sineEase.EasingMode = EasingMode.EaseIn
						da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

						def onCurrentStateInvalidated(sender, args):
							if sender.CurrentState == ClockState.Filling:
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

					closeTimer.Tick += onClose
					closeTimer.Interval = TimeSpan.FromSeconds(3)

					fs = None
					bi = BitmapImage()

					try:
						fs = FileStream("Assets\\Background-Popup.png", FileMode.Open, FileAccess.Read, FileShare.Read)

						bi.BeginInit()
						bi.StreamSource = fs
						bi.CacheOption = BitmapCacheOption.OnLoad
						bi.CreateOptions = BitmapCreateOptions.None
						bi.EndInit()

					finally:
						if fs is not None:
							fs.Close()

					imageBrush = ImageBrush(bi)
					imageBrush.TileMode = TileMode.None
					imageBrush.Stretch = Stretch.Fill
					imageBrush.ViewboxUnits = BrushMappingMode.Absolute
					imageBrush.Viewbox = Rect(0, 0, bi.Width, bi.Height)
					imageBrush.AlignmentX = AlignmentX.Left
					imageBrush.AlignmentY = AlignmentY.Top
					imageBrush.Opacity = 0.5

					dg = DrawingGroup()
					dc = dg.Open()
					dc.DrawRectangle(SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, 0, 0, 0)), None, Rect(0, 0, bi.Width, bi.Height))
					dc.DrawRectangle(imageBrush, None, Rect(0, 0, bi.Width, bi.Height))
					dc.Close()

					window.Owner = Application.Current.MainWindow
					window.Title = Application.Current.MainWindow.Title
					window.WindowStartupLocation = WindowStartupLocation.CenterOwner
					window.AllowsTransparency = True
					window.WindowStyle = WindowStyle.None
					window.ResizeMode = ResizeMode.NoResize
					window.ShowActivated = False
					window.ShowInTaskbar = Application.Current.MainWindow.ContextMenu.Items[5].IsChecked
					window.Topmost = True
					window.SizeToContent = SizeToContent.WidthAndHeight
					window.Background = Brushes.Transparent
					window.Loaded += onLoaded

					contentControl.UseLayoutRounding = True
					contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
					contentControl.VerticalAlignment = VerticalAlignment.Stretch
					contentControl.Opacity = 0
					contentControl.RenderTransform = ScaleTransform(1, 1)
					
					window.Content = contentControl
					
					backgroundBrush = ImageBrush(DrawingImage(dg))
					backgroundBrush.TileMode = TileMode.Tile
					backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
					backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
					backgroundBrush.Stretch = Stretch.None

					if backgroundBrush.CanFreeze:
						backgroundBrush.Freeze()

					border1.HorizontalAlignment = HorizontalAlignment.Center
					border1.VerticalAlignment = VerticalAlignment.Center
					border1.Padding = Thickness(4)
					border1.CornerRadius = CornerRadius(4)
					border1.Background = backgroundBrush

					contentControl.Content = border1

					stackPanel1 = StackPanel()
					stackPanel1.HorizontalAlignment = HorizontalAlignment.Center
					stackPanel1.VerticalAlignment = VerticalAlignment.Center
					stackPanel1.Orientation = Orientation.Vertical
					stackPanel1.Background = Brushes.Transparent

					border1.Child = stackPanel1

					stackPanel2 = StackPanel()
					stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
					stackPanel2.VerticalAlignment = VerticalAlignment.Center
					stackPanel2.Margin = Thickness(0, 4, 0, 0)
					stackPanel2.Orientation = Orientation.Horizontal
					stackPanel2.Background = Brushes.Transparent

					stackPanel1.Children.Add(stackPanel2)

					solidColorBrush1 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 244, 0, 9))

					if solidColorBrush1.CanFreeze:
						solidColorBrush1.Freeze()

					fontSizeConverter = FontSizeConverter()

					for kvp in likesDictionary:
						stackPanel3 = StackPanel()
						stackPanel3.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel3.VerticalAlignment = VerticalAlignment.Stretch
						stackPanel3.Margin = Thickness(4, 0, 4, 0)
						stackPanel3.Orientation = Orientation.Vertical
						stackPanel3.Background = Brushes.Transparent

						stackPanel2.Children.Add(stackPanel3)

						stackPanel4 = StackPanel()
						stackPanel4.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel4.VerticalAlignment = VerticalAlignment.Stretch
						stackPanel4.Orientation = Orientation.Horizontal
						stackPanel4.Background = Brushes.Transparent

						stackPanel3.Children.Add(stackPanel4)

						heartGeometry = createHeartGeometry(Rect(0, 0, 20, 15))

						if heartGeometry.CanFreeze:
							heartGeometry.Freeze()

						path = Path()
						path.HorizontalAlignment = HorizontalAlignment.Stretch
						path.VerticalAlignment = VerticalAlignment.Center
						path.Fill = solidColorBrush1
						path.Data = heartGeometry

						dropShadowEffect1 = DropShadowEffect()
						dropShadowEffect1.BlurRadius = 1
						dropShadowEffect1.Color = Colors.Black
						dropShadowEffect1.Direction = 270
						dropShadowEffect1.Opacity = 0.5
						dropShadowEffect1.ShadowDepth = 1

						if dropShadowEffect1.CanFreeze:
							dropShadowEffect1.Freeze()

						path.Effect = dropShadowEffect1

						stackPanel4.Children.Add(path)

						border2 = Border()
						border2.HorizontalAlignment = HorizontalAlignment.Stretch
						border2.VerticalAlignment = VerticalAlignment.Stretch
						border2.Margin = Thickness(0)
						border2.Padding = Thickness(0)
						border2.CornerRadius = CornerRadius(0)
						border2.Background = Brushes.Transparent
						
						dropShadowEffect2 = DropShadowEffect()
						dropShadowEffect2.BlurRadius = 1
						dropShadowEffect2.Color = Colors.Black
						dropShadowEffect2.Direction = 270
						dropShadowEffect2.Opacity = 0.5
						dropShadowEffect2.ShadowDepth = 1

						if dropShadowEffect2.CanFreeze:
							dropShadowEffect2.Freeze()

						border2.Effect = dropShadowEffect2

						stackPanel4.Children.Add(border2)

						label1 = Label()
						label1.HorizontalAlignment = HorizontalAlignment.Stretch
						label1.VerticalAlignment = VerticalAlignment.Stretch
						label1.FontSize = fontSizeConverter.ConvertFromString("24pt")
						label1.Foreground = Brushes.White
						label1.FontWeight = FontWeights.Bold
						label1.Content = kvp.Value.Count.ToString()

						RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)

						border2.Child = label1

						if backingDictionary.ContainsKey(kvp.Key):
							border3 = Border()
							border3.HorizontalAlignment = HorizontalAlignment.Stretch
							border3.VerticalAlignment = VerticalAlignment.Top
							border3.Margin = Thickness(0)
							border3.Padding = Thickness(0)
							border3.CornerRadius = CornerRadius(0)
							border3.Background = Brushes.Transparent

							dropShadowEffect3 = DropShadowEffect()
							dropShadowEffect3.BlurRadius = 1
							dropShadowEffect3.Color = Colors.Black
							dropShadowEffect3.Direction = 270
							dropShadowEffect3.Opacity = 0.5
							dropShadowEffect3.ShadowDepth = 1

							if dropShadowEffect3.CanFreeze:
								dropShadowEffect3.Freeze()

							border3.Effect = dropShadowEffect2

							stackPanel4.Children.Add(border3)

							label2 = Label()
							label2.HorizontalAlignment = HorizontalAlignment.Stretch
							label2.VerticalAlignment = VerticalAlignment.Stretch
							label2.FontSize = fontSizeConverter.ConvertFromString("8pt")
							label2.Foreground = Brushes.White
							label2.FontWeight = FontWeights.Bold
							label2.Content = String.Concat("+", backingDictionary[kvp.Key].ToString())

							RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)

							border3.Child = label2

						border4 = Border()
						border4.HorizontalAlignment = HorizontalAlignment.Center
						border4.VerticalAlignment = VerticalAlignment.Stretch
						border4.Margin = Thickness(0)
						border4.Padding = Thickness(0)
						border4.CornerRadius = CornerRadius(0)
						border4.Background = Brushes.Transparent

						stackPanel3.Children.Add(border4)

						dropShadowEffect4 = DropShadowEffect()
						dropShadowEffect4.BlurRadius = 1
						dropShadowEffect4.Color = Colors.Black
						dropShadowEffect4.Direction = 270
						dropShadowEffect4.Opacity = 0.5
						dropShadowEffect4.ShadowDepth = 1

						if dropShadowEffect4.CanFreeze:
							dropShadowEffect4.Freeze()

						border4.Effect = dropShadowEffect4
	
						label3 = Label()
						label3.HorizontalAlignment = HorizontalAlignment.Stretch
						label3.VerticalAlignment = VerticalAlignment.Stretch
						label3.FontSize = fontSizeConverter.ConvertFromString("8pt")
						label3.Foreground = Brushes.White
						label3.FontWeight = FontWeights.Bold
						label3.Content = kvp.Key

						RenderOptions.SetClearTypeHint(label3, ClearTypeHint.Enabled)

						border4.Child = label3
	
					border5 = Border()
					border5.HorizontalAlignment = HorizontalAlignment.Center
					border5.VerticalAlignment = VerticalAlignment.Stretch
					border5.Margin = Thickness(0, 0, 0, 4)
					border5.Padding = Thickness(0)
					border5.CornerRadius = CornerRadius(0)
					border5.Background = Brushes.Transparent

					dropShadowEffect = DropShadowEffect()
					dropShadowEffect.BlurRadius = 1
					dropShadowEffect.Color = Colors.Black
					dropShadowEffect.Direction = 270
					dropShadowEffect.Opacity = 0.5
					dropShadowEffect.ShadowDepth = 1

					if dropShadowEffect.CanFreeze:
						dropShadowEffect.Freeze()

					border5.Effect = dropShadowEffect

					stackPanel1.Children.Add(border5)

					label4 = Label()
					label4.HorizontalAlignment = HorizontalAlignment.Stretch
					label4.VerticalAlignment = VerticalAlignment.Stretch
					label4.FontSize = fontSizeConverter.ConvertFromString("8pt")
					label4.Foreground = Brushes.White
					label4.FontWeight = FontWeights.Normal

					if unlockedAchievementList.Count > 0:
						if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
							label4.Content = "実績のロックが解除されました！"
						else:
							label4.Content = "Achievement unlocked."

						solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 204, 204, 204))

						if solidColorBrush2.CanFreeze:
							solidColorBrush2.Freeze()

						for achievement in unlockedAchievementList:
							border6 = Border()
							border6.HorizontalAlignment = HorizontalAlignment.Center
							border6.VerticalAlignment = VerticalAlignment.Stretch
							border6.Margin = Thickness(0, 0, 0, 4)
							border6.Padding = Thickness(0)
							border6.CornerRadius = CornerRadius(0)
							border6.Background = Brushes.Transparent

							effect = DropShadowEffect()
							effect.BlurRadius = 1
							effect.Color = Colors.Black
							effect.Direction = 270
							effect.Opacity = 0.5
							effect.ShadowDepth = 1

							if effect.CanFreeze:
								effect.Freeze()

							border6.Effect = effect

							stackPanel1.Children.Add(border6)

							label5 = Label()
							label5.HorizontalAlignment = HorizontalAlignment.Stretch
							label5.VerticalAlignment = VerticalAlignment.Stretch
							label5.FontSize = fontSizeConverter.ConvertFromString("8pt")
							label5.Foreground = solidColorBrush2
							label5.FontWeight = FontWeights.Normal
							label5.Content = achievement

							RenderOptions.SetClearTypeHint(label5, ClearTypeHint.Enabled)

							border6.Child = label5

					else:
						if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
							label4.Content = "好感度が上がりました！"
						else:
							label4.Content = "Likes increased."
					
					RenderOptions.SetClearTypeHint(label4, ClearTypeHint.Enabled)

					border5.Child = label4
					
					window.Show()

				else:
					remainingCount += 1

			Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())
		
		stackPanel = StackPanel()
		stackPanel.HorizontalAlignment = HorizontalAlignment.Left
		stackPanel.VerticalAlignment = VerticalAlignment.Top
		stackPanel.Orientation = Orientation.Horizontal

		likeMenuItem.Header = stackPanel
		likeMenuItem.Click += onLikeClick
		
		menuItem.Items.Add(likeMenuItem)

		textBlock1 = TextBlock()
		textBlock1.HorizontalAlignment = HorizontalAlignment.Left

		textBlock2 = TextBlock()
		textBlock2.HorizontalAlignment = HorizontalAlignment.Right
		textBlock2.Margin = Thickness(10, 0, 0, 0)
		textBlock2.Foreground = SystemColors.GrayTextBrush

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			textBlock1.Text = "好感度を上げる"
			textBlock2.Text = "N/A" if remainingCount is None else String.Concat("残り ", remainingCount.ToString())
		
		else:
			textBlock1.Text = "Like"
			textBlock2.Text = "N/A" if remainingCount is None else String.Concat(remainingCount.ToString(), " remaining")

		stackPanel.Children.Add(textBlock1)
		stackPanel.Children.Add(textBlock2)

	else:
		likeMenuItem.IsEnabled = False

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			likeMenuItem.Header = "好感度を上げる"
		else:
			likeMenuItem.Header = "Like"

		menuItem.Items.Add(likeMenuItem)

	menuItem.Items.Add(Separator())

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
		def onSignInWithTwitterClick(sender, args):
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
					webBrowser = WebBrowser()
						
					def onWindowLoaded(sender, args):
						webBrowser.Navigate(Uri(String.Concat("https://api.twitter.com/oauth/authorize?oauth_token=", dictionary["oauth_token"])))

					def onVerifyClick(sender, args):
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
											fs = None
											sr = None
											sw = None

											try:
												fs = FileStream(__file__, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
												encoding = UTF8Encoding(False)
												sr = StreamReader(fs, encoding, True)
												lines = Regex.Replace(Regex.Replace(sr.ReadToEnd(), "oauthToken\\s*=\\s*None", String.Format("oauthToken = \"{0}\"", d["oauth_token"]), RegexOptions.CultureInvariant), "oauthTokenSecret\\s*=\\s*None", String.Format("oauthTokenSecret = \"{0}\"", d["oauth_token_secret"]), RegexOptions.CultureInvariant)
												fs.SetLength(0)
												sw = StreamWriter(fs, encoding)
												sw.Write(lines)

											finally:
												if sw is not None:
													sw.Close()

												if sr is not None:
													sr.Close()

												if fs is not None:
													fs.Close()

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

					def onCancelClick(sender, args):
						window.Close()
			
					window.Owner = Application.Current.MainWindow
					window.Title = Application.Current.MainWindow.Title
					window.WindowStartupLocation = WindowStartupLocation.CenterScreen
					window.ResizeMode = ResizeMode.NoResize
					window.SizeToContent = SizeToContent.WidthAndHeight
					window.Background = SystemColors.WindowBrush
					window.Loaded += onWindowLoaded

					stackPanel1 = StackPanel()
					stackPanel1.UseLayoutRounding = True
					stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
					stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
					stackPanel1.Orientation = Orientation.Vertical

					solidColorBrush1 = SolidColorBrush(Colors.Black)
					solidColorBrush1.Opacity = 0.25

					if solidColorBrush1.CanFreeze:
						solidColorBrush1.Freeze()

					window.Content = stackPanel1
					
					border1 = Border()
					border1.HorizontalAlignment = HorizontalAlignment.Stretch
					border1.VerticalAlignment = VerticalAlignment.Stretch
					border1.BorderThickness = Thickness(0, 0, 0, 1)
					border1.BorderBrush = solidColorBrush1

					stackPanel1.Children.Add(border1)

					webBrowser.HorizontalAlignment = HorizontalAlignment.Stretch
					webBrowser.VerticalAlignment = VerticalAlignment.Stretch
					webBrowser.Width = 640
					webBrowser.Height = 480

					border1.Child = webBrowser

					solidColorBrush2 = SolidColorBrush(Colors.White)
					solidColorBrush2.Opacity = 0.5

					if solidColorBrush2.CanFreeze:
						solidColorBrush2.Freeze()

					border2 = Border()
					border2.HorizontalAlignment = HorizontalAlignment.Stretch
					border2.VerticalAlignment = VerticalAlignment.Stretch
					border2.BorderThickness = Thickness(0, 1, 0, 0)
					border2.BorderBrush = solidColorBrush2

					stackPanel1.Children.Add(border2)

					stackPanel2 = StackPanel()
					stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
					stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
					stackPanel2.Orientation = Orientation.Horizontal

					border2.Child = stackPanel2

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
					verifyButton.Padding = Thickness(10, 5, 10, 5)
					verifyButton.IsDefault = True

					cancelButton = Button()
					cancelButton.HorizontalAlignment = HorizontalAlignment.Right
					cancelButton.VerticalAlignment = VerticalAlignment.Center
					cancelButton.Margin = Thickness(5, 10, 10, 10)
					cancelButton.Padding = Thickness(10, 5, 10, 5)

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

					window.Show()

			Task.Factory.StartNew(onOAuthRequestToken, TaskCreationOptions.LongRunning).ContinueWith(onOAuthRequestTokenCompleted, context)

		signInWithTwitterMenuItem = MenuItem()
	
		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			signInWithTwitterMenuItem.Header = "Twitterアカウントでサインイン..."
		else:
			signInWithTwitterMenuItem.Header = "Sign in with Twitter..."

		signInWithTwitterMenuItem.Click += onSignInWithTwitterClick

		menuItem.Items.Insert(0, signInWithTwitterMenuItem)
		menuItem.Items.Insert(1, Separator())

		if askMenuItem is not None:
			askMenuItem.IsEnabled = False

			menuItem.Items.Add(askMenuItem)
		
	elif askMenuItem is not None:
		def onClick(sender, args):
			from System.IO import Path

			config = None
			directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)
			backgroundBrush = None
			textColor = SystemColors.ControlTextBrush
			
			if Directory.Exists(directory):
				fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
				for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
					if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
						exeConfigurationFileMap = ExeConfigurationFileMap()
						exeConfigurationFileMap.ExeConfigFilename = fileName2
						config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
			if config is None:
				config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				directory = None

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
			window.Owner = Application.Current.MainWindow
			window.Title = Application.Current.MainWindow.Title
			window.WindowStartupLocation = WindowStartupLocation.CenterOwner
			window.ResizeMode = ResizeMode.NoResize
			window.SizeToContent = SizeToContent.WidthAndHeight
			window.Background = SystemColors.WindowBrush
				
			stackPanel1 = StackPanel()
			stackPanel1.UseLayoutRounding = True
			stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel1.Orientation = Orientation.Vertical

			window.Content = stackPanel1

			stackPanel2 = StackPanel()
			stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel2.Orientation = Orientation.Vertical
			stackPanel2.Background = SystemColors.WindowBrush if backgroundBrush is None else backgroundBrush

			stackPanel1.Children.Add(stackPanel2)

			gradientStopCollection = GradientStopCollection()
			gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
			gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

			linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
			linearGradientBrush.Opacity = 0.1

			if linearGradientBrush.CanFreeze:
				linearGradientBrush.Freeze()

			stackPanel3 = StackPanel()
			stackPanel3.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel3.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel3.Orientation = Orientation.Vertical
			stackPanel3.Background = linearGradientBrush

			stackPanel2.Children.Add(stackPanel3)

			solidColorBrush1 = SolidColorBrush(Colors.Black)
			solidColorBrush1.Opacity = 0.25

			if solidColorBrush1.CanFreeze:
				solidColorBrush1.Freeze()

			border1 = Border()
			border1.HorizontalAlignment = HorizontalAlignment.Stretch
			border1.VerticalAlignment = VerticalAlignment.Stretch
			border1.BorderThickness = Thickness(0, 0, 0, 1)
			border1.BorderBrush = solidColorBrush1

			stackPanel3.Children.Add(border1)

			stackPanel4 = StackPanel()
			stackPanel4.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel4.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel4.Orientation = Orientation.Vertical
			stackPanel4.Margin = Thickness(10, 10, 10, 20)

			border1.Child = stackPanel4

			stackPanel5 = StackPanel()
			stackPanel5.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel5.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel5.Orientation = Orientation.Vertical

			dropShadowEffect = DropShadowEffect()
			dropShadowEffect.BlurRadius = 1
			dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(textColor.R, textColor.G), textColor.B) > Byte.MaxValue / 2 else Colors.White
			dropShadowEffect.Direction = 270
			dropShadowEffect.Opacity = 0.5
			dropShadowEffect.ShadowDepth = 1

			if dropShadowEffect.CanFreeze:
				dropShadowEffect.Freeze()

			stackPanel5.Effect = dropShadowEffect

			stackPanel4.Children.Add(stackPanel5)

			label = Label()
			label.Foreground = textBrush

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				label.Content = "話しかける一言"
			else:
				label.Content = "Message"

			RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
			
			textColor = SystemColors.ControlText if textBrush is None else textBrush.Color

			stackPanel5.Children.Add(label)

			textBox = TextBox()
			textBox.Width = 240
			
			stackPanel4.Children.Add(textBox)

			solidColorBrush2 = SolidColorBrush(Colors.White)
			solidColorBrush2.Opacity = 0.5

			if solidColorBrush2.CanFreeze:
				solidColorBrush2.Freeze()

			border2 = Border()
			border2.HorizontalAlignment = HorizontalAlignment.Stretch
			border2.VerticalAlignment = VerticalAlignment.Stretch
			border2.BorderThickness = Thickness(0, 1, 0, 0)
			border2.BorderBrush = solidColorBrush2

			stackPanel1.Children.Add(border2)
				
			def onAskClick(sender, rea):
				if not String.IsNullOrEmpty(textBox.Text):
					ask(textBox.Text)

				window.Close()

			askButton = Button()
			askButton.HorizontalAlignment = HorizontalAlignment.Right
			askButton.VerticalAlignment = VerticalAlignment.Center
			askButton.Margin = Thickness(10, 10, 10, 10)
			askButton.Padding = Thickness(10, 5, 10, 5)
			askButton.IsDefault = True

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				askButton.Content = "話しかける"
			else:
				askButton.Content = "Ask"

			askButton.Click += onAskClick

			border2.Child = askButton

			textBox.Focus()
			window.Show()

		askMenuItem.Click += onClick

		menuItem.Items.Add(askMenuItem)
	
	learnMenuItem = MenuItem()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		learnMenuItem.Header = "言葉を教える..."
	else:
		learnMenuItem.Header = "Learn..."

	def onLearnClick1(sender, args):
		from System.IO import Path

		config = None
		directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)
		backgroundBrush = None
		textColor = SystemColors.ControlTextBrush
			
		if Directory.Exists(directory):
			fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
			for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
				if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
					exeConfigurationFileMap = ExeConfigurationFileMap()
					exeConfigurationFileMap.ExeConfigFilename = fileName2
					config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
		if config is None:
			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
			directory = None

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
		stackPanel2.Background = SystemColors.WindowBrush if backgroundBrush is None else backgroundBrush

		gradientStopCollection = GradientStopCollection()
		gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
		gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

		linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
		linearGradientBrush.Opacity = 0.1

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
			label.Content = "教える言葉"
		else:
			label.Content = "Word"

		RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
			
		textColor = SystemColors.ControlText if textBrush is None else textBrush.Color

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
			
		border1.Child = stackPanel4

		stackPanel3.Children.Add(border1)
		stackPanel2.Children.Add(stackPanel3)
		stackPanel1.Children.Add(stackPanel2)

		def onLearnClick2(sender, args):
			if not String.IsNullOrEmpty(textBox.Text):
				Script.Instance.Learn(textBox.Text)

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

		learnButton = Button()
		learnButton.HorizontalAlignment = HorizontalAlignment.Right
		learnButton.VerticalAlignment = VerticalAlignment.Center
		learnButton.Margin = Thickness(10, 10, 10, 10)
		learnButton.Padding = Thickness(10, 5, 10, 5)
		learnButton.IsDefault = True

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			learnButton.Content = "教える"
		else:
			learnButton.Content = "Learn"

		learnButton.Click += onLearnClick2

		border2.Child = learnButton
		stackPanel1.Children.Add(border2)
		textBox.Focus()
			
		window.Owner = Application.Current.MainWindow
		window.Title = Application.Current.MainWindow.Title
		window.WindowStartupLocation = WindowStartupLocation.CenterOwner
		window.ResizeMode = ResizeMode.NoResize
		window.SizeToContent = SizeToContent.WidthAndHeight
		window.Background = SystemColors.WindowBrush
		window.Content = stackPanel1
		window.Show()
	
	learnMenuItem.Click += onLearnClick1

	menuItem.Items.Add(learnMenuItem)
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
	
	maxLength = 15
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
				def onLaunch(state):
					Process.Start(state)

				Task.Factory.StartNew(onLaunch, sender.Tag.ToString())

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

def onIsVisibleChanged(sender, args):
	from System.Windows.Shapes import Path
	global chargesDictionary, imageDictionary, likesDictionary, trendsDictionary

	if args.NewValue == True and sender.Messages.Count > 0:
		max = 1.5
		dictionary = Dictionary[String, Double]()
		
		for o in sender.Messages[sender.Messages.Count - 1]:
			if clr.GetClrType(Entry).IsInstanceOfType(o):
				for word in Script.Instance.Words:
					if word.Name.Equals(o.Title):
						score1 = (Math.Log(0.001, Math.E) - Math.Log(0.025 if o.Score is None else o.Score if o.Score > 0.025 else 0.025, Math.E)) / Math.Log(0.001, Math.E)
						sum1 = None

						if chargesDictionary.ContainsKey(o.Title):
							sum2 = 0.0

							for score2 in chargesDictionary[o.Title]:
								sum2 += score2
		
							if sum2 < max:
								chargesDictionary[o.Title].Add(score1)
								sum1 = Nullable[Double](sum2)

						else:
							chargesDictionary.Add(o.Title, List[Double]([score1]))
							sum1 = Nullable[Double](0.0)

						if not dictionary.ContainsKey(o.Title) and sum1 is not None:
							if Math.Floor(sum1 / (max / 2)) < Math.Floor((sum1 + score1) / (max / 2)):
								dictionary.Add(o.Title, sum1)

						if o.Image is None:
							if not String.IsNullOrEmpty(o.Description):
								entry = o.Clone()
								entry.NextImage()

								if entry.Image is not None:
									if imageDictionary.ContainsKey(o.Title):
										imageDictionary[o.Title] = entry.Image

									else:
										imageDictionary.Add(o.Title, entry.Image)

						else:
							if imageDictionary.ContainsKey(o.Title):
								imageDictionary[o.Title] = o.Image

							else:
								imageDictionary.Add(o.Title, o.Image)
					
						break

		if dictionary.Count > 0:
			termList = List[String]()
			tempTermList1 = List[String]()
			tempTermList2 = List[String]()
			isCharged = False

			for kvp in chargesDictionary:
				sum = 0.0

				for score in kvp.Value:
					sum += score

				if dictionary.ContainsKey(kvp.Key):
					tempTermList1.Add(kvp.Key)

					if sum >= max:
						tempTermList2.Add(kvp.Key)
						isCharged = True

				elif sum >= max:
					termList.Add(kvp.Key)
					tempTermList2.Add(kvp.Key)

			if isCharged:
				sequenceList = List[Sequence]()
								
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Charge"):
						sequenceList.Add(sequence)

				Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, tempTermList2.Count.ToString(CultureInfo.InvariantCulture)))

			termList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.CurrentCulture))
			tempTermList1.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.CurrentCulture))
			termList.AddRange(tempTermList1)

			isLocked = False
			termHashSet = HashSet[String]()
			imageHashSet = HashSet[Uri]()
									
			if tempTermList2.Count >= 5:
				for i in range(3):
					termHashSet.Add(tempTermList2[i])

				isLocked = True

			window = Window()
			contentControl = ContentControl()
			border1 = Border()
			stackPanel1 = StackPanel()
			closeTimer = DispatcherTimer(DispatcherPriority.Background)
			
			def onLoaded1(sender, args):
				border1.Width = contentControl.ActualWidth
				border1.Height = contentControl.ActualHeight
				contentControl.Width = contentControl.ActualWidth * 1.5
				contentControl.Height = contentControl.ActualHeight * 1.5
				contentControl.RenderTransform.CenterX = contentControl.Width / 2
				contentControl.RenderTransform.CenterY = contentControl.Height / 2

				for element1 in stackPanel1.Children:
					for element2 in element1.Children:
						for element3 in element2.Child.Children:
							if clr.GetClrType(Grid).IsInstanceOfType(element3):
								if clr.GetClrType(ScaleTransform).IsInstanceOfType(element3.RenderTransform):
									element3.RenderTransform.CenterX = element3.ActualWidth / 2
									element3.RenderTransform.CenterY = element3.ActualHeight / 2

				storyboard1 = Storyboard()
				da1 = DoubleAnimation(contentControl.Opacity, 1, TimeSpan.FromMilliseconds(500))
				da2 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
				da3 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
				sineEase = SineEase()

				sineEase.EasingMode = EasingMode.EaseOut
				da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

				def onCurrentStateInvalidated1(sender, args):
					if sender.CurrentState == ClockState.Filling:
						contentControl.Opacity = 1
						contentControl.RenderTransform.ScaleX = 1
						contentControl.RenderTransform.ScaleY = 1
						storyboard1.Remove(contentControl)

						storyboard2 = Storyboard()

						def onCurrentStateInvalidated2(sender, args):
							if sender.CurrentState == ClockState.Filling:
								isCharged = False

								if isLocked:
									for key in termHashSet:
										if not chargesDictionary.ContainsKey(key):
											if not border1.Tag:
												closeTimer.Start()

											break

									else:
										def comparison(s1, s2):
											sum1 = 0.0
											sum2 = 0.0

											for score in chargesDictionary[s1]:
												sum1 += score

											for score in chargesDictionary[s2]:
												sum2 += score
																			
											if sum1 / chargesDictionary[s1].Count > sum2 / chargesDictionary[s2].Count:
												return 1

											elif sum1 / chargesDictionary[s1].Count < sum2 / chargesDictionary[s2].Count:
												return -1
																			
											return 0

										list = List[String](termHashSet)
										list.Sort(comparison)
										list.Reverse()

										storyboard = Storyboard()

										def onCurrentStateInvalidated3(sender, args):
											if sender.CurrentState == ClockState.Filling and not border1.Tag:
												closeTimer.Start()

										storyboard.CurrentStateInvalidated += onCurrentStateInvalidated3
											
										for element1 in stackPanel1.Children:
											for element2 in element1.Children:
												if not isCharged:
													for element3 in element2.Child.Children:
														if clr.GetClrType(Grid).IsInstanceOfType(element3):
															for element4 in element3.Children:
																if clr.GetClrType(Grid).IsInstanceOfType(element4) and dictionary.ContainsKey(element2.Child.Tag):
																	sum = 0.0

																	for score in element4.Tag:
																		sum += score

																	if sum >= max:
																		isCharged = True

																		break

															if isCharged:
																break

												index = list.IndexOf(element2.Child.Tag)

												if index >= 0:
													element2.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
													ca = ColorAnimation(element2.Background.Color, Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
													sineEase = SineEase()

													sineEase.EasingMode = EasingMode.EaseOut
													ca.EasingFunction = sineEase
													ca.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(250 * index))

													storyboard.Children.Add(ca)
						
													Storyboard.SetTarget(ca, element2)
													Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

										storyboard.Begin()

								else:
									if not border1.Tag:
										closeTimer.Start()

									for element1 in stackPanel1.Children:
										for element2 in element1.Children:
											for element3 in element2.Child.Children:
												if clr.GetClrType(Grid).IsInstanceOfType(element3):
													for element4 in element3.Children:
														if clr.GetClrType(Grid).IsInstanceOfType(element4) and dictionary.ContainsKey(element2.Child.Tag):
															sum = 0.0

															for score in element4.Tag:
																sum += score

															if sum >= max:
																element3.RenderTransform = ScaleTransform(1, 1)
																element3.RenderTransform.CenterX = element3.ActualWidth / 2
																element3.RenderTransform.CenterY = element3.ActualHeight / 2

																def onMouseEnter(sender1, args1):
																	if not termHashSet.Contains(sender1.Child.Tag):
																		if sender1.Tag is not None:
																			sender1.Tag.Stop(sender1)

																		sender1.Tag = storyboard1 = Storyboard()
																		ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
																		sineEase = SineEase()

																		sineEase.EasingMode = EasingMode.EaseOut
																		ca.EasingFunction = sineEase

																		def onCurrentStateInvalidated3(sender2, args2):
																			if sender2.CurrentState == ClockState.Filling:
																				sender1.Background = SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102))
																				sender1.Tag = None
																				storyboard1.Remove(sender1)

																		storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated3
																		storyboard1.Children.Add(ca)

																		Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

																		sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)
						
																		for e1 in sender1.Child.Children:
																			if clr.GetClrType(Grid).IsInstanceOfType(e1):
																				if e1.Tag is not None:
																					e1.Tag.Stop(e1)

																				e1.Tag = storyboard2 = Storyboard()
																				da1 = DoubleAnimation(e1.RenderTransform.ScaleX, 1.1, TimeSpan.FromMilliseconds(500))
																				da2 = DoubleAnimation(e1.RenderTransform.ScaleY, 1.1, TimeSpan.FromMilliseconds(500))
																				sineEase = SineEase()

																				sineEase.EasingMode = EasingMode.EaseOut
																				da1.EasingFunction = da2.EasingFunction = sineEase

																				def onCurrentStateInvalidated4(sender2, args2):
																					if sender2.CurrentState == ClockState.Filling:
																						for e2 in sender1.Child.Children:
																							if e2.Tag == storyboard2:
																								e2.RenderTransform.ScaleX = 1.1
																								e2.RenderTransform.ScaleY = 1.1
																								e2.Tag = None
																								storyboard2.Remove(e2)

																				storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated4
																				storyboard2.Children.Add(da1)
																				storyboard2.Children.Add(da2)

																				Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																				Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
																				e1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)

																def onMouseLeave(sender1, args1):
																	if not termHashSet.Contains(sender1.Child.Tag):
																		if sender1.Tag is not None:
																			sender1.Tag.Stop(sender1)

																		sender1.Tag = storyboard1 = Storyboard()
																		ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(0, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
																		sineEase = SineEase()

																		sineEase.EasingMode = EasingMode.EaseIn
																		ca.EasingFunction = sineEase

																		def onCurrentStateInvalidated3(sender2, args2):
																			if sender2.CurrentState == ClockState.Filling:
																				sender1.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
																				sender1.Tag = None
																				storyboard1.Remove(sender1)

																		storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated3
																		storyboard1.Children.Add(ca)

																		Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

																		sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

																		for e1 in sender1.Child.Children:
																			if clr.GetClrType(Grid).IsInstanceOfType(e1):
																				if e1.Tag is not None:
																					e1.Tag.Stop(e1)

																				e1.Tag = storyboard2 = Storyboard()
																				da1 = DoubleAnimation(e1.RenderTransform.ScaleX, 1, TimeSpan.FromMilliseconds(500))
																				da2 = DoubleAnimation(e1.RenderTransform.ScaleY, 1, TimeSpan.FromMilliseconds(500))
																				sineEase = SineEase()

																				sineEase.EasingMode = EasingMode.EaseIn
																				da1.EasingFunction = da2.EasingFunction = sineEase

																				def onCurrentStateInvalidated4(sender2, args2):
																					if sender2.CurrentState == ClockState.Filling:
																						for e2 in sender1.Child.Children:
																							if e2.Tag == storyboard2:
																								e2.RenderTransform.ScaleX = 1
																								e2.RenderTransform.ScaleY = 1
																								e2.Tag = None
																								storyboard2.Remove(e2)

																				storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated4
																				storyboard2.Children.Add(da1)
																				storyboard2.Children.Add(da2)

																				Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																				Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
																				e1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)

																def onMouseLeftButtonUp(sender1, args1):
																	from System.IO import Path as _Path
																	global remainingCount

																	if termHashSet.Count < 3:
																		if sender1.Tag is not None:
																			sender1.Tag.Stop(sender1)
																		
																		if termHashSet.Contains(sender1.Child.Tag):
																			termHashSet.Remove(sender1.Child.Tag)
																			
																			sender1.Tag = storyboard = Storyboard()
																			color = Color.FromArgb(0, Byte.MaxValue, 0, 102)
																			ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
																			sineEase = SineEase()

																			sineEase.EasingMode = EasingMode.EaseIn
																			ca.EasingFunction = sineEase

																			def onCurrentStateInvalidated3(sender2, args2):
																				if sender2.CurrentState == ClockState.Filling:
																					sender1.Background = SolidColorBrush(color)
																					sender1.Tag = None
																					storyboard.Remove(sender1)

																			storyboard.CurrentStateInvalidated += onCurrentStateInvalidated3
																			storyboard.Children.Add(ca)

																			Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

																			sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

																		else:
																			termHashSet.Add(sender1.Child.Tag)
																		
																			sender1.Tag = storyboard = Storyboard()
																			color = Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102)
																			ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
																			sineEase = SineEase()

																			sineEase.EasingMode = EasingMode.EaseOut
																			ca.EasingFunction = sineEase

																			def onCurrentStateInvalidated3(sender2, args2):
																				if sender2.CurrentState == ClockState.Filling:
																					sender1.Background = SolidColorBrush(color)
																					sender1.Tag = None
																					storyboard.Remove(sender1)

																			storyboard.CurrentStateInvalidated += onCurrentStateInvalidated3
																			storyboard.Children.Add(ca)

																			Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

																			sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

																			if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control and termHashSet.Count > 0 or termHashSet.Count == 3):
																				storyboard = Storyboard()
																				da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
																				da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
																				da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
																				sineEase = SineEase()

																				sineEase.EasingMode = EasingMode.EaseIn
																				da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

																				def onCurrentStateInvalidated4(sender, args):
																					if sender.CurrentState == ClockState.Filling:
																						contentControl.Opacity = 0
																						contentControl.RenderTransform.ScaleX = 1.5
																						contentControl.RenderTransform.ScaleY = 1.5
																						storyboard.Remove(contentControl)
																						imageHashSet.Clear()
																						window.Close()

																				storyboard.CurrentStateInvalidated += onCurrentStateInvalidated4
																				storyboard.Children.Add(da1)
																				storyboard.Children.Add(da2)
																				storyboard.Children.Add(da3)

																				Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
																				Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																				Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
																				contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
																				closeTimer.Tag = False

																				for key in termHashSet:
																					if not chargesDictionary.ContainsKey(key):
																						break

																				else:
																					def comparison(s1, s2):
																						sum1 = 0.0
																						sum2 = 0.0

																						for score in chargesDictionary[s1]:
																							sum1 += score

																						for score in chargesDictionary[s2]:
																							sum2 += score
																			
																						if sum1 / chargesDictionary[s1].Count > sum2 / chargesDictionary[s2].Count:
																							return 1

																						elif sum1 / chargesDictionary[s1].Count < sum2 / chargesDictionary[s2].Count:
																							return -1
																			
																						return 0

																					list = List[String](termHashSet)
																					list.Sort(comparison)
																					list.Reverse()

																					for key in termHashSet:
																						chargesDictionary.Remove(key)

																						if imageDictionary.ContainsKey(key):
																							imageDictionary.Remove(key)

																					d = Dictionary[String, List[Double]](chargesDictionary)
																					chargesDictionary.Clear()
																					charges = 0

																					for kvp in d:
																						chargesDictionary.Add(kvp.Key, kvp.Value)

																						sum = 0.0

																						for score in kvp.Value:
																							sum += score

																						if sum >= max:
																							charges += 1

																					if remainingCount + termHashSet.Count < 5:
																						remainingCount += termHashSet.Count
																					else:
																						remainingCount = 5
																	
																					sequenceList = List[Sequence]()
								
																					for sequence in Script.Instance.Sequences:
																						if sequence.Name.Equals("Charge"):
																							sequenceList.Add(sequence)

																					Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, charges.ToString(CultureInfo.InvariantCulture)))

																					maxWidth = Double.MinValue
																					maxHeight = Double.MinValue
																					bitmapImageList = List[BitmapImage]()
																					r = Random(Environment.TickCount)
																					index = 0

																					for fileName in ["Star-Dark1.png", "Star-Dark2.png", "Star-Dark3.png", "Star-Dark4.png", "Star-Dark5.png", "Star-Light1.png", "Star-Light2.png", "Star-Light3.png", "Star-Light4.png", "Star-Light5.png"]:
																						fs = None
																					
																						try:
																							fs = FileStream(String.Concat("Assets\\", fileName), FileMode.Open, FileAccess.Read, FileShare.Read)
																						
																							bi = BitmapImage()
																							bi.BeginInit()
																							bi.StreamSource = fs
																							bi.CacheOption = BitmapCacheOption.OnLoad
																							bi.CreateOptions = BitmapCreateOptions.None
																							bi.EndInit()

																							if bi.Width > maxWidth:
																								maxWidth = bi.Width

																							if bi.Height > maxHeight:
																								maxHeight = bi.Height

																							bitmapImageList.Add(bi)

																						except:
																							continue

																						finally:
																							if fs is not None:
																								fs.Close()

																					count = Math.Round(SystemParameters.PrimaryScreenWidth * SystemParameters.PrimaryScreenHeight / maxWidth / maxHeight / bitmapImageList.Count * termHashSet.Count)
																					countdownEvent = CountdownEvent(Convert.ToInt32(count))

																					while index < count:
																						def onLoaded2(sender1, args1):
																							storyboard = Storyboard()
																							da1 = DoubleAnimation(sender1.Content.Opacity, 1, TimeSpan.FromMilliseconds(500))
																							da2 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
																							da3 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
																							da4 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																							da5 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																							da6 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																							sineEase1 = SineEase()
																							sineEase2 = SineEase()

																							storyboard.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(1000)))
																							sineEase1.EasingMode = EasingMode.EaseOut
																							sineEase2.EasingMode = EasingMode.EaseIn
																							da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase1
																							da4.BeginTime = da5.BeginTime = da6.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(500))
																							da4.EasingFunction = da5.EasingFunction = da6.EasingFunction = sineEase2

																							def onCurrentStateInvalidated5(sender2, args2):
																								if sender2.CurrentState == ClockState.Filling:
																									sender1.Close()

																							storyboard.CurrentStateInvalidated += onCurrentStateInvalidated5
																							storyboard.Children.Add(da1)
																							storyboard.Children.Add(da2)
																							storyboard.Children.Add(da3)
																							storyboard.Children.Add(da4)
																							storyboard.Children.Add(da5)
																							storyboard.Children.Add(da6)

																							Storyboard.SetTarget(da1, sender1.Content)
																							Storyboard.SetTarget(da2, sender1.Content)
																							Storyboard.SetTarget(da3, sender1.Content)
																							Storyboard.SetTarget(da4, sender1.Content)
																							Storyboard.SetTarget(da5, sender1.Content)
																							Storyboard.SetTarget(da6, sender1.Content)
																							Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
																							Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																							Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
																							Storyboard.SetTargetProperty(da4, PropertyPath(ContentControl.OpacityProperty))
																							Storyboard.SetTargetProperty(da5, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																							Storyboard.SetTargetProperty(da6, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

																							storyboard.Begin()

																						def onClosed(sender1, args1):
																							countdownEvent.Signal()
					
																						bi = bitmapImageList[r.Next(bitmapImageList.Count)]
																						width = Convert.ToInt32(bi.Width) / 2
																						height = Convert.ToInt32(bi.Height) / 2
							
																						w = Window()
																						w.Owner = Application.Current.MainWindow
																						w.Title = Application.Current.MainWindow.Title
																						w.Left = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenWidth) - width)
																						w.Top = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenHeight) - height)
																						w.AllowsTransparency = True
																						w.WindowStyle = WindowStyle.None
																						w.ResizeMode = ResizeMode.NoResize
																						w.ShowActivated = False
																						w.ShowInTaskbar = False
																						w.Topmost = True
																						w.SizeToContent = SizeToContent.WidthAndHeight
																						w.Background = Brushes.Transparent
																						w.Loaded += onLoaded2
																						w.Closed += onClosed

																						cc = ContentControl()
																						cc.UseLayoutRounding = True
																						cc.HorizontalAlignment = HorizontalAlignment.Stretch
																						cc.VerticalAlignment = VerticalAlignment.Stretch
																						cc.Opacity = 0
																						cc.RenderTransform = ScaleTransform(0, 0, width / 2, height / 2)
															
																						w.Content = cc
															
																						image = Image()
																						image.CacheMode = BitmapCache(1)
																						image.HorizontalAlignment = HorizontalAlignment.Left
																						image.VerticalAlignment = VerticalAlignment.Top
																						image.Source = bi
																						image.Width = height
																						image.Height = height
																						image.Stretch = Stretch.Uniform
															
																						cc.Content = image
															
																						w.Show()

																						index += 1

																					if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked:
																						def onPlay():
																							soundPlayer = None

																							try:
																								soundPlayer = SoundPlayer("Assets\\Transform.wav")
																								soundPlayer.Load()
																								soundPlayer.PlaySync()

																							finally:
																								if soundPlayer is not None:
																									soundPlayer.Dispose()
									
																						Task.Factory.StartNew(onPlay)

																					def onRun(state):
																						countdownEvent.Wait()
																						countdownEvent.Dispose()

																						return enumerateCharacters(state)

																					def onCompleted(task):
																						if task.Result.Count > 1:
																							characterList = List[Character]()

																							for character in task.Result:
																								isNew = True

																								for c in Script.Instance.Characters:
																									if character.Name.Equals(c.Name):
																										isNew = False

																										break

																								if isNew:
																									success, likes = likesDictionary.TryGetValue(character.Name)

																									if success:
																										for i in range(likes.Count):
																											characterList.Add(character)

																									characterList.Add(character)

																							if characterList.Count > 0:
																								visit(characterList[Random(Environment.TickCount).Next(characterList.Count)], list)

																							else:
																								sequenceList = List[Sequence]()

																								for sequence in Script.Instance.Sequences:
																									if sequence.Name.Equals("Activate"):
																										sequenceList.Add(sequence)

																								Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

																						else:
																							sequenceList = List[Sequence]()

																							for sequence in Script.Instance.Sequences:
																								if sequence.Name.Equals("Activate"):
																									sequenceList.Add(sequence)

																							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

																					Task.Factory.StartNew[List[Character]](onRun, _Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), TaskCreationOptions.LongRunning).ContinueWith(Action[Task[List[Character]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

																element2.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
																element2.MouseEnter += onMouseEnter
																element2.MouseLeave += onMouseLeave
																element2.MouseLeftButtonUp += onMouseLeftButtonUp
																isCharged = True

								if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked and isCharged:
									def onPlay():
										soundPlayer = None

										try:
											soundPlayer = SoundPlayer("Assets\\Charge.wav")
											soundPlayer.Load()
											soundPlayer.PlaySync()

										finally:
											if soundPlayer is not None:
												soundPlayer.Dispose()
									
									Task.Factory.StartNew(onPlay)

						storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated2

						for element1 in stackPanel1.Children:
							for element2 in element1.Children:
								for element3 in element2.Child.Children:
									if clr.GetClrType(Grid).IsInstanceOfType(element3):
										for element4 in element3.Children:
											if clr.GetClrType(Grid).IsInstanceOfType(element4):
												sum = 0.0

												for score in element4.Tag:
													sum += score

												da = DoubleAnimation(element4.Opacity, 1 if sum > max else 0.75 + 0.25 * sum / max, TimeSpan.FromMilliseconds(((50 if sum > max else 50 * sum / max) - element4.Clip.Rect.Height) * 50))
												ra = RectAnimation(element4.Clip.Rect, Rect(0, 0 if sum > max else 50 - 50 * sum / max, 50, 50 if sum > max else 50 * sum / max), TimeSpan.FromMilliseconds(((50 if sum > max else 50 * sum / max) - element4.Clip.Rect.Height) * 50))
												sineEase = SineEase()

												sineEase.EasingMode = EasingMode.EaseOut
												da.EasingFunction = ra.EasingFunction = sineEase

												storyboard2.Children.Add(da)
												storyboard2.Children.Add(ra)
						
												Storyboard.SetTarget(da, element4)
												Storyboard.SetTarget(ra, element4)
												Storyboard.SetTargetProperty(da, PropertyPath(Grid.OpacityProperty))
												Storyboard.SetTargetProperty(ra, PropertyPath("(0).(1)", Grid.ClipProperty, RectangleGeometry.RectProperty))
				
												for element5 in element4.Children:
													if clr.GetClrType(Image).IsInstanceOfType(element5):
														if element5.Tag is None:
															element5.Source = createColorBarsImage(Size(70, 70))

															for element6 in element4.Children:
																if clr.GetClrType(Ellipse).IsInstanceOfType(element6):
																	if element6.Clip is not None:
																		storyboard3 = Storyboard()
																		ra = RectAnimation(element6.Clip.Rect, Rect(0, 0, 50, 0), TimeSpan.FromMilliseconds(500))
																		sineEase = SineEase()

																		sineEase.EasingMode = EasingMode.EaseOut
																		ra.EasingFunction = sineEase

																		storyboard3.Children.Add(ra)

																		Storyboard.SetTarget(ra, element6)
																		Storyboard.SetTargetProperty(ra, PropertyPath("(0).(1)", Ellipse.ClipProperty, RectangleGeometry.RectProperty))
																
																		storyboard3.Begin()

														elif not imageHashSet.Contains(element5.Tag):
															imageHashSet.Add(element5.Tag)
																
															def onUpdated(task):
																bi = None

																if task.Result.Value is not None:
																	try:
																		bi = BitmapImage()
																		bi.BeginInit()
																		bi.StreamSource = task.Result.Value
																		bi.CacheOption = BitmapCacheOption.OnLoad
																		bi.CreateOptions = BitmapCreateOptions.None
																		bi.EndInit()

																	except:
																		bi = None

																	finally:
																		task.Result.Value.Close()

																if imageHashSet.Contains(task.Result.Key):
																	for e1 in stackPanel1.Children:
																		for e2 in e1.Children:
																			if imageDictionary.ContainsKey(e2.Child.Tag):
																				if imageDictionary[e2.Child.Tag].Equals(task.Result.Key):
																					for e3 in e2.Child.Children:
																						if clr.GetClrType(Grid).IsInstanceOfType(e3):
																							for e4 in e3.Children:
																								if clr.GetClrType(Grid).IsInstanceOfType(e4):
																									for e5 in e4.Children:
																										if clr.GetClrType(Image).IsInstanceOfType(e5):
																											if e5.Tag is not None:
																												if e5.Tag.Equals(task.Result.Key):
																													if bi is None:
																														e5.Source = createColorBarsImage(Size(70, 70))
																												
																													else:
																														e5.Source = cropImage(bi)

																													for e6 in e4.Children:
																														if clr.GetClrType(Ellipse).IsInstanceOfType(e6):
																															if e6.Clip is not None:
																																storyboard = Storyboard()
																																ra = RectAnimation(e6.Clip.Rect, Rect(0, 0, 50, 0), TimeSpan.FromMilliseconds(500))
																																sineEase = SineEase()

																																sineEase.EasingMode = EasingMode.EaseOut
																																ra.EasingFunction = sineEase

																																storyboard.Children.Add(ra)

																																Storyboard.SetTarget(ra, e6)
																																Storyboard.SetTargetProperty(ra, PropertyPath("(0).(1)", Ellipse.ClipProperty, RectangleGeometry.RectProperty))
																
																																storyboard.Begin()

															task = createUpdateImageTask(imageDictionary[element2.Child.Tag])
															task.ContinueWith(Action[Task[KeyValuePair[Uri, MemoryStream]]](onUpdated), TaskScheduler.FromCurrentSynchronizationContext())
															task.Start()

						storyboard2.Begin()

				storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated1
				storyboard1.Children.Add(da1)
				storyboard1.Children.Add(da2)
				storyboard1.Children.Add(da3)

				Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
				Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
				Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

				contentControl.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

			def onWindowMouseEnter(sender, args):
				closeTimer.Stop()
				border1.Tag = True

			def onWindowMouseLeave(sender, args):
				if closeTimer.Tag:
					closeTimer.Start()

				border1.Tag = False

			def onClose(sender, args):
				from System.IO import Path as _Path
				global remainingCount
	
				closeTimer.Stop()
						
				storyboard = Storyboard()
				da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
				da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
				da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
				sineEase = SineEase()

				sineEase.EasingMode = EasingMode.EaseIn
				da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

				def onCurrentStateInvalidated1(sender, args):
					if sender.CurrentState == ClockState.Filling:
						contentControl.Opacity = 0
						contentControl.RenderTransform.ScaleX = 1.5
						contentControl.RenderTransform.ScaleY = 1.5
						storyboard.Remove(contentControl)
						imageHashSet.Clear()
						window.Close()

				storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
				storyboard.Children.Add(da1)
				storyboard.Children.Add(da2)
				storyboard.Children.Add(da3)

				Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
				Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
				Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
				contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
				closeTimer.Tag = False

				if isLocked:
					for key in termHashSet:
						if not chargesDictionary.ContainsKey(key):
							break

					else:
						def comparison(s1, s2):
							sum1 = 0.0
							sum2 = 0.0

							for score in chargesDictionary[s1]:
								sum1 += score

							for score in chargesDictionary[s2]:
								sum2 += score

							if sum1 / chargesDictionary[s1].Count > sum2 / chargesDictionary[s2].Count:
								return 1

							elif sum1 / chargesDictionary[s1].Count < sum2 / chargesDictionary[s2].Count:
								return -1
																			
							return 0

						list = List[String](termHashSet)
						list.Sort(comparison)
						list.Reverse()

						for key in termHashSet:
							chargesDictionary.Remove(key)

							if imageDictionary.ContainsKey(key):
								imageDictionary.Remove(key)

						d = Dictionary[String, List[Double]](chargesDictionary)
						chargesDictionary.Clear()
						charges = 0

						for kvp in d:
							chargesDictionary.Add(kvp.Key, kvp.Value)

							sum = 0.0

							for score in kvp.Value:
								sum += score

							if sum >= max:
								charges += 1

						if remainingCount + termHashSet.Count < 5:
							remainingCount += termHashSet.Count
						else:
							remainingCount = 5
																	
						sequenceList = List[Sequence]()
								
						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Charge"):
								sequenceList.Add(sequence)

						Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, charges.ToString(CultureInfo.InvariantCulture)))

						maxWidth = Double.MinValue
						maxHeight = Double.MinValue
						bitmapImageList = List[BitmapImage]()
						r = Random(Environment.TickCount)
						index = 0

						for fileName in ["Star-Dark1.png", "Star-Dark2.png", "Star-Dark3.png", "Star-Dark4.png", "Star-Dark5.png", "Star-Light1.png", "Star-Light2.png", "Star-Light3.png", "Star-Light4.png", "Star-Light5.png"]:
							fs = None
																					
							try:
								fs = FileStream(String.Concat("Assets\\", fileName), FileMode.Open, FileAccess.Read, FileShare.Read)
																						
								bi = BitmapImage()
								bi.BeginInit()
								bi.StreamSource = fs
								bi.CacheOption = BitmapCacheOption.OnLoad
								bi.CreateOptions = BitmapCreateOptions.None
								bi.EndInit()

								if bi.Width > maxWidth:
									maxWidth = bi.Width

								if bi.Height > maxHeight:
									maxHeight = bi.Height

								bitmapImageList.Add(bi)

							except:
								continue

							finally:
								if fs is not None:
									fs.Close()

						count = Math.Round(SystemParameters.PrimaryScreenWidth * SystemParameters.PrimaryScreenHeight / maxWidth / maxHeight / bitmapImageList.Count * termHashSet.Count)
						countdownEvent = CountdownEvent(Convert.ToInt32(count))

						while index < count:
							def onLoaded2(sender1, args1):
								storyboard = Storyboard()
								da1 = DoubleAnimation(sender1.Content.Opacity, 1, TimeSpan.FromMilliseconds(500))
								da2 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
								da3 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
								da4 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
								da5 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
								da6 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
								sineEase1 = SineEase()
								sineEase2 = SineEase()

								storyboard.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(1000)))
								sineEase1.EasingMode = EasingMode.EaseOut
								sineEase2.EasingMode = EasingMode.EaseIn
								da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase1
								da4.BeginTime = da5.BeginTime = da6.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(500))
								da4.EasingFunction = da5.EasingFunction = da6.EasingFunction = sineEase2

								def onCurrentStateInvalidated2(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Close()

								storyboard.CurrentStateInvalidated += onCurrentStateInvalidated2
								storyboard.Children.Add(da1)
								storyboard.Children.Add(da2)
								storyboard.Children.Add(da3)
								storyboard.Children.Add(da4)
								storyboard.Children.Add(da5)
								storyboard.Children.Add(da6)

								Storyboard.SetTarget(da1, sender1.Content)
								Storyboard.SetTarget(da2, sender1.Content)
								Storyboard.SetTarget(da3, sender1.Content)
								Storyboard.SetTarget(da4, sender1.Content)
								Storyboard.SetTarget(da5, sender1.Content)
								Storyboard.SetTarget(da6, sender1.Content)
								Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
								Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
								Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
								Storyboard.SetTargetProperty(da4, PropertyPath(ContentControl.OpacityProperty))
								Storyboard.SetTargetProperty(da5, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
								Storyboard.SetTargetProperty(da6, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

								storyboard.Begin()
					
							def onClosed(sender1, args1):
								countdownEvent.Signal()
							
							bi = bitmapImageList[r.Next(bitmapImageList.Count)]
							width = Convert.ToInt32(bi.Width) / 2
							height = Convert.ToInt32(bi.Height) / 2
							
							w = Window()
							w.Owner = Application.Current.MainWindow
							w.Title = Application.Current.MainWindow.Title
							w.Left = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenWidth) - width)
							w.Top = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenHeight) - height)
							w.AllowsTransparency = True
							w.WindowStyle = WindowStyle.None
							w.ResizeMode = ResizeMode.NoResize
							w.ShowActivated = False
							w.ShowInTaskbar = False
							w.Topmost = True
							w.SizeToContent = SizeToContent.WidthAndHeight
							w.Background = Brushes.Transparent
							w.Loaded += onLoaded2
							w.Closed += onClosed
													
							cc = ContentControl()
							cc.UseLayoutRounding = True
							cc.HorizontalAlignment = HorizontalAlignment.Stretch
							cc.VerticalAlignment = VerticalAlignment.Stretch
							cc.Opacity = 0
							cc.RenderTransform = ScaleTransform(0, 0, width / 2, height / 2)
															
							w.Content = cc
															
							image = Image()
							image.CacheMode = BitmapCache(1)
							image.HorizontalAlignment = HorizontalAlignment.Left
							image.VerticalAlignment = VerticalAlignment.Top
							image.Source = bi
							image.Width = height
							image.Height = height
							image.Stretch = Stretch.Uniform
															
							cc.Content = image
															
							w.Show()

							index += 1

						if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked:
							def onPlay():
								soundPlayer = None

								try:
									soundPlayer = SoundPlayer("Assets\\Transform.wav")
									soundPlayer.Load()
									soundPlayer.PlaySync()

								finally:
									if soundPlayer is not None:
										soundPlayer.Dispose()
									
							Task.Factory.StartNew(onPlay)

						def onRun(state):
							countdownEvent.Wait()
							countdownEvent.Dispose()

							return enumerateCharacters(state)

						def onCompleted(task):
							if task.Result.Count > 1:
								characterList = List[Character]()

								for character in task.Result:
									isNew = True

									for c in Script.Instance.Characters:
										if character.Name.Equals(c.Name):
											isNew = False

											break

									if isNew:
										success, likes = likesDictionary.TryGetValue(character.Name)

										if success:
											for i in range(likes.Count):
												characterList.Add(character)

										characterList.Add(character)

								if characterList.Count > 0:
									visit(characterList[Random(Environment.TickCount).Next(characterList.Count)], list)

								else:
									sequenceList = List[Sequence]()

									for sequence in Script.Instance.Sequences:
										if sequence.Name.Equals("Activate"):
											sequenceList.Add(sequence)

									Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

							else:
								sequenceList = List[Sequence]()

								for sequence in Script.Instance.Sequences:
									if sequence.Name.Equals("Activate"):
										sequenceList.Add(sequence)

								Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

						Task.Factory.StartNew[List[Character]](onRun, _Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), TaskCreationOptions.LongRunning).ContinueWith(Action[Task[List[Character]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())
			
			closeTimer.Tick += onClose
			closeTimer.Interval = TimeSpan.FromSeconds(3)
			closeTimer.Tag = True
			
			fs = None
			bi = BitmapImage()

			try:
				fs = FileStream("Assets\\Background-Popup.png", FileMode.Open, FileAccess.Read, FileShare.Read)

				bi.BeginInit()
				bi.StreamSource = fs
				bi.CacheOption = BitmapCacheOption.OnLoad
				bi.CreateOptions = BitmapCreateOptions.None
				bi.EndInit()

			finally:
				if fs is not None:
					fs.Close()

			imageBrush = ImageBrush(bi)
			imageBrush.TileMode = TileMode.None
			imageBrush.Stretch = Stretch.Fill
			imageBrush.ViewboxUnits = BrushMappingMode.Absolute
			imageBrush.Viewbox = Rect(0, 0, bi.Width, bi.Height)
			imageBrush.AlignmentX = AlignmentX.Left
			imageBrush.AlignmentY = AlignmentY.Top
			imageBrush.Opacity = 0.5

			dg = DrawingGroup()
			dc = dg.Open()
			dc.DrawRectangle(SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, 0, 0, 0)), None, Rect(0, 0, bi.Width, bi.Height))
			dc.DrawRectangle(imageBrush, None, Rect(0, 0, bi.Width, bi.Height))
			dc.Close()
			
			window.Owner = Application.Current.MainWindow
			window.Title = Application.Current.MainWindow.Title
			window.WindowStartupLocation = WindowStartupLocation.CenterOwner
			window.AllowsTransparency = True
			window.WindowStyle = WindowStyle.None
			window.ResizeMode = ResizeMode.NoResize
			window.ShowActivated = False
			window.ShowInTaskbar = Application.Current.MainWindow.ContextMenu.Items[5].IsChecked
			window.Topmost = True
			window.SizeToContent = SizeToContent.WidthAndHeight
			window.Background = Brushes.Transparent
			window.Loaded += onLoaded1
			window.MouseEnter += onWindowMouseEnter
			window.MouseLeave += onWindowMouseLeave

			contentControl.UseLayoutRounding = True
			contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
			contentControl.VerticalAlignment = VerticalAlignment.Stretch
			contentControl.Opacity = 0
			contentControl.RenderTransform = ScaleTransform(1, 1)

			window.Content = contentControl
			
			backgroundBrush = ImageBrush(DrawingImage(dg))
			backgroundBrush.TileMode = TileMode.Tile
			backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
			backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
			backgroundBrush.Stretch = Stretch.None

			if backgroundBrush.CanFreeze:
				backgroundBrush.Freeze()

			border1.HorizontalAlignment = HorizontalAlignment.Center
			border1.VerticalAlignment = VerticalAlignment.Center
			border1.Padding = Thickness(2)
			border1.CornerRadius = CornerRadius(4)
			border1.Background = backgroundBrush
			border1.Tag = False

			contentControl.Content = border1

			stackPanel1.HorizontalAlignment = HorizontalAlignment.Center
			stackPanel1.VerticalAlignment = VerticalAlignment.Center
			stackPanel1.Orientation = Orientation.Vertical
			stackPanel1.Background = Brushes.Transparent

			border1.Child = stackPanel1
			
			stackPanel2 = StackPanel()
			stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
			stackPanel2.VerticalAlignment = VerticalAlignment.Top
			stackPanel2.Orientation = Orientation.Horizontal
			stackPanel2.Background = Brushes.Transparent

			stackPanel1.Children.Add(stackPanel2)
			
			fontSizeConverter = FontSizeConverter()

			for i in range(termList.Count):
				if i % 3 == 0:
					stackPanel2 = StackPanel()
					stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
					stackPanel2.VerticalAlignment = VerticalAlignment.Top
					stackPanel2.Orientation = Orientation.Horizontal
					stackPanel2.Background = Brushes.Transparent

					stackPanel1.Children.Add(stackPanel2)

				border2 = Border()
				border2.HorizontalAlignment = HorizontalAlignment.Stretch
				border2.VerticalAlignment = VerticalAlignment.Stretch
				border2.Margin = Thickness(2)
				border2.Padding = Thickness(4)
				border2.CornerRadius = CornerRadius(3)

				grid1 = Grid()
				grid1.HorizontalAlignment = HorizontalAlignment.Center
				grid1.VerticalAlignment = VerticalAlignment.Top
				grid1.Margin = Thickness(0, 10, 0, 10)
				grid1.Background = Brushes.Transparent
						
				dropShadowEffect1 = DropShadowEffect()
				dropShadowEffect1.BlurRadius = 1
				dropShadowEffect1.Color = Colors.Black
				dropShadowEffect1.Direction = 270
				dropShadowEffect1.Opacity = 0.5
				dropShadowEffect1.ShadowDepth = 1

				if dropShadowEffect1.CanFreeze:
					dropShadowEffect1.Freeze()

				grid1.Effect = dropShadowEffect1

				grid2 = Grid()
				grid2.HorizontalAlignment = HorizontalAlignment.Center
				grid2.VerticalAlignment = VerticalAlignment.Center
				grid2.Background = Brushes.Transparent
				grid2.Tag = chargesDictionary[termList[i]]

				if dictionary.ContainsKey(termList[i]):
					grid2.Opacity = 0.75 + 0.25 * dictionary[termList[i]] / max
					grid2.Clip = RectangleGeometry(Rect(0, 50 - 50 * dictionary[termList[i]] / max, 50, 50 * dictionary[termList[i]] / max))
							
					border2.Background = Brushes.Transparent

				else:
					grid1.RenderTransform = ScaleTransform(1, 1)
					grid2.Clip = RectangleGeometry(Rect(0, 0, 50, 50))

					if not isLocked:
						def onMouseEnter(sender1, args1):
							if not termHashSet.Contains(sender1.Child.Tag):
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								sender1.Tag = storyboard1 = Storyboard()
								ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseOut
								ca.EasingFunction = sineEase

								def onCurrentStateInvalidated1(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Background = SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102))
										sender1.Tag = None
										storyboard1.Remove(sender1)

								storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated1
								storyboard1.Children.Add(ca)

								Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

								sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

								for element1 in sender1.Child.Children:
									if clr.GetClrType(Grid).IsInstanceOfType(element1):
										if element1.Tag is not None:
											element1.Tag.Stop(element1)

										element1.Tag = storyboard2 = Storyboard()
										da1 = DoubleAnimation(element1.RenderTransform.ScaleX, 1.1, TimeSpan.FromMilliseconds(500))
										da2 = DoubleAnimation(element1.RenderTransform.ScaleY, 1.1, TimeSpan.FromMilliseconds(500))
										sineEase = SineEase()

										sineEase.EasingMode = EasingMode.EaseOut
										da1.EasingFunction = da2.EasingFunction = sineEase

										def onCurrentStateInvalidated2(sender2, args2):
											if sender2.CurrentState == ClockState.Filling:
												for element2 in sender1.Child.Children:
													if element2.Tag == storyboard2:
														element2.RenderTransform.ScaleX = 1.1
														element2.RenderTransform.ScaleY = 1.1
														element2.Tag = None
														storyboard2.Remove(element2)

										storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated2
										storyboard2.Children.Add(da1)
										storyboard2.Children.Add(da2)

										Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
										Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
										element1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)
						
						def onMouseLeave(sender1, args1):
							if not termHashSet.Contains(sender1.Child.Tag):
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								sender1.Tag = storyboard1 = Storyboard()
								ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(0, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseIn
								ca.EasingFunction = sineEase

								def onCurrentStateInvalidated1(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
										sender1.Tag = None
										storyboard1.Remove(sender1)

								storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated1
								storyboard1.Children.Add(ca)

								Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

								sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

								for element1 in sender1.Child.Children:
									if clr.GetClrType(Grid).IsInstanceOfType(element1):
										if element1.Tag is not None:
											element1.Tag.Stop(element1)

										element1.Tag = storyboard2 = Storyboard()
										da1 = DoubleAnimation(element1.RenderTransform.ScaleX, 1, TimeSpan.FromMilliseconds(500))
										da2 = DoubleAnimation(element1.RenderTransform.ScaleY, 1, TimeSpan.FromMilliseconds(500))
										sineEase = SineEase()

										sineEase.EasingMode = EasingMode.EaseIn
										da1.EasingFunction = da2.EasingFunction = sineEase

										def onCurrentStateInvalidated2(sender2, args2):
											if sender2.CurrentState == ClockState.Filling:
												for element2 in sender1.Child.Children:
													if element2.Tag == storyboard2:
														element2.RenderTransform.ScaleX = 1
														element2.RenderTransform.ScaleY = 1
														element2.Tag = None
														storyboard2.Remove(element2)

										storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated2
										storyboard2.Children.Add(da1)
										storyboard2.Children.Add(da2)

										Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
										Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
										element1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)

						def onMouseLeftButtonUp(sender1, args1):
							from System.IO import Path as _Path
							global remainingCount

							if termHashSet.Count < 3:
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								if termHashSet.Contains(sender1.Child.Tag):
									termHashSet.Remove(sender1.Child.Tag)

									sender1.Tag = storyboard = Storyboard()
									color = Color.FromArgb(0, Byte.MaxValue, 0, 102)
									ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
									sineEase = SineEase()

									sineEase.EasingMode = EasingMode.EaseIn
									ca.EasingFunction = sineEase

									def onCurrentStateInvalidated1(sender2, args2):
										if sender2.CurrentState == ClockState.Filling:
											sender1.Background = SolidColorBrush(color)
											sender1.Tag = None
											storyboard.Remove(sender1)

									storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
									storyboard.Children.Add(ca)

									Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

									sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

								else:
									termHashSet.Add(sender1.Child.Tag)

									sender1.Tag = storyboard = Storyboard()
									color = Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102)
									ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
									sineEase = SineEase()

									sineEase.EasingMode = EasingMode.EaseOut
									ca.EasingFunction = sineEase

									def onCurrentStateInvalidated1(sender2, args2):
										if sender2.CurrentState == ClockState.Filling:
											sender1.Background = SolidColorBrush(color)
											sender1.Tag = None
											storyboard.Remove(sender1)

									storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
									storyboard.Children.Add(ca)

									Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

									sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
								
									if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control and termHashSet.Count > 0 or termHashSet.Count == 3):
										storyboard = Storyboard()
										da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
										da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
										da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
										sineEase = SineEase()

										sineEase.EasingMode = EasingMode.EaseIn
										da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

										def onCurrentStateInvalidated2(sender, args):
											if sender.CurrentState == ClockState.Filling:
												contentControl.Opacity = 0
												contentControl.RenderTransform.ScaleX = 1.5
												contentControl.RenderTransform.ScaleY = 1.5
												storyboard.Remove(contentControl)
												imageHashSet.Clear()
												window.Close()

										storyboard.CurrentStateInvalidated += onCurrentStateInvalidated2
										storyboard.Children.Add(da1)
										storyboard.Children.Add(da2)
										storyboard.Children.Add(da3)

										Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
										Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
										Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
										contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
										closeTimer.Tag = False

										for key in termHashSet:
											if not chargesDictionary.ContainsKey(key):
												break

										else:
											def comparison(s1, s2):
												sum1 = 0.0
												sum2 = 0.0

												for score in chargesDictionary[s1]:
													sum1 += score

												for score in chargesDictionary[s2]:
													sum2 += score
																			
												if sum1 / chargesDictionary[s1].Count > sum2 / chargesDictionary[s2].Count:
													return 1

												elif sum1 / chargesDictionary[s1].Count < sum2 / chargesDictionary[s2].Count:
													return -1
																			
												return 0

											list = List[String](termHashSet)
											list.Sort(comparison)
											list.Reverse()

											for key in termHashSet:
												chargesDictionary.Remove(key)

												if imageDictionary.ContainsKey(key):
													imageDictionary.Remove(key)

											d = Dictionary[String, List[Double]](chargesDictionary)
											chargesDictionary.Clear()
											charges = 0

											for kvp in d:
												chargesDictionary.Add(kvp.Key, kvp.Value)

												sum = 0.0

												for score in kvp.Value:
													sum += score

												if sum >= max:
													charges += 1

											if remainingCount + termHashSet.Count < 5:
												remainingCount += termHashSet.Count
											else:
												remainingCount = 5
																	
											sequenceList = List[Sequence]()
								
											for sequence in Script.Instance.Sequences:
												if sequence.Name.Equals("Charge"):
													sequenceList.Add(sequence)

											Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, charges.ToString(CultureInfo.InvariantCulture)))
										
											maxWidth = Double.MinValue
											maxHeight = Double.MinValue
											bitmapImageList = List[BitmapImage]()
											r = Random(Environment.TickCount)
											index = 0

											for fileName in ["Star-Dark1.png", "Star-Dark2.png", "Star-Dark3.png", "Star-Dark4.png", "Star-Dark5.png", "Star-Light1.png", "Star-Light2.png", "Star-Light3.png", "Star-Light4.png", "Star-Light5.png"]:
												fs = None
																					
												try:
													fs = FileStream(String.Concat("Assets\\", fileName), FileMode.Open, FileAccess.Read, FileShare.Read)
																						
													bi = BitmapImage()
													bi.BeginInit()
													bi.StreamSource = fs
													bi.CacheOption = BitmapCacheOption.OnLoad
													bi.CreateOptions = BitmapCreateOptions.None
													bi.EndInit()

													if bi.Width > maxWidth:
														maxWidth = bi.Width

													if bi.Height > maxHeight:
														maxHeight = bi.Height

													bitmapImageList.Add(bi)

												except:
													continue

												finally:
													if fs is not None:
														fs.Close()

											count = Math.Round(SystemParameters.PrimaryScreenWidth * SystemParameters.PrimaryScreenHeight / maxWidth / maxHeight / bitmapImageList.Count * termHashSet.Count)
											countdownEvent = CountdownEvent(Convert.ToInt32(count))

											while index < count:
												def onLoaded2(sender1, args1):
													storyboard = Storyboard()
													da1 = DoubleAnimation(sender1.Content.Opacity, 1, TimeSpan.FromMilliseconds(500))
													da2 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
													da3 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
													da4 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
													da5 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
													da6 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
													sineEase1 = SineEase()
													sineEase2 = SineEase()

													storyboard.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(1000)))
													sineEase1.EasingMode = EasingMode.EaseOut
													sineEase2.EasingMode = EasingMode.EaseIn
													da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase1
													da4.BeginTime = da5.BeginTime = da6.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(500))
													da4.EasingFunction = da5.EasingFunction = da6.EasingFunction = sineEase2

													def onCurrentStateInvalidated3(sender2, args2):
														if sender2.CurrentState == ClockState.Filling:
															sender1.Close()

													storyboard.CurrentStateInvalidated += onCurrentStateInvalidated3
													storyboard.Children.Add(da1)
													storyboard.Children.Add(da2)
													storyboard.Children.Add(da3)
													storyboard.Children.Add(da4)
													storyboard.Children.Add(da5)
													storyboard.Children.Add(da6)

													Storyboard.SetTarget(da1, sender1.Content)
													Storyboard.SetTarget(da2, sender1.Content)
													Storyboard.SetTarget(da3, sender1.Content)
													Storyboard.SetTarget(da4, sender1.Content)
													Storyboard.SetTarget(da5, sender1.Content)
													Storyboard.SetTarget(da6, sender1.Content)
													Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
													Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
													Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
													Storyboard.SetTargetProperty(da4, PropertyPath(ContentControl.OpacityProperty))
													Storyboard.SetTargetProperty(da5, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
													Storyboard.SetTargetProperty(da6, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

													storyboard.Begin()
					
												def onClosed(sender1, args1):
													countdownEvent.Signal()

												bi = bitmapImageList[r.Next(bitmapImageList.Count)]
												width = Convert.ToInt32(bi.Width) / 2
												height = Convert.ToInt32(bi.Height) / 2
							
												w = Window()
												w.Owner = Application.Current.MainWindow
												w.Title = Application.Current.MainWindow.Title
												w.Left = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenWidth) - width)
												w.Top = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenHeight) - height)
												w.AllowsTransparency = True
												w.WindowStyle = WindowStyle.None
												w.ResizeMode = ResizeMode.NoResize
												w.ShowActivated = False
												w.ShowInTaskbar = False
												w.Topmost = True
												w.SizeToContent = SizeToContent.WidthAndHeight
												w.Background = Brushes.Transparent
												w.Loaded += onLoaded2
												w.Closed += onClosed
													
												cc = ContentControl()
												cc.UseLayoutRounding = True
												cc.HorizontalAlignment = HorizontalAlignment.Stretch
												cc.VerticalAlignment = VerticalAlignment.Stretch
												cc.Opacity = 0
												cc.RenderTransform = ScaleTransform(0, 0, width / 2, height / 2)
															
												w.Content = cc
															
												image = Image()
												image.CacheMode = BitmapCache(1)
												image.HorizontalAlignment = HorizontalAlignment.Left
												image.VerticalAlignment = VerticalAlignment.Top
												image.Source = bi
												image.Width = height
												image.Height = height
												image.Stretch = Stretch.Uniform
															
												cc.Content = image
															
												w.Show()

												index += 1

											if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked:
												def onPlay():
													soundPlayer = None

													try:
														soundPlayer = SoundPlayer("Assets\\Transform.wav")
														soundPlayer.Load()
														soundPlayer.PlaySync()

													finally:
														if soundPlayer is not None:
															soundPlayer.Dispose()
									
												Task.Factory.StartNew(onPlay)

											def onRun(state):
												countdownEvent.Wait()
												countdownEvent.Dispose()

												return enumerateCharacters(state)

											def onCompleted(task):
												if task.Result.Count > 1:
													characterList = List[Character]()

													for character in task.Result:
														isNew = True

														for c in Script.Instance.Characters:
															if character.Name.Equals(c.Name):
																isNew = False

																break

														if isNew:
															success, likes = likesDictionary.TryGetValue(character.Name)

															if success:
																for i in range(likes.Count):
																	characterList.Add(character)

															characterList.Add(character)

													if characterList.Count > 0:
														visit(characterList[Random(Environment.TickCount).Next(characterList.Count)], list)

													else:
														sequenceList = List[Sequence]()

														for sequence in Script.Instance.Sequences:
															if sequence.Name.Equals("Activate"):
																sequenceList.Add(sequence)

														Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))
									
												else:
													sequenceList = List[Sequence]()

													for sequence in Script.Instance.Sequences:
														if sequence.Name.Equals("Activate"):
															sequenceList.Add(sequence)

													Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

											Task.Factory.StartNew[List[Character]](onRun, _Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), TaskCreationOptions.LongRunning).ContinueWith(Action[Task[List[Character]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

						border2.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
						border2.MouseEnter += onMouseEnter
						border2.MouseLeave += onMouseLeave
						border2.MouseLeftButtonUp += onMouseLeftButtonUp

				stackPanel2.Children.Add(border2)

				dockPanel = DockPanel()
				dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
				dockPanel.VerticalAlignment = VerticalAlignment.Stretch
				dockPanel.Tag = termList[i]

				border2.Child = dockPanel
				
				dockPanel.Children.Add(grid1)

				DockPanel.SetDock(grid1, Dock.Top)
					
				solidColorBrush1 = SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, 102, 102, 102))

				if solidColorBrush1.CanFreeze:
					solidColorBrush1.Freeze()

				ellipse1 = Ellipse()
				ellipse1.HorizontalAlignment = HorizontalAlignment.Center
				ellipse1.VerticalAlignment = VerticalAlignment.Center
				ellipse1.Fill = solidColorBrush1
				ellipse1.Width = 50
				ellipse1.Height = 50

				grid1.Children.Add(ellipse1)
				grid1.Children.Add(grid2)

				ellipseGeometry = EllipseGeometry(Rect(0, 0, 50, 50))

				if ellipseGeometry.CanFreeze:
					ellipseGeometry.Freeze()

				image = Image()
				image.HorizontalAlignment = HorizontalAlignment.Left
				image.VerticalAlignment = VerticalAlignment.Top
				image.Width = 50
				image.Height = 50
				image.Stretch = Stretch.Fill
				image.Clip = ellipseGeometry

				if imageDictionary.ContainsKey(termList[i]):
					image.Tag = imageDictionary[termList[i]]

				grid2.Children.Add(image)

				solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, Byte.MaxValue, 0, 102))

				if solidColorBrush2.CanFreeze:
					solidColorBrush2.Freeze()

				ellipse2 = Ellipse()
				ellipse2.HorizontalAlignment = HorizontalAlignment.Left
				ellipse2.VerticalAlignment = VerticalAlignment.Top
				ellipse2.Fill = solidColorBrush2
				ellipse2.Width = 50
				ellipse2.Height = 50
				ellipse2.Clip = RectangleGeometry(Rect(0, 0, 50, 50))
						
				grid2.Children.Add(ellipse2)
				
				ellipse3 = Ellipse()
				ellipse3.HorizontalAlignment = HorizontalAlignment.Center
				ellipse3.VerticalAlignment = VerticalAlignment.Center
				ellipse3.Stroke = Brushes.White
				ellipse3.StrokeThickness = 4
				ellipse3.Width = 50 + 8
				ellipse3.Height = 50 + 8

				grid1.Children.Add(ellipse3)

				starGeometry = createStarGeometry(Rect(0, 0, 20, 20))

				if starGeometry.CanFreeze:
					starGeometry.Freeze()

				path = Path()
				path.HorizontalAlignment = HorizontalAlignment.Center
				path.VerticalAlignment = VerticalAlignment.Center
				path.Fill = Brushes.White
				path.Data = starGeometry

				dropShadowEffect2 = DropShadowEffect()
				dropShadowEffect2.BlurRadius = 1
				dropShadowEffect2.Color = Colors.Black
				dropShadowEffect2.Direction = 270
				dropShadowEffect2.Opacity = 0.5
				dropShadowEffect2.ShadowDepth = 1

				if dropShadowEffect2.CanFreeze:
					dropShadowEffect2.Freeze()

				path.Effect = dropShadowEffect2
				
				grid1.Children.Add(path)

				border3 = Border()
				border3.HorizontalAlignment = HorizontalAlignment.Center
				border3.VerticalAlignment = VerticalAlignment.Top
				border3.Margin = Thickness(0)
				border3.Padding = Thickness(0)
				border3.CornerRadius = CornerRadius(0)
				border3.Width = 75
				border3.Background = Brushes.Transparent

				dropShadowEffect3 = DropShadowEffect()
				dropShadowEffect3.BlurRadius = 1
				dropShadowEffect3.Color = Colors.Black
				dropShadowEffect3.Direction = 270
				dropShadowEffect3.Opacity = 0.5
				dropShadowEffect3.ShadowDepth = 1

				if dropShadowEffect3.CanFreeze:
					dropShadowEffect3.Freeze()

				border3.Effect = dropShadowEffect3

				dockPanel.Children.Add(border3)
				
				DockPanel.SetDock(border3, Dock.Bottom)

				textBlock = TextBlock()
				textBlock.HorizontalAlignment = HorizontalAlignment.Center
				textBlock.VerticalAlignment = VerticalAlignment.Top
				textBlock.MaxWidth = 75
				textBlock.Text = termList[i]
				textBlock.Foreground = Brushes.White
				textBlock.FontSize = fontSizeConverter.ConvertFromString("9pt")
				textBlock.FontWeight = FontWeights.Bold
				textBlock.TextAlignment = TextAlignment.Center
				textBlock.TextWrapping = TextWrapping.Wrap

				RenderOptions.SetClearTypeHint(textBlock, ClearTypeHint.Enabled)

				border3.Child = textBlock

			window.Show()

		if sender.Messages[sender.Messages.Count - 1].HasAttachments:
			isUpdatable = True
			hashSet = HashSet[String](trendsDictionary.Keys)
		
			for entry in sender.Messages[sender.Messages.Count - 1].Attachments:
				if entry.Score is None:
					isUpdatable = False
					
					break

				else:
					if trendsDictionary.ContainsKey(entry.Title):
						trendsDictionary[entry.Title] = entry.Score
					else:
						trendsDictionary.Add(entry.Title, entry.Score)

					if hashSet.Contains(entry.Title):
						hashSet.Remove(entry.Title)

			if isUpdatable:
				for key in hashSet:
					trendsDictionary.Remove(key)

def visit(character, terms):
	global likesDictionary

	activateSequenceList = List[Sequence]()

	for sequence in Script.Instance.Sequences:
		if sequence.Name.Equals("Activate") and not sequence.Owner.Equals(character.Name):
			activateSequenceList.Add(sequence)

	sourcePreparedSequences = Script.Instance.Prepare(activateSequenceList, None, terms)

	for sourcePreparedSequence in sourcePreparedSequences:
		for c in Script.Instance.Characters:
			if c.Name.Equals(sourcePreparedSequence.Owner):
				exsistingCharacterList = List[Character](Script.Instance.Characters)

				character.Location = Point(c.BaseLocation.X - c.Origin.X + c.Size.Width - character.Origin.X if c.Mirror else c.BaseLocation.X + c.Origin.X - character.Size.Width + character.Origin.X, c.BaseLocation.Y + (c.Size.Height - character.Size.Height) / 2)
				character.Mirror = not c.Mirror

				Script.Instance.Characters.Add(character)
				Script.Instance.Parse(character.Script)

				def onClosing(sender, args):
					isCleared = False
					characterList = List[Character](Script.Instance.Characters)
					sequenceList = List[Sequence]()

					Script.Instance.Characters.Remove(character)
					
					for sequence in List[Sequence](Script.Instance.Sequences):
						if sequence.Owner.Equals(character.Name):
							Script.Instance.Sequences.Remove(sequence)

					while not isCleared:
						isCleared = True

						for c in characterList:
							success, sequence = Script.Instance.TryDequeue(c.Name)

							if success:
								if not character.Name.Equals(c.Name):
									sequenceList.Add(sequence)

								isCleared = False

					Script.Instance.TryEnqueue(sequenceList)
					
				agent = Agent(character.Name)
				agent.Closing += onClosing
				agent.Show()
				
				startSequenceList = List[Sequence]()
				preparedSequenceList = List[Sequence]()

				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Start") and sequence.Owner.Equals(character.Name):
						startSequenceList.Add(sequence)

				for sequence in Script.Instance.Prepare(startSequenceList, None):
					if preparedSequenceList.Count == 0:
						s = Sequence()
						s.Owner = character.Name

						preparedSequenceList.Add(s)

					preparedSequenceList.Add(sequence)

				Script.Instance.TryEnqueue(preparedSequenceList)

				likeSequenceList = List[Sequence]()
						
				for sequence in Script.Instance.Sequences:
					if sequence.Name.Equals("Like") and character.Name.Equals(sequence.Owner):
						likeSequenceList.Add(sequence)

				if not Script.Instance.TryEnqueue(Script.Instance.Prepare(likeSequenceList, ((likesDictionary[character.Name].Count + backingDictionary[character.Name] if backingDictionary.ContainsKey(character.Name) else likesDictionary[character.Name].Count) if likesDictionary.ContainsKey(character.Name) else backingDictionary[character.Name] if backingDictionary.ContainsKey(character.Name) else 0).ToString(CultureInfo.InvariantCulture))):
					s = Sequence()
					s.Owner = character.Name
					s.Add(Collection[Motion]())

					Script.Instance.TryEnqueue([s])

				stringBuilder = StringBuilder()

				for sequence in sourcePreparedSequences:
					for o1 in sequence:
						if clr.GetClrType(Message).IsInstanceOfType(o1):
							for o2 in o1:
								if clr.GetClrType(Entry).IsInstanceOfType(o2):
									o2.Score = None

							if stringBuilder.Length > 0:
								stringBuilder.AppendLine()

							stringBuilder.Append(o1.Text)

				Script.Instance.TryEnqueue(sourcePreparedSequences)
				
				for exsistingCharacter in exsistingCharacterList:
					s = Sequence()
					s.Owner = exsistingCharacter.Name
					s.Add(Collection[Motion]())

					Script.Instance.TryEnqueue([s])

				wordList = List[String]()
				documentDictionary = Dictionary[String, List[String]]()
				wordDictionary = Dictionary[String, List[String]]()
				attributeHashSet = HashSet[String]()
				context = TaskScheduler.FromCurrentSynchronizationContext()
	
				def onLoad():
					fileStream1 = None
					streamReader1 = None
					fileStream2 = None
					streamReader2 = None

					try:
						fileStream1 = FileStream("Words.json", FileMode.Open, FileAccess.Read, FileShare.Read)
						streamReader1 = StreamReader(fileStream1, UTF8Encoding(False), True)
						jsonArray = Json.decode(streamReader1.ReadToEnd())
			
						if jsonArray is not None and clr.GetClrType(Array).IsInstanceOfType(jsonArray):
							for obj in jsonArray:
								if obj is not None and clr.GetClrType(String).IsInstanceOfType(obj):
									wordList.Add(obj)

						fileStream2 = FileStream("Training.json", FileMode.Open, FileAccess.Read, FileShare.Read)
						streamReader2 = StreamReader(fileStream2, UTF8Encoding(False), True)
						jsonDictionary = Json.decode(streamReader2.ReadToEnd())
			
						if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
							for kvp in jsonDictionary:
								if kvp.Value is not None and clr.GetClrType(Array).IsInstanceOfType(kvp.Value):
									list = List[String]()

									for s in kvp.Value:
										if clr.GetClrType(String).IsInstanceOfType(s):
											list.Add(s)
						
									documentDictionary.Add(kvp.Key, list)

					except Exception, e:
						Trace.WriteLine(e.clsException.Message)
						Trace.WriteLine(e.clsException.StackTrace)

					finally:
						if streamReader2 is not None:
							streamReader2.Close()

						if fileStream2 is not None:
							fileStream2.Close()

						if streamReader1 is not None:
							streamReader1.Close()

						if fileStream1 is not None:
							fileStream1.Close()
	
				def onReady(task):
					tempWordDictionary = Dictionary[Char, List[String]]()

					for word in wordList:
						if word.Length > 0:
							if not tempWordDictionary.ContainsKey(word[0]):
								tempWordDictionary.Add(word[0], List[String]())

							tempWordDictionary[word[0]].Add(word)

					blockTermList = getTermList(tempWordDictionary, stringBuilder.ToString())
		
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
							list[i] = Regex.Replace(list[i], "(?<1>(?<Open>\\{{2})*)\\{\\*}(?<2>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", MatchEvaluator(lambda x: String.Concat(x.Groups[1].Value.Substring(x.Groups[1].Length / 2), "{", String.Join("|", attributeHashSet), "}", x.Groups[2].Value.Substring(x.Groups[2].Length / 2)) if x.Groups[1].Success and x.Groups[2].Success else String.Concat(x.Groups[1].Value.Substring(x.Groups[1].Length / 2), "{", String.Join("|", attributeHashSet), "}") if x.Groups[1].Success else String.Concat("{", String.Join("|", attributeHashSet), "}", x.Groups[2].Value.Substring(x.Groups[2].Length / 2)) if x.Groups[2].Success else String.Concat("{", String.Join("|", attributeHashSet), "}")), RegexOptions.CultureInvariant)

					for character in Script.Instance.Characters:
						wordList.Add(character.Name)

						if not wordDictionary.ContainsKey(character.Name):
							wordDictionary.Add(character.Name, List[String]())

						wordDictionary[character.Name].Add("自分")

					if not attributeHashSet.Contains("自分"):
						attributeHashSet.Add("自分")
		
				def onTrain(task):
					tempStringBuilder = StringBuilder(stringBuilder.ToString())
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

					for value in documentDictionary.Values:
						for i in range(value.Count):
							for match in Regex.Matches(value[i], "(?<Open>\\{{2})*\\{(?<1>(?:[^{}]|(?<2>(?:(?:\\{|}){2})+))+)}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
								if match.Groups[2].Success:
									j = match.Groups[1].Index
									sb = StringBuilder()

									for capture in match.Groups[2].Captures:
										if capture.Index > j:
											sb.Append(value[i].Substring(j, capture.Index - j))

										sb.Append(capture.Value.Substring(capture.Length / 2))
										j = capture.Index + capture.Length

									if match.Groups[1].Index + match.Groups[1].Length > j:
										sb.Append(value[i].Substring(j, match.Groups[1].Index + match.Groups[1].Length - j))

									pattern = sb.ToString()

								else:
									pattern = match.Groups[1].Value

								for attribute in attributeHashSet:
									if Regex.IsMatch(attribute, pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline):
										if not usageDictionary.ContainsKey(attribute):
											usageDictionary.Add(attribute, List[String]())

										usageDictionary[attribute].Add(match.Groups[1].Value)

					for value in documentDictionary.Values:
						for i in range(value.Count):
							index = 0
							sb = StringBuilder()

							for match in Regex.Matches(value[i], "(?<1>(?<Open>\\{{2})*)\\{(?<2>(?:[^{}]|(?:(?:\\{|}){2})+)+)}(?<3>(?<Close-Open>}{2})*)(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
								if match.Index - index > 0:
									sb.Append(Regex.Replace(value[i].Substring(index, match.Index - index), "\\{\\{|}}", MatchEvaluator(lambda x: x.Value.Substring(x.Length / 2)), RegexOptions.CultureInvariant))

								if cacheDictionary.ContainsKey(match.Value):
									if match.Groups[1].Success:
										sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

									sb.Append(cacheDictionary[match.Value])

									if match.Groups[3].Success:
										sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))

								else:
									max1 = 0
									word1 = None
									max2 = 0
									word2 = None

									for word in wordList:
										if wordDictionary.ContainsKey(word):
											for attribute in wordDictionary[word]:
												if usageDictionary.ContainsKey(attribute):
													if usageDictionary[attribute].Contains(match.Groups[2].Value):
														if termHashSet.Contains(word):
															if usageDictionary[attribute].Count > max1:
																max1 = usageDictionary[attribute].Count
																word1 = word

														elif usageDictionary[attribute].Count > max2:
															max2 = usageDictionary[attribute].Count
															word2 = word

									if word1 is None:
										if word2 is None:
											sb.Append(match.Value)

										else:
											if match.Groups[1].Success:
												sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

											sb.Append(word2)

											if match.Groups[3].Success:
												sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))

											cacheDictionary.Add(match.Value, word2)

									else:
										if match.Groups[1].Success:
											sb.Append(match.Groups[1].Value.Substring(match.Groups[1].Length / 2))

										sb.Append(word1)

										if match.Groups[3].Success:
											sb.Append(match.Groups[3].Value.Substring(match.Groups[3].Length / 2))
										
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
								if not Regex.IsMatch(s, "(?<Open>\\{{2})*\\{([^{}]|((\\{|}){2})+)+}(?<Close-Open>}{2})*(?(Open)(?!))(?!})", RegexOptions.CultureInvariant):
									naiveBayes.train(s, kvp.Key)

					return naiveBayes

				def onCompleted(task):
					category = task.Result.classify(stringBuilder.ToString())
					tempDictionary = Dictionary[Char, List[String]]()
					sequenceList = List[Sequence]()

					for word in Script.Instance.Words:
						if word.Name.Length > 0:
							if not tempDictionary.ContainsKey(word.Name[0]):
								tempDictionary.Add(word.Name[0], List[String]())

							tempDictionary[word.Name[0]].Add(word.Name)

					availableTermList = getTermList(tempDictionary, stringBuilder.ToString())

					if category is None:
						if availableTermList.Count > 0:
							for sequence in Script.Instance.Sequences:
								if sequence.Name.Equals("Activate") and sequence.Owner.Equals(character.Name):
									sequenceList.Add(sequence)

							targetPreparedSequences = Script.Instance.Prepare(sequenceList, None, availableTermList)

							if not Script.Instance.TryEnqueue(targetPreparedSequences):
								sequenceList.Clear()

								for sequence in Script.Instance.Sequences:
									if sequence.Name.Equals("Ignore") and sequence.Owner.Equals(character.Name):
										sequenceList.Add(sequence)

								targetPreparedSequences = Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), availableTermList)
					
								if not Script.Instance.TryEnqueue(targetPreparedSequences):
									Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), Enumerable.Empty[String]()))

						else:
							for sequence in Script.Instance.Sequences:
								if sequence.Name.Equals("Ignore") and sequence.Owner.Equals(character.Name):
									sequenceList.Add(sequence)

							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), Enumerable.Empty[String]()))

					else:
						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals(category) and sequence.Owner.Equals(character.Name):
								sequenceList.Add(sequence)

						targetPreparedSequences = Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), availableTermList)

						if availableTermList.Count > 0:
							if not Script.Instance.TryEnqueue(targetPreparedSequences):
								targetPreparedSequences = Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), Enumerable.Empty[String]())

								if not Script.Instance.TryEnqueue(targetPreparedSequences):
									sequenceList.Clear()

									for sequence in Script.Instance.Sequences:
										if sequence.Name.Equals("Activate") and sequence.Owner.Equals(character.Name):
											sequenceList.Add(sequence)

									targetPreparedSequences = Script.Instance.Prepare(sequenceList, None, availableTermList)

									if not Script.Instance.TryEnqueue(targetPreparedSequences):
										sequenceList.Clear()

										for sequence in Script.Instance.Sequences:
											if sequence.Name.Equals("Ignore") and sequence.Owner.Equals(character.Name):
												sequenceList.Add(sequence)

										targetPreparedSequences = Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), availableTermList)
							
										if not Script.Instance.TryEnqueue(targetPreparedSequences):
											Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), Enumerable.Empty[String]()))

						elif not Script.Instance.TryEnqueue(targetPreparedSequences):
							sequenceList.Clear()

							for sequence in Script.Instance.Sequences:
								if sequence.Name.Equals("Ignore") and sequence.Owner.Equals(character.Name):
									sequenceList.Add(sequence)

							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, stringBuilder.ToString(), Enumerable.Empty[String]()))
				
					s = Sequence()
					s.Owner = character.Name

					Script.Instance.TryEnqueue([s])

				Task.Factory.StartNew(onLoad, TaskCreationOptions.LongRunning).ContinueWith(onReady, context).ContinueWith[NaiveBayes](onTrain, TaskContinuationOptions.LongRunning).ContinueWith(Action[Task[NaiveBayes]](onCompleted), context)

				return

def enumerateCharacters(directory):
	from System.IO import Path

	characterList = List[Character]()

	for fileName in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories):
		extension = Path.GetExtension(fileName)
		
		if extension.Equals(".xml", StringComparison.OrdinalIgnoreCase):
			fs = None

			try:
				fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
				rootElement = XDocument.Load(fs).Root

				if rootElement.Name.LocalName.Equals("script"):
					for characterElement in rootElement.Elements("character"):
						for sequenceElement in characterElement.Elements("sequence"):
							isReady = False
							
							for attribute in sequenceElement.Attributes():
								if attribute.Name.LocalName.Equals("name") and (attribute.Value.Equals("Greet") or attribute.Value.Equals("Hate") or attribute.Value.Equals("Interest") or attribute.Value.Equals("Thank") or attribute.Value.Equals("Ignore")):
									isReady = True
									
									break
									
							if isReady:
								character = Character()
								originX = 0
								originY = 0
								x = 0
								y = 0
								width = 0
								height = 0

								for attribute in characterElement.Attributes():
									if attribute.Name.LocalName.Equals("name"):
										character.Name = attribute.Value
									elif attribute.Name.LocalName.Equals("origin-x"):
										originX = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("origin-y"):
										originY = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("x") or attribute.Name.LocalName.Equals("left"):
										x = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("y") or attribute.Name.LocalName.Equals("top"):
										y = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("width"):
										width = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("height"):
										height = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)

								character.Origin = Point(originX, originY)
								character.BaseLocation = Point(x, y)
								character.Size = Size(width, height)
								character.Script = fileName

								characterList.Add(character)

								break

			except Exception, e:
				continue

			finally:
				if fs is not None:
					fs.Close()

		elif extension.Equals(".zip", StringComparison.OrdinalIgnoreCase):
			fs = None

			try:
				fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
				
				try:
					zipArchive = ZipArchive(fs)
					fs = None

					for zipArchiveEntry in zipArchive.Entries:
						if zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase):
							s = None

							try:
								s = zipArchiveEntry.Open()
								rootElement = XDocument.Load(s).Root

								if rootElement.Name.LocalName.Equals("script"):
									for characterElement in rootElement.Elements("character"):
										for sequenceElement in characterElement.Elements("sequence"):
											isReady = False
							
											for attribute in sequenceElement.Attributes():
												if attribute.Name.LocalName.Equals("name") and (attribute.Value.Equals("Greet") or attribute.Value.Equals("Hate") or attribute.Value.Equals("Interest") or attribute.Value.Equals("Thank") or attribute.Value.Equals("Ignore")):
													isReady = True
									
													break
									
											if isReady:
												character = Character()
												originX = 0
												originY = 0
												x = 0
												y = 0
												width = 0
												height = 0

												for attribute in characterElement.Attributes():
													if attribute.Name.LocalName.Equals("name"):
														character.Name = attribute.Value
													elif attribute.Name.LocalName.Equals("origin-x"):
														originX = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
													elif attribute.Name.LocalName.Equals("origin-y"):
														originY = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
													elif attribute.Name.LocalName.Equals("x") or attribute.Name.LocalName.Equals("left"):
														x = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
													elif attribute.Name.LocalName.Equals("y") or attribute.Name.LocalName.Equals("top"):
														y = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
													elif attribute.Name.LocalName.Equals("width"):
														width = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
													elif attribute.Name.LocalName.Equals("height"):
														height = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)

												character.Origin = Point(originX, originY)
												character.BaseLocation = Point(x, y)
												character.Size = Size(width, height)
												character.Script = fileName

												characterList.Add(character)

												break

							finally:
								if s is not None:
									s.Close()

				finally:
					zipArchive.Dispose()

			except Exception, e:
				continue

			finally:
				if fs is not None:
					fs.Close()

	return characterList

def createUpdateImageTask(uri):
	from System.IO import Path

	path = None

	if uri.IsAbsoluteUri:
		config = None
		directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)

		if Directory.Exists(directory):
			fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
			for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
				if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
					exeConfigurationFileMap = ExeConfigurationFileMap()
					exeConfigurationFileMap.ExeConfigFilename = fileName2
					config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
		if config is None:
			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
			directory = None

		if config.AppSettings.Settings["Cache"] is not None:
			sha1 = SHA1CryptoServiceProvider()
			stringBuilder = StringBuilder()

			for b in sha1.ComputeHash(Encoding.UTF8.GetBytes(uri.AbsoluteUri)):
				stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture))

			stringBuilder.Append(Path.GetExtension(uri.AbsolutePath))

			tempPath = Path.Combine(config.AppSettings.Settings["Cache"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["Cache"].Value), stringBuilder.ToString())

			if stringBuilder.ToString().IndexOfAny(Path.GetInvalidFileNameChars()) < 0 and File.Exists(tempPath):
				path = tempPath

	else:
		path = uri.ToString()

	if path is None:
		webRequest = WebRequest.Create(uri)
			
		if config.AppSettings.Settings["Timeout"] is not None:
			if config.AppSettings.Settings["Timeout"].Value.Length > 0:
				webRequest.Timeout = Int32.Parse(config.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture)
			
		if config.AppSettings.Settings["UserAgent"] is not None and clr.GetClrType(HttpWebRequest).IsInstanceOfType(webRequest):
			webRequest.UserAgent = config.AppSettings.Settings["UserAgent"].Value

		def onFetch(state):
			ms = None

			if NetworkInterface.GetIsNetworkAvailable():
				request = state
				response = None
				s = None
				bs = None

				try:
					response = request.GetResponse()

					if Regex.IsMatch(response.ContentType, "image/((x-)?bmp|gif|jpeg|png|tiff(-fx)?)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase):
						s = response.GetResponseStream()
						bs = BufferedStream(s)
						s = None
						ms = MemoryStream()
						i = bs.ReadByte()

						while i != -1:
							ms.WriteByte(i)
							i = bs.ReadByte()

						ms.Seek(0, SeekOrigin.Begin)

				except:
					if ms is not None:
						ms.Close()
						ms = None

				finally:
					if bs is not None:
						bs.Close()

					if s is not None:
						s.Close()

					if response is not None:
						response.Close()

			return KeyValuePair[Uri, MemoryStream](uri, ms)
		
		return Task[KeyValuePair[Uri, MemoryStream]](onFetch, webRequest, TaskCreationOptions.LongRunning)
	
	def onLoad(state):
		ms = None
		fs = None
			
		try:
			fs = FileStream(state, FileMode.Open, FileAccess.Read, FileShare.Read)
			ms = MemoryStream()
			buffer = Array.CreateInstance(Byte, fs.Length)
			bytesRead = fs.Read(buffer, 0, buffer.Length)

			while bytesRead > 0:
				ms.Write(buffer, 0, bytesRead)
				bytesRead = fs.Read(buffer, 0, buffer.Length)

			ms.Seek(0, SeekOrigin.Begin)

		except:
			if ms is not None:
				ms.Close()
				ms = None

		finally:
			if fs is not None:
				fs.Close()

		return KeyValuePair[Uri, MemoryStream](uri, ms)

	return Task[KeyValuePair[Uri, MemoryStream]](onLoad, path)

def cropImage(bitmapImage):
	imageBrush = ImageBrush(bitmapImage)
	imageBrush.TileMode = TileMode.None
	imageBrush.Stretch = Stretch.Fill
	imageBrush.ViewboxUnits = BrushMappingMode.Absolute
	imageBrush.Viewbox = Rect((bitmapImage.Width - bitmapImage.Height) / 2 if bitmapImage.Width > bitmapImage.Height else 0, 0 if bitmapImage.Width > bitmapImage.Height else (bitmapImage.Height - bitmapImage.Width) / 2, bitmapImage.Height if bitmapImage.Width > bitmapImage.Height else bitmapImage.Width, bitmapImage.Height if bitmapImage.Width > bitmapImage.Height else bitmapImage.Width)
	imageBrush.AlignmentX = AlignmentX.Left
	imageBrush.AlignmentY = AlignmentY.Top

	drawingGroup = DrawingGroup()
	drawingContext = drawingGroup.Open()
	drawingContext.DrawRectangle(imageBrush, None, Rect(0, 0, bitmapImage.Height if bitmapImage.Width > bitmapImage.Height else bitmapImage.Width, bitmapImage.Height if bitmapImage.Width > bitmapImage.Height else bitmapImage.Width))
	drawingContext.Close()

	drawingImage = DrawingImage(drawingGroup)

	if drawingImage.CanFreeze:
		drawingImage.Freeze()

	return drawingImage

def attachStackPanel(stackPanel, thickness, color, text1, text2):
	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	sp = StackPanel()
	sp.HorizontalAlignment = HorizontalAlignment.Stretch
	sp.VerticalAlignment = VerticalAlignment.Stretch
	sp.Orientation = Orientation.Vertical
	sp.Background = linearGradientBrush

	stackPanel.Children.Add(sp)
	
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
	border1.BorderThickness = thickness
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	sp.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	border1.Child = border2

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

	border2.Child = dockPanel

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

def attachSectionStackPanel(stackPanel, color, text):
	sp = StackPanel()
	sp.HorizontalAlignment = HorizontalAlignment.Left
	sp.VerticalAlignment = VerticalAlignment.Stretch
	sp.Margin = Thickness(10, 0, 10, 5)
	sp.Orientation = Orientation.Vertical
	
	stackPanel.Children.Add(sp)
	
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

	sp.Children.Add(dockPanel)

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
	
def attachAnnotationStackPanel(stackPanel, color, text):
	sp = StackPanel()
	sp.HorizontalAlignment = HorizontalAlignment.Right
	sp.VerticalAlignment = VerticalAlignment.Stretch
	sp.Margin = Thickness(10, 5, 0, 0)
	sp.Orientation = Orientation.Vertical

	stackPanel.Children.Add(sp)
	
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

	sp.Children.Add(dockPanel)

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
	
def attachStackPanelWithProgressBar(stackPanel, thickness, color, text1, text2, brush, progressPercentage):
	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush

	stackPanel.Children.Add(stackPanel1)
	
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
	border1.BorderThickness = thickness
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	stackPanel1.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	border1.Child = border2

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Vertical

	border2.Child = stackPanel2

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
	
	solidColorBrush3 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 102, 102, 102))

	if solidColorBrush3.CanFreeze:
		solidColorBrush3.Freeze()

	canvas = Canvas()
	canvas.HorizontalAlignment = HorizontalAlignment.Left
	canvas.VerticalAlignment = VerticalAlignment.Top
	canvas.Margin = Thickness(5, 10, 5, 10)
	canvas.Background = solidColorBrush3
	canvas.Height = 5

	rectangle = Rectangle()
	rectangle.HorizontalAlignment = HorizontalAlignment.Left
	rectangle.VerticalAlignment = VerticalAlignment.Top
	rectangle.Height = 5
	rectangle.RadiusX = 2.5
	rectangle.RadiusY = 2.5
	rectangle.Fill = brush

	def onLoaded(sender, args):
		width = dockPanel.ActualWidth - canvas.Margin.Left - canvas.Margin.Right
		canvas.Width = width
		canvas.Clip = RectangleGeometry(Rect(0, 0, width, 5), 2.5, 2.5)
		rectangle.Width = width * progressPercentage / 100
				
	dockPanel.Loaded += onLoaded

	stackPanel2.Children.Add(dockPanel)

	solidColorBrush4 = SolidColorBrush(color)

	if solidColorBrush4.CanFreeze:
		solidColorBrush4.Freeze()

	label1 = Label()
	label1.Margin = Thickness(0, 0, 50, 0)
	label1.HorizontalAlignment = HorizontalAlignment.Left
	label1.VerticalAlignment = VerticalAlignment.Stretch
	label1.Foreground = solidColorBrush4
	label1.Content = text1

	RenderOptions.SetClearTypeHint(label1, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label1)

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Right
	label2.VerticalAlignment = VerticalAlignment.Stretch
	label2.Foreground = solidColorBrush4
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	dockPanel.Children.Add(label2)
	stackPanel2.Children.Add(canvas)
	canvas.Children.Add(rectangle)

	Canvas.SetLeft(rectangle, 0)
	Canvas.SetTop(rectangle, 0)

def attachStackPanelWithHint(stackPanel, thickness, color, text1, text2, text3):
	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush

	stackPanel.Children.Add(stackPanel1)
	
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
	border1.BorderThickness = thickness
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	stackPanel1.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	border1.Child = border2

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

	border2.Child = dockPanel

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

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Vertical

	dockPanel.Children.Add(stackPanel2)

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

	stackPanel2.Children.Add(label2)
	stackPanel2.Children.Add(label3)

def attachStackPanelWithHeart(stackPanel, thickness, color, text1, text2, brush):
	from System.Windows.Shapes import Path

	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush

	stackPanel.Children.Add(stackPanel1)
	
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
	border1.BorderThickness = thickness
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	stackPanel1.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	border1.Child = border2

	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	border2.Child = dockPanel

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
	
	dockPanel.Children.Add(border3)

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

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Horizontal
	stackPanel2.Background = Brushes.Transparent

	dockPanel.Children.Add(stackPanel2)

	heartGeometry = createHeartGeometry(Rect(0, 0, 8, 6))

	if heartGeometry.CanFreeze:
		heartGeometry.Freeze()

	path = Path()
	path.HorizontalAlignment = HorizontalAlignment.Stretch
	path.VerticalAlignment = VerticalAlignment.Center
	path.Fill = brush
	path.Data = heartGeometry

	stackPanel2.Children.Add(path)

	border4 = Border()
	border4.HorizontalAlignment = HorizontalAlignment.Stretch
	border4.VerticalAlignment = VerticalAlignment.Stretch
	border4.Margin = Thickness(0)
	border4.Padding = Thickness(0)
	border4.CornerRadius = CornerRadius(0)
	border4.Background = Brushes.Transparent
	border4.Effect = dropShadowEffect

	stackPanel2.Children.Add(border4)

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Stretch
	label2.VerticalAlignment = VerticalAlignment.Center
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	border4.Child = label2

def attachStackPanelWithHeartAndAnnotation(stackPanel, thickness, color, text1, text2, text3, brush):
	from System.Windows.Shapes import Path

	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = linearGradientBrush

	stackPanel.Children.Add(stackPanel1)
	
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
	border1.BorderThickness = thickness
	border1.BorderBrush = solidColorBrush2 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush1

	stackPanel1.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 1, 1) if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else Thickness(1, 1, 0, 0)
	border2.BorderBrush = solidColorBrush1 if Math.Max(Math.Max(color.R, color.G), color.B) > Byte.MaxValue / 2 else solidColorBrush2
	
	border1.Child = border2

	dockPanel = DockPanel()
	dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
	dockPanel.VerticalAlignment = VerticalAlignment.Stretch

	border2.Child = dockPanel

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
	
	dockPanel.Children.Add(border3)

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

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Right
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Horizontal
	stackPanel2.Background = Brushes.Transparent

	dockPanel.Children.Add(stackPanel2)

	heartGeometry = createHeartGeometry(Rect(0, 0, 8, 6))

	if heartGeometry.CanFreeze:
		heartGeometry.Freeze()

	path = Path()
	path.HorizontalAlignment = HorizontalAlignment.Stretch
	path.VerticalAlignment = VerticalAlignment.Center
	path.Fill = brush
	path.Data = heartGeometry

	stackPanel2.Children.Add(path)

	border4 = Border()
	border4.HorizontalAlignment = HorizontalAlignment.Stretch
	border4.VerticalAlignment = VerticalAlignment.Stretch
	border4.Margin = Thickness(0)
	border4.Padding = Thickness(0)
	border4.CornerRadius = CornerRadius(0)
	border4.Background = Brushes.Transparent
	border4.Effect = dropShadowEffect

	stackPanel2.Children.Add(border4)

	label2 = Label()
	label2.HorizontalAlignment = HorizontalAlignment.Stretch
	label2.VerticalAlignment = VerticalAlignment.Center
	label2.Foreground = solidColorBrush3
	label2.FontWeight = FontWeights.Bold
	label2.Content = text2
	
	RenderOptions.SetClearTypeHint(label2, ClearTypeHint.Enabled)
	
	border4.Child = label2

	solidColorBrush4 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, 204, 204, 204))
	solidColorBrush4.Opacity = 0.5

	if solidColorBrush4.CanFreeze:
		solidColorBrush4.Freeze()

	border5 = Border()
	border5.HorizontalAlignment = HorizontalAlignment.Stretch
	border5.VerticalAlignment = VerticalAlignment.Stretch
	border5.Margin = Thickness(5, 0, 0, 0)
	border5.Padding = Thickness(0)
	border5.CornerRadius = CornerRadius(4)
	border5.Background = solidColorBrush4

	stackPanel2.Children.Add(border5)

	border6 = Border()
	border6.HorizontalAlignment = HorizontalAlignment.Stretch
	border6.VerticalAlignment = VerticalAlignment.Stretch
	border6.Margin = Thickness(0)
	border6.Padding = Thickness(0)
	border6.CornerRadius = CornerRadius(0)
	border6.Background = Brushes.Transparent
	border6.Effect = dropShadowEffect

	border5.Child = border6

	label3 = Label()
	label3.HorizontalAlignment = HorizontalAlignment.Stretch
	label3.VerticalAlignment = VerticalAlignment.Center
	label3.Foreground = solidColorBrush3
	label3.FontWeight = FontWeights.Regular
	label3.Content = text3
	
	RenderOptions.SetClearTypeHint(label3, ClearTypeHint.Enabled)
	
	border6.Child = label3

def createColorBarsImage(size):
	drawingGroup = DrawingGroup()
	drawingContext = drawingGroup.Open()
	drawingContext.DrawRectangle(Brushes.Yellow, None, Rect(0, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Cyan, None, Rect(size.Width / 7, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Lime, None, Rect(size.Width / 7 * 2, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Magenta, None, Rect(size.Width / 7 * 3, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Red, None, Rect(size.Width / 7 * 4, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Blue, None, Rect(size.Width / 7 * 5, 0, size.Width / 7, size.Height))
	drawingContext.DrawRectangle(Brushes.Black, None, Rect(size.Width / 7 * 6, 0, size.Width / 7, size.Height))
	drawingContext.Close()

	drawingImage = DrawingImage(drawingGroup)

	if drawingImage.CanFreeze:
		drawingImage.Freeze()

	return drawingImage

def createHeartGeometry(rect):
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

def createStarGeometry(rect):
	streamGeometry = StreamGeometry()
	streamGeometry.FillRule = FillRule.Nonzero

	streamGeometryContext = None

	try:
		streamGeometryContext = streamGeometry.Open()
		streamGeometryContext.BeginFigure(Point(rect.X, rect.Y + rect.Height * 15 / 40), True, True)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 14 / 40, rect.Y + rect.Height * 13 / 40), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width / 2, rect.Y), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 26 / 40, rect.Y + rect.Height * 13 / 40), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width, rect.Y + rect.Height * 15 / 40), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 30 / 40, rect.Y + rect.Height * 25 / 40), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 32 / 40, rect.Y + rect.Height), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width / 2, rect.Y + rect.Height * 33 / 40), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 8 / 40, rect.Y + rect.Height), True, False)
		streamGeometryContext.LineTo(Point(rect.X + rect.Width * 10 / 40, rect.Y + rect.Height * 25 / 40), True, False)
		
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
	
def onStart(sender, args):
	global balloonList, menuItem, separator, timer, chargesDictionary, imageDictionary

	tempList = List[Balloon]()

	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				def onClosing(sender, args):
					for window in Application.Current.Windows:
						if window != Application.Current.MainWindow and window.Owner is None and clr.GetClrType(Agent).IsInstanceOfType(window):
							window.Close()

				def onMouseUp(sender, args):
					from System.Windows.Shapes import Path
					global likesDictionary, trendsDictionary

					if args.ChangedButton == MouseButton.Middle and (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control or args.ChangedButton == MouseButton.Left and (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Control | ModifierKeys.Shift:
						max = 1.5
						chargesList = List[KeyValuePair[String, Double]]()
						tmepChargesList = List[KeyValuePair[String, Double]]()

						for kvp in chargesDictionary:
							sum = 0.0

							for score in kvp.Value:
								sum += score

							if sum >= max:
								chargesList.Add(KeyValuePair[String, Double](kvp.Key, sum))

							else:
								tmepChargesList.Add(KeyValuePair[String, Double](kvp.Key, sum))
								
						if chargesList.Count > 0 or tmepChargesList.Count > 0:
							chargesList.Sort(lambda kvp1, kvp2: String.Compare(kvp1.Key, kvp2.Key, StringComparison.CurrentCulture))
									
							if chargesList.Count < 9:
								def comparison(kvp1, kvp2):
									if kvp1.Value > kvp2.Value:
										return 1

									elif kvp1.Value < kvp2.Value:
										return -1

									return 0

								tmepChargesList.Sort(comparison)
								tmepChargesList.Reverse()

								if tmepChargesList.Count > 9 - chargesList.Count:
									tmepChargesList.RemoveRange(9 - chargesList.Count, tmepChargesList.Count - 9 + chargesList.Count)

								tmepChargesList.Sort(lambda kvp1, kvp2: String.Compare(kvp1.Key, kvp2.Key, StringComparison.CurrentCulture))
								chargesList.AddRange(tmepChargesList)

							termHashSet = HashSet[String]()
							imageHashSet = HashSet[Uri]()
							window = Window()
							contentControl = ContentControl()
							border1 = Border()
							stackPanel1 = StackPanel()
							closeTimer = DispatcherTimer(DispatcherPriority.Background)
							
							def onLoaded1(sender1, args1):
								border1.Width = contentControl.ActualWidth
								border1.Height = contentControl.ActualHeight
								contentControl.Width = contentControl.ActualWidth * 1.5
								contentControl.Height = contentControl.ActualHeight * 1.5
								contentControl.RenderTransform.CenterX = contentControl.Width / 2
								contentControl.RenderTransform.CenterY = contentControl.Height / 2
										
								for element1 in stackPanel1.Children:
									for element2 in element1.Children:
										for element3 in element2.Child.Children:
											if clr.GetClrType(Grid).IsInstanceOfType(element3):
												if clr.GetClrType(ScaleTransform).IsInstanceOfType(element3.RenderTransform):
													element3.RenderTransform.CenterX = element3.ActualWidth / 2
													element3.RenderTransform.CenterY = element3.ActualHeight / 2

								storyboard1 = Storyboard()
								doubleAnimation1 = DoubleAnimation(contentControl.Opacity, 1, TimeSpan.FromMilliseconds(500))
								doubleAnimation2 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
								doubleAnimation3 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseOut
								doubleAnimation1.EasingFunction = doubleAnimation2.EasingFunction = doubleAnimation3.EasingFunction = sineEase

								def onCurrentStateInvalidated(sender, args):
									if sender.CurrentState == ClockState.Filling:
										contentControl.Opacity = 1
										contentControl.RenderTransform.ScaleX = 1
										contentControl.RenderTransform.ScaleY = 1
										storyboard1.Remove(contentControl)

										if not border1.Tag:
											closeTimer.Start()

										for element1 in stackPanel1.Children:
											for element2 in element1.Children:
												for element3 in element2.Child.Children:
													if clr.GetClrType(Grid).IsInstanceOfType(element3):
														for element4 in element3.Children:
															if clr.GetClrType(Grid).IsInstanceOfType(element4):
																for element5 in element4.Children:
																	if clr.GetClrType(Image).IsInstanceOfType(element5):
																		if element5.Tag is None:
																			element5.Source = createColorBarsImage(Size(70, 70))

																			for element6 in element4.Children:
																				if clr.GetClrType(Ellipse).IsInstanceOfType(element6):
																					if element6.Clip is not None:
																						storyboard2 = Storyboard()
																						ra = RectAnimation(element6.Clip.Rect, Rect(0, 0, 50, 0), TimeSpan.FromMilliseconds(500))
																						sineEase = SineEase()

																						sineEase.EasingMode = EasingMode.EaseOut
																						ra.EasingFunction = sineEase

																						storyboard2.Children.Add(ra)

																						Storyboard.SetTarget(ra, element6)
																						Storyboard.SetTargetProperty(ra, PropertyPath("(0).(1)", Ellipse.ClipProperty, RectangleGeometry.RectProperty))
																
																						storyboard2.Begin()

																		elif not imageHashSet.Contains(element5.Tag):
																			imageHashSet.Add(element5.Tag)
																
																			def onUpdated(task):
																				bi = None

																				if task.Result.Value is not None:
																					try:
																						bi = BitmapImage()
																						bi.BeginInit()
																						bi.StreamSource = task.Result.Value
																						bi.CacheOption = BitmapCacheOption.OnLoad
																						bi.CreateOptions = BitmapCreateOptions.None
																						bi.EndInit()

																					except:
																						bi = None

																					finally:
																						task.Result.Value.Close()

																				if imageHashSet.Contains(task.Result.Key):
																					for e1 in stackPanel1.Children:
																						for e2 in e1.Children:
																							if imageDictionary.ContainsKey(e2.Child.Tag):
																								if imageDictionary[e2.Child.Tag].Equals(task.Result.Key):
																									for e3 in e2.Child.Children:
																										if clr.GetClrType(Grid).IsInstanceOfType(e3):
																											for e4 in e3.Children:
																												if clr.GetClrType(Grid).IsInstanceOfType(e4):
																													for e5 in e4.Children:
																														if clr.GetClrType(Image).IsInstanceOfType(e5):
																															if e5.Tag is not None:
																																if e5.Tag.Equals(task.Result.Key):
																																	if bi is None:
																																		e5.Source = createColorBarsImage(Size(70, 70))
																												
																																	else:
																																		e5.Source = cropImage(bi)

																																	for e6 in e4.Children:
																																		if clr.GetClrType(Ellipse).IsInstanceOfType(e6):
																																			if e6.Clip is not None:
																																				storyboard = Storyboard()
																																				ra = RectAnimation(e6.Clip.Rect, Rect(0, 0, 50, 0), TimeSpan.FromMilliseconds(500))
																																				sineEase = SineEase()

																																				sineEase.EasingMode = EasingMode.EaseOut
																																				ra.EasingFunction = sineEase

																																				storyboard.Children.Add(ra)

																																				Storyboard.SetTarget(ra, e6)
																																				Storyboard.SetTargetProperty(ra, PropertyPath("(0).(1)", Ellipse.ClipProperty, RectangleGeometry.RectProperty))
																
																																				storyboard.Begin()

																			task = createUpdateImageTask(imageDictionary[element2.Child.Tag])
																			task.ContinueWith(Action[Task[KeyValuePair[Uri, MemoryStream]]](onUpdated), TaskScheduler.FromCurrentSynchronizationContext())
																			task.Start()

								storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated
								storyboard1.Children.Add(doubleAnimation1)
								storyboard1.Children.Add(doubleAnimation2)
								storyboard1.Children.Add(doubleAnimation3)

								Storyboard.SetTargetProperty(doubleAnimation1, PropertyPath(ContentControl.OpacityProperty))
								Storyboard.SetTargetProperty(doubleAnimation2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
								Storyboard.SetTargetProperty(doubleAnimation3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

								contentControl.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

							def onWindowMouseEnter(sender, args):
								closeTimer.Stop()
								border1.Tag = True

							def onWindowMouseLeave(sender, args):
								if closeTimer.Tag:
									closeTimer.Start()

								border1.Tag = False

							def onClose(sender, args):
								closeTimer.Stop()
						
								storyboard = Storyboard()
								doubleAnimation1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
								doubleAnimation2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
								doubleAnimation3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseIn
								doubleAnimation1.EasingFunction = doubleAnimation2.EasingFunction = doubleAnimation3.EasingFunction = sineEase

								def onCurrentStateInvalidated(sender, args):
									if sender.CurrentState == ClockState.Filling:
										contentControl.Opacity = 0
										contentControl.RenderTransform.ScaleX = 1.5
										contentControl.RenderTransform.ScaleY = 1.5
										storyboard.Remove(contentControl)
										imageHashSet.Clear()
										window.Close()

								storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
								storyboard.Children.Add(doubleAnimation1)
								storyboard.Children.Add(doubleAnimation2)
								storyboard.Children.Add(doubleAnimation3)

								Storyboard.SetTargetProperty(doubleAnimation1, PropertyPath(ContentControl.OpacityProperty))
								Storyboard.SetTargetProperty(doubleAnimation2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
								Storyboard.SetTargetProperty(doubleAnimation3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
								contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
								closeTimer.Tag = False
							
							closeTimer.Tick += onClose
							closeTimer.Interval = TimeSpan.FromSeconds(3)
							closeTimer.Tag = True

							fs = None
							bi = BitmapImage()

							try:
								fs = FileStream("Assets\\Background-Popup.png", FileMode.Open, FileAccess.Read, FileShare.Read)

								bi.BeginInit()
								bi.StreamSource = fs
								bi.CacheOption = BitmapCacheOption.OnLoad
								bi.CreateOptions = BitmapCreateOptions.None
								bi.EndInit()

							finally:
								if fs is not None:
									fs.Close()

							imageBrush = ImageBrush(bi)
							imageBrush.TileMode = TileMode.None
							imageBrush.Stretch = Stretch.Fill
							imageBrush.ViewboxUnits = BrushMappingMode.Absolute
							imageBrush.Viewbox = Rect(0, 0, bi.Width, bi.Height)
							imageBrush.AlignmentX = AlignmentX.Left
							imageBrush.AlignmentY = AlignmentY.Top
							imageBrush.Opacity = 0.5

							dg = DrawingGroup()
							dc = dg.Open()
							dc.DrawRectangle(SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, 0, 0, 0)), None, Rect(0, 0, bi.Width, bi.Height))
							dc.DrawRectangle(imageBrush, None, Rect(0, 0, bi.Width, bi.Height))
							dc.Close()

							window.Owner = Application.Current.MainWindow
							window.Title = Application.Current.MainWindow.Title
							window.WindowStartupLocation = WindowStartupLocation.CenterOwner
							window.AllowsTransparency = True
							window.WindowStyle = WindowStyle.None
							window.ResizeMode = ResizeMode.NoResize
							window.ShowActivated = False
							window.ShowInTaskbar = Application.Current.MainWindow.ContextMenu.Items[5].IsChecked
							window.Topmost = True
							window.SizeToContent = SizeToContent.WidthAndHeight
							window.Background = Brushes.Transparent
							window.Loaded += onLoaded1
							window.MouseEnter += onWindowMouseEnter
							window.MouseLeave += onWindowMouseLeave

							contentControl.UseLayoutRounding = True
							contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
							contentControl.VerticalAlignment = VerticalAlignment.Stretch
							contentControl.Opacity = 0
							contentControl.RenderTransform = ScaleTransform(1, 1)
							
							window.Content = contentControl

							backgroundBrush = ImageBrush(DrawingImage(dg))
							backgroundBrush.TileMode = TileMode.Tile
							backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
							backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
							backgroundBrush.Stretch = Stretch.None

							if backgroundBrush.CanFreeze:
								backgroundBrush.Freeze()

							border1.HorizontalAlignment = HorizontalAlignment.Center
							border1.VerticalAlignment = VerticalAlignment.Center
							border1.Padding = Thickness(2)
							border1.CornerRadius = CornerRadius(4)
							border1.Background = backgroundBrush
							border1.Tag = False

							contentControl.Content = border1

							stackPanel1.HorizontalAlignment = HorizontalAlignment.Center
							stackPanel1.VerticalAlignment = VerticalAlignment.Center
							stackPanel1.Orientation = Orientation.Vertical
							stackPanel1.Background = Brushes.Transparent

							border1.Child = stackPanel1

							stackPanel2 = StackPanel()
							stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
							stackPanel2.VerticalAlignment = VerticalAlignment.Top
							stackPanel2.Orientation = Orientation.Horizontal
							stackPanel2.Background = Brushes.Transparent

							stackPanel1.Children.Add(stackPanel2)
							
							fontSizeConverter = FontSizeConverter()

							for i in range(chargesList.Count):
								if i % 3 == 0:
									stackPanel2 = StackPanel()
									stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
									stackPanel2.VerticalAlignment = VerticalAlignment.Top
									stackPanel2.Orientation = Orientation.Horizontal
									stackPanel2.Background = Brushes.Transparent

									stackPanel1.Children.Add(stackPanel2)

								border2 = Border()
								border2.HorizontalAlignment = HorizontalAlignment.Stretch
								border2.VerticalAlignment = VerticalAlignment.Stretch
								border2.Margin = Thickness(2)
								border2.Padding = Thickness(4)
								border2.CornerRadius = CornerRadius(3)

								grid1 = Grid()
								grid1.HorizontalAlignment = HorizontalAlignment.Center
								grid1.VerticalAlignment = VerticalAlignment.Top
								grid1.Margin = Thickness(0, 10, 0, 10)
								grid1.Background = Brushes.Transparent

								dropShadowEffect1 = DropShadowEffect()
								dropShadowEffect1.BlurRadius = 1
								dropShadowEffect1.Color = Colors.Black
								dropShadowEffect1.Direction = 270
								dropShadowEffect1.Opacity = 0.5
								dropShadowEffect1.ShadowDepth = 1

								if dropShadowEffect1.CanFreeze:
									dropShadowEffect1.Freeze()

								grid1.Effect = dropShadowEffect1

								grid2 = Grid()
								grid2.HorizontalAlignment = HorizontalAlignment.Center
								grid2.VerticalAlignment = VerticalAlignment.Center
								grid2.Background = Brushes.Transparent

								if chargesList[i].Value >= max:
									grid1.RenderTransform = ScaleTransform(1, 1)
									grid2.Clip = RectangleGeometry(Rect(0, 0, 50, 50))

									def onMouseEnter(sender1, args1):
										if not termHashSet.Contains(sender1.Child.Tag):
											if sender1.Tag is not None:
												sender1.Tag.Stop(sender1)

											sender1.Tag = storyboard1 = Storyboard()
											ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
											sineEase = SineEase()

											sineEase.EasingMode = EasingMode.EaseOut
											ca.EasingFunction = sineEase

											def onCurrentStateInvalidated1(sender2, args2):
												if sender2.CurrentState == ClockState.Filling:
													sender1.Background = SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102))
													sender1.Tag = None
													storyboard1.Remove(sender1)

											storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated1
											storyboard1.Children.Add(ca)

											Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

											sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

											for element1 in sender1.Child.Children:
												if clr.GetClrType(Grid).IsInstanceOfType(element1):
													if element1.Tag is not None:
														element1.Tag.Stop(element1)

													element1.Tag = storyboard2 = Storyboard()
													da1 = DoubleAnimation(element1.RenderTransform.ScaleX, 1.1, TimeSpan.FromMilliseconds(500))
													da2 = DoubleAnimation(element1.RenderTransform.ScaleY, 1.1, TimeSpan.FromMilliseconds(500))
													sineEase = SineEase()

													sineEase.EasingMode = EasingMode.EaseOut
													da1.EasingFunction = da2.EasingFunction = sineEase

													def onCurrentStateInvalidated2(sender2, args2):
														if sender2.CurrentState == ClockState.Filling:
															for element2 in sender1.Child.Children:
																if element2.Tag == storyboard2:
																	element2.RenderTransform.ScaleX = 1.1
																	element2.RenderTransform.ScaleY = 1.1
																	element2.Tag = None
																	storyboard2.Remove(element2)

													storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated2
													storyboard2.Children.Add(da1)
													storyboard2.Children.Add(da2)

													Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
													Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
													element1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)
											
									def onMouseLeave(sender1, args1):
										if not termHashSet.Contains(sender1.Child.Tag):
											if sender1.Tag is not None:
												sender1.Tag.Stop(sender1)

											sender1.Tag = storyboard1 = Storyboard()
											ca = ColorAnimation(sender1.Background.Color, Color.FromArgb(0, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
											sineEase = SineEase()

											sineEase.EasingMode = EasingMode.EaseIn
											ca.EasingFunction = sineEase

											def onCurrentStateInvalidated(sender2, args2):
												if sender2.CurrentState == ClockState.Filling:
													sender1.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
													sender1.Tag = None
													storyboard1.Remove(sender1)

											storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated
											storyboard1.Children.Add(ca)

											Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

											sender1.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)

											for element1 in sender1.Child.Children:
												if clr.GetClrType(Grid).IsInstanceOfType(element1):
													if element1.Tag is not None:
														element1.Tag.Stop(element1)

													element1.Tag = storyboard2 = Storyboard()
													da1 = DoubleAnimation(element1.RenderTransform.ScaleX, 1, TimeSpan.FromMilliseconds(500))
													da2 = DoubleAnimation(element1.RenderTransform.ScaleY, 1, TimeSpan.FromMilliseconds(500))
													sineEase = SineEase()

													sineEase.EasingMode = EasingMode.EaseIn
													da1.EasingFunction = da2.EasingFunction = sineEase

													def onCurrentStateInvalidated2(sender2, args2):
														if sender2.CurrentState == ClockState.Filling:
															for element2 in sender1.Child.Children:
																if element2.Tag == storyboard2:
																	element2.RenderTransform.ScaleX = 1
																	element2.RenderTransform.ScaleY = 1
																	element2.Tag = None
																	storyboard2.Remove(element2)

													storyboard2.CurrentStateInvalidated += onCurrentStateInvalidated2
													storyboard2.Children.Add(da1)
													storyboard2.Children.Add(da2)

													Storyboard.SetTargetProperty(da1, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleXProperty))
													Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Grid.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
													element1.BeginStoryboard(storyboard2, HandoffBehavior.SnapshotAndReplace, True)

									def onMouseLeftButtonUp(sender1, args1):
										from System.IO import Path as _Path
										global remainingCount

										if termHashSet.Count < 3:
											if sender1.Tag is not None:
												sender1.Tag.Stop(sender1)

											if termHashSet.Contains(sender1.Child.Tag):
												termHashSet.Remove(sender1.Child.Tag)

												sender1.Tag = storyboard = Storyboard()
												color = Color.FromArgb(0, Byte.MaxValue, 0, 102)
												ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
												sineEase = SineEase()

												sineEase.EasingMode = EasingMode.EaseIn
												ca.EasingFunction = sineEase

												def onCurrentStateInvalidated1(sender2, args2):
													if sender2.CurrentState == ClockState.Filling:
														sender1.Background = SolidColorBrush(color)
														sender1.Tag = None
														storyboard.Remove(sender1)

												storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
												storyboard.Children.Add(ca)

												Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

												sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

											else:
												termHashSet.Add(sender1.Child.Tag)

												sender1.Tag = storyboard = Storyboard()
												color = Color.FromArgb(Byte.MaxValue * 50 / 100, Byte.MaxValue, 0, 102)
												ca = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
												sineEase = SineEase()

												sineEase.EasingMode = EasingMode.EaseOut
												ca.EasingFunction = sineEase

												def onCurrentStateInvalidated1(sender2, args2):
													if sender2.CurrentState == ClockState.Filling:
														sender1.Background = SolidColorBrush(color)
														sender1.Tag = None
														storyboard.Remove(sender1)

												storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
												storyboard.Children.Add(ca)

												Storyboard.SetTargetProperty(ca, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

												sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
												
												if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control and termHashSet.Count > 0 or termHashSet.Count == 3):
													storyboard1 = Storyboard()
													da1 = DoubleAnimation(contentControl.Opacity, 0, TimeSpan.FromMilliseconds(500))
													da2 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
													da3 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
													sineEase = SineEase()

													sineEase.EasingMode = EasingMode.EaseIn
													da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase

													def onCurrentStateInvalidated2(sender, args):
														if sender.CurrentState == ClockState.Filling:
															contentControl.Opacity = 0
															contentControl.RenderTransform.ScaleX = 1.5
															contentControl.RenderTransform.ScaleY = 1.5
															storyboard1.Remove(contentControl)
															imageHashSet.Clear()
															window.Close()

													storyboard1.CurrentStateInvalidated += onCurrentStateInvalidated2
													storyboard1.Children.Add(da1)
													storyboard1.Children.Add(da2)
													storyboard1.Children.Add(da3)

													Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
													Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
													Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
			
													contentControl.BeginStoryboard(storyboard1, HandoffBehavior.SnapshotAndReplace, True)
													closeTimer.Tag = False

													for key in termHashSet:
														if not chargesDictionary.ContainsKey(key):
															break

													else:
														def comparison(s1, s2):
															sum1 = 0.0
															sum2 = 0.0

															for score in chargesDictionary[s1]:
																sum1 += score

															for score in chargesDictionary[s2]:
																sum2 += score
																			
															if sum1 / chargesDictionary[s1].Count > sum2 / chargesDictionary[s2].Count:
																return 1

															elif sum1 / chargesDictionary[s1].Count < sum2 / chargesDictionary[s2].Count:
																return -1
																			
															return 0

														list = List[String](termHashSet)
														list.Sort(comparison)
														list.Reverse()

														for key in termHashSet:
															chargesDictionary.Remove(key)

															if imageDictionary.ContainsKey(key):
																imageDictionary.Remove(key)

														d = Dictionary[String, List[Double]](chargesDictionary)
														chargesDictionary.Clear()
														charges = 0

														for kvp in d:
															chargesDictionary.Add(kvp.Key, kvp.Value)

															sum = 0.0

															for score in kvp.Value:
																sum += score

															if sum >= max:
																charges += 1

														if remainingCount + termHashSet.Count < 5:
															remainingCount += termHashSet.Count
														else:
															remainingCount = 5
														
														sequenceList = List[Sequence]()
								
														for sequence in Script.Instance.Sequences:
															if sequence.Name.Equals("Charge"):
																sequenceList.Add(sequence)

														Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, charges.ToString(CultureInfo.InvariantCulture)))

														maxWidth = Double.MinValue
														maxHeight = Double.MinValue
														bitmapImageList = List[BitmapImage]()
														r = Random(Environment.TickCount)
														index = 0

														for fileName in ["Star-Dark1.png", "Star-Dark2.png", "Star-Dark3.png", "Star-Dark4.png", "Star-Dark5.png", "Star-Light1.png", "Star-Light2.png", "Star-Light3.png", "Star-Light4.png", "Star-Light5.png"]:
															fs = None
																					
															try:
																fs = FileStream(String.Concat("Assets\\", fileName), FileMode.Open, FileAccess.Read, FileShare.Read)
																						
																bi = BitmapImage()
																bi.BeginInit()
																bi.StreamSource = fs
																bi.CacheOption = BitmapCacheOption.OnLoad
																bi.CreateOptions = BitmapCreateOptions.None
																bi.EndInit()

																if bi.Width > maxWidth:
																	maxWidth = bi.Width

																if bi.Height > maxHeight:
																	maxHeight = bi.Height

																bitmapImageList.Add(bi)

															except:
																continue

															finally:
																if fs is not None:
																	fs.Close()

														count = Math.Round(SystemParameters.PrimaryScreenWidth * SystemParameters.PrimaryScreenHeight / maxWidth / maxHeight / bitmapImageList.Count * termHashSet.Count)
														countdownEvent = CountdownEvent(Convert.ToInt32(count))

														while index < count:
															def onLoaded2(sender1, args1):
																storyboard = Storyboard()
																da1 = DoubleAnimation(sender1.Content.Opacity, 1, TimeSpan.FromMilliseconds(500))
																da2 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
																da3 = DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
																da4 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																da5 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																da6 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
																sineEase1 = SineEase()
																sineEase2 = SineEase()

																storyboard.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(1000)))
																sineEase1.EasingMode = EasingMode.EaseOut
																sineEase2.EasingMode = EasingMode.EaseIn
																da1.EasingFunction = da2.EasingFunction = da3.EasingFunction = sineEase1
																da4.BeginTime = da5.BeginTime = da6.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(500))
																da4.EasingFunction = da5.EasingFunction = da6.EasingFunction = sineEase2

																def onCurrentStateInvalidated3(sender2, args2):
																	if sender2.CurrentState == ClockState.Filling:
																		sender1.Close()

																storyboard.CurrentStateInvalidated += onCurrentStateInvalidated3
																storyboard.Children.Add(da1)
																storyboard.Children.Add(da2)
																storyboard.Children.Add(da3)
																storyboard.Children.Add(da4)
																storyboard.Children.Add(da5)
																storyboard.Children.Add(da6)

																Storyboard.SetTarget(da1, sender1.Content)
																Storyboard.SetTarget(da2, sender1.Content)
																Storyboard.SetTarget(da3, sender1.Content)
																Storyboard.SetTarget(da4, sender1.Content)
																Storyboard.SetTarget(da5, sender1.Content)
																Storyboard.SetTarget(da6, sender1.Content)
																Storyboard.SetTargetProperty(da1, PropertyPath(ContentControl.OpacityProperty))
																Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																Storyboard.SetTargetProperty(da3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
																Storyboard.SetTargetProperty(da4, PropertyPath(ContentControl.OpacityProperty))
																Storyboard.SetTargetProperty(da5, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
																Storyboard.SetTargetProperty(da6, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

																storyboard.Begin()

															def onClosed(sender1, args1):
																countdownEvent.Signal()
					
															bi = bitmapImageList[r.Next(bitmapImageList.Count)]
															width = Convert.ToInt32(bi.Width) / 2
															height = Convert.ToInt32(bi.Height) / 2
							
															w = Window()
															w.Owner = Application.Current.MainWindow
															w.Title = Application.Current.MainWindow.Title
															w.Left = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenWidth) - width)
															w.Top = r.Next(Convert.ToInt32(SystemParameters.PrimaryScreenHeight) - height)
															w.AllowsTransparency = True
															w.WindowStyle = WindowStyle.None
															w.ResizeMode = ResizeMode.NoResize
															w.ShowActivated = False
															w.ShowInTaskbar = False
															w.Topmost = True
															w.SizeToContent = SizeToContent.WidthAndHeight
															w.Background = Brushes.Transparent
															w.Loaded += onLoaded2
															w.Closed += onClosed
													
															cc = ContentControl()
															cc.UseLayoutRounding = True
															cc.HorizontalAlignment = HorizontalAlignment.Stretch
															cc.VerticalAlignment = VerticalAlignment.Stretch
															cc.Opacity = 0
															cc.RenderTransform = ScaleTransform(0, 0, width / 2, height / 2)
															
															w.Content = cc
															
															image = Image()
															image.CacheMode = BitmapCache(1)
															image.HorizontalAlignment = HorizontalAlignment.Left
															image.VerticalAlignment = VerticalAlignment.Top
															image.Source = bi
															image.Width = height
															image.Height = height
															image.Stretch = Stretch.Uniform
															
															cc.Content = image
															
															w.Show()

															index += 1

														if not Application.Current.MainWindow.ContextMenu.Items[7].IsChecked:
															def onPlay():
																soundPlayer = None

																try:
																	soundPlayer = SoundPlayer("Assets\\Transform.wav")
																	soundPlayer.Load()
																	soundPlayer.PlaySync()

																finally:
																	if soundPlayer is not None:
																		soundPlayer.Dispose()
									
															Task.Factory.StartNew(onPlay)

														def onRun(state):
															countdownEvent.Wait()
															countdownEvent.Dispose()

															return enumerateCharacters(state)

														def onCompleted(task):
															if task.Result.Count > 1:
																characterList = List[Character]()

																for character in task.Result:
																	isNew = True

																	for c in Script.Instance.Characters:
																		if character.Name.Equals(c.Name):
																			isNew = False

																			break

																	if isNew:
																		success, likes = likesDictionary.TryGetValue(character.Name)

																		if success:
																			for i in range(likes.Count):
																				characterList.Add(character)

																		characterList.Add(character)

																if characterList.Count > 0:
																	visit(characterList[Random(Environment.TickCount).Next(characterList.Count)], list)

																else:
																	sequenceList = List[Sequence]()

																	for sequence in Script.Instance.Sequences:
																		if sequence.Name.Equals("Activate"):
																			sequenceList.Add(sequence)

																	Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

															else:
																sequenceList = List[Sequence]()

																for sequence in Script.Instance.Sequences:
																	if sequence.Name.Equals("Activate"):
																		sequenceList.Add(sequence)

																Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, None, list))

														Task.Factory.StartNew[List[Character]](onRun, _Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), TaskCreationOptions.LongRunning).ContinueWith(Action[Task[List[Character]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

									border2.Background = SolidColorBrush(Color.FromArgb(0, Byte.MaxValue, 0, 102))
									border2.MouseEnter += onMouseEnter
									border2.MouseLeave += onMouseLeave
									border2.MouseLeftButtonUp += onMouseLeftButtonUp

								else:
									grid2.Opacity = 0.75 + 0.25 * chargesList[i].Value / max
									grid2.Clip = RectangleGeometry(Rect(0, 50 - 50 * chargesList[i].Value / max, 50, 50 * chargesList[i].Value / max))
											
									border2.Background = Brushes.Transparent

								stackPanel2.Children.Add(border2)
										
								dockPanel = DockPanel()
								dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch
								dockPanel.VerticalAlignment = VerticalAlignment.Stretch
								dockPanel.Tag = chargesList[i].Key
								
								border2.Child = dockPanel
								
								dockPanel.Children.Add(grid1)

								DockPanel.SetDock(grid1, Dock.Top)
								
								solidColorBrush1 = SolidColorBrush(Color.FromArgb(Byte.MaxValue * 50 / 100, 102, 102, 102))

								if solidColorBrush1.CanFreeze:
									solidColorBrush1.Freeze()

								ellipse1 = Ellipse()
								ellipse1.HorizontalAlignment = HorizontalAlignment.Center
								ellipse1.VerticalAlignment = VerticalAlignment.Center
								ellipse1.Fill = solidColorBrush1
								ellipse1.Width = 50
								ellipse1.Height = 50

								grid1.Children.Add(ellipse1)
								grid1.Children.Add(grid2)

								ellipseGeometry = EllipseGeometry(Rect(0, 0, 50, 50))

								if ellipseGeometry.CanFreeze:
									ellipseGeometry.Freeze()

								image = Image()
								image.HorizontalAlignment = HorizontalAlignment.Left
								image.VerticalAlignment = VerticalAlignment.Top
								image.Width = 50
								image.Height = 50
								image.Stretch = Stretch.Fill
								image.Clip = ellipseGeometry

								if imageDictionary.ContainsKey(chargesList[i].Key):
									image.Tag = imageDictionary[chargesList[i].Key]

								grid2.Children.Add(image)

								solidColorBrush2 = SolidColorBrush(Color.FromArgb(Byte.MaxValue, Byte.MaxValue, 0, 102))

								if solidColorBrush2.CanFreeze:
									solidColorBrush2.Freeze()

								ellipse2 = Ellipse()
								ellipse2.HorizontalAlignment = HorizontalAlignment.Left
								ellipse2.VerticalAlignment = VerticalAlignment.Top
								ellipse2.Fill = solidColorBrush2
								ellipse2.Width = 50
								ellipse2.Height = 50
								ellipse2.Clip = RectangleGeometry(Rect(0, 0, 50, 50))

								grid2.Children.Add(ellipse2)

								ellipse3 = Ellipse()
								ellipse3.HorizontalAlignment = HorizontalAlignment.Center
								ellipse3.VerticalAlignment = VerticalAlignment.Center
								ellipse3.Stroke = Brushes.White
								ellipse3.StrokeThickness = 4
								ellipse3.Width = 50 + 8
								ellipse3.Height = 50 + 8

								grid1.Children.Add(ellipse3)

								starGeometry = createStarGeometry(Rect(0, 0, 20, 20))

								if starGeometry.CanFreeze:
									starGeometry.Freeze()

								path = Path()
								path.HorizontalAlignment = HorizontalAlignment.Center
								path.VerticalAlignment = VerticalAlignment.Center
								path.Fill = Brushes.White
								path.Data = starGeometry

								dropShadowEffect2 = DropShadowEffect()
								dropShadowEffect2.BlurRadius = 1
								dropShadowEffect2.Color = Colors.Black
								dropShadowEffect2.Direction = 270
								dropShadowEffect2.Opacity = 0.5
								dropShadowEffect2.ShadowDepth = 1

								if dropShadowEffect2.CanFreeze:
									dropShadowEffect2.Freeze()

								path.Effect = dropShadowEffect2

								grid1.Children.Add(path)

								border3 = Border()
								border3.HorizontalAlignment = HorizontalAlignment.Center
								border3.VerticalAlignment = VerticalAlignment.Top
								border3.Margin = Thickness(0)
								border3.Padding = Thickness(0)
								border3.CornerRadius = CornerRadius(0)
								border3.Width = 75
								border3.Background = Brushes.Transparent

								dropShadowEffect3 = DropShadowEffect()
								dropShadowEffect3.BlurRadius = 1
								dropShadowEffect3.Color = Colors.Black
								dropShadowEffect3.Direction = 270
								dropShadowEffect3.Opacity = 0.5
								dropShadowEffect3.ShadowDepth = 1

								if dropShadowEffect3.CanFreeze:
									dropShadowEffect3.Freeze()

								border3.Effect = dropShadowEffect3

								dockPanel.Children.Add(border3)

								DockPanel.SetDock(border3, Dock.Bottom)

								textBlock = TextBlock()
								textBlock.HorizontalAlignment = HorizontalAlignment.Center
								textBlock.VerticalAlignment = VerticalAlignment.Top
								textBlock.MaxWidth = 75
								textBlock.Text = chargesList[i].Key
								textBlock.Foreground = Brushes.White
								textBlock.FontSize = fontSizeConverter.ConvertFromString("9pt")
								textBlock.FontWeight = FontWeights.Bold
								textBlock.TextAlignment = TextAlignment.Center
								textBlock.TextWrapping = TextWrapping.Wrap

								RenderOptions.SetClearTypeHint(textBlock, ClearTypeHint.Enabled)

								border3.Child = textBlock

							window.Show()

					elif trendsDictionary.Count > 0 and args.ChangedButton == MouseButton.Left and (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control)) == ModifierKeys.Alt | ModifierKeys.Control:
						window = Window()
						contentControl = ContentControl()
						stackPanel1 = StackPanel()
						closeTimer = DispatcherTimer(DispatcherPriority.Background)
						
						def onLoaded1(sender, args):
							storyboard = Storyboard()

							def onCurrentStateInvalidated(sender, args):
								if sender.CurrentState == ClockState.Filling:
									for element1 in stackPanel1.Children:
										for element2 in element1.Children:
											element2.Opacity = 1

									storyboard.Remove(contentControl)

									if not stackPanel1.Tag:
										closeTimer.Start()

							storyboard.CurrentStateInvalidated += onCurrentStateInvalidated

							r = Random(Environment.TickCount)

							for element1 in stackPanel1.Children:
								for element2 in element1.Children:
									doubleAnimation = DoubleAnimation(element2.Opacity, 1, TimeSpan.FromMilliseconds(500))
									doubleAnimation.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(500)))
									sineEase = SineEase()

									sineEase.EasingMode = EasingMode.EaseOut
									doubleAnimation.EasingFunction = sineEase

									storyboard.Children.Add(doubleAnimation)
								
									Storyboard.SetTarget(doubleAnimation, element2)
									Storyboard.SetTargetProperty(doubleAnimation, PropertyPath(Border.OpacityProperty))

							contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

						def onWindowMouseEnter(sender, args):
							closeTimer.Stop()
							stackPanel1.Tag = True

						def onWindowMouseLeave(sender, args):
							if closeTimer.Tag:
								closeTimer.Start()

							stackPanel1.Tag = False

						def onClose(sender, args):
							closeTimer.Stop()

							storyboard = Storyboard()

							def onCurrentStateInvalidated(sender, args):
								if sender.CurrentState == ClockState.Filling:
									for element1 in stackPanel1.Children:
										for element2 in element1.Children:
											element2.Opacity = 0

									storyboard.Remove(contentControl)
									window.Close()

							storyboard.CurrentStateInvalidated += onCurrentStateInvalidated

							r = Random(Environment.TickCount)

							for element1 in stackPanel1.Children:
								for element2 in element1.Children:
									doubleAnimation = DoubleAnimation(element2.Opacity, 0, TimeSpan.FromMilliseconds(500))
									doubleAnimation.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(500)))
									sineEase = SineEase()

									sineEase.EasingMode = EasingMode.EaseIn
									doubleAnimation.EasingFunction = sineEase

									storyboard.Children.Add(doubleAnimation)

									Storyboard.SetTarget(doubleAnimation, element2)
									Storyboard.SetTargetProperty(doubleAnimation, PropertyPath(Border.OpacityProperty))

							contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
							closeTimer.Tag = False

						closeTimer.Tick += onClose
						closeTimer.Interval = TimeSpan.FromSeconds(3)
						closeTimer.Tag = True

						window.Owner = Application.Current.MainWindow
						window.Title = Application.Current.MainWindow.Title
						window.WindowStartupLocation = WindowStartupLocation.CenterScreen
						window.AllowsTransparency = True
						window.WindowStyle = WindowStyle.None
						window.ResizeMode = ResizeMode.NoResize
						window.ShowActivated = False
						window.ShowInTaskbar = Application.Current.MainWindow.ContextMenu.Items[5].IsChecked
						window.Topmost = True
						window.SizeToContent = SizeToContent.WidthAndHeight
						window.Background = Brushes.Transparent
						window.Loaded += onLoaded1
						window.MouseEnter += onWindowMouseEnter
						window.MouseLeave += onWindowMouseLeave

						contentControl.UseLayoutRounding = True
						contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
						contentControl.VerticalAlignment = VerticalAlignment.Stretch

						window.Content = contentControl

						stackPanel1.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel1.VerticalAlignment = VerticalAlignment.Center
						stackPanel1.Orientation = Orientation.Vertical
						stackPanel1.Background = Brushes.Transparent
						stackPanel1.Tag = False

						contentControl.Content = stackPanel1

						stackPanel2 = StackPanel()
						stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
						stackPanel2.VerticalAlignment = VerticalAlignment.Top
						stackPanel2.Orientation = Orientation.Horizontal
						stackPanel2.Background = Brushes.Transparent

						stackPanel1.Children.Add(stackPanel2)

						keyList = List[String](trendsDictionary.Keys)
						max = Double.MinValue
						
						if keyList.Count > 100:
							keyList.Sort(lambda s1, s2: trendsDictionary[s1] - trendsDictionary[s2])
							keyList.Reverse()
							keyList.RemoveRange(100, keyList.Count - 100)

						for key in keyList:
							if max < trendsDictionary[key]:
								max = trendsDictionary[key]

						keyList.Sort(lambda s1, s2: String.Compare(s1, s2, StringComparison.CurrentCulture))
						
						fontSizeConverter = FontSizeConverter()

						for key in keyList:
							if stackPanel2.Children.Count == 10:
								stackPanel2 = StackPanel()
								stackPanel2.HorizontalAlignment = HorizontalAlignment.Center
								stackPanel2.VerticalAlignment = VerticalAlignment.Top
								stackPanel2.Orientation = Orientation.Horizontal
								stackPanel2.Background = Brushes.Transparent

								stackPanel1.Children.Add(stackPanel2)

							border1 = Border()

							def onMouseEnter(sender1, args1):
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								sender1.Tag = storyboard = Storyboard()
								colorAnimation = ColorAnimation(sender1.Background.Color, Color.FromArgb(Byte.MaxValue, Byte.MaxValue, 0, 102), TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseOut
								colorAnimation.EasingFunction = sineEase

								def onCurrentStateInvalidated1(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Background = SolidColorBrush(Color.FromArgb(Byte.MaxValue, Byte.MaxValue, 0, 102))
										sender1.Tag = None
										storyboard.Remove(sender1)

								storyboard.CurrentStateInvalidated += onCurrentStateInvalidated1
								storyboard.Children.Add(colorAnimation)

								Storyboard.SetTargetProperty(colorAnimation, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

								sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

							def onMouseLeave(sender1, args1):
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								sender1.Tag = storyboard = Storyboard()
								colorAnimation = ColorAnimation(sender1.Background.Color, Color.FromArgb(Convert.ToByte(Byte.MaxValue * sender1.Child.Tag), 51, 51, 50), TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseIn
								colorAnimation.EasingFunction = sineEase

								def onCurrentStateInvalidated(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Background = SolidColorBrush(Color.FromArgb(Convert.ToByte(Byte.MaxValue * sender1.Child.Tag), 51, 51, 50))
										sender1.Tag = None
										storyboard.Remove(sender1)

								storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
								storyboard.Children.Add(colorAnimation)

								Storyboard.SetTargetProperty(colorAnimation, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

								sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

							def onMouseLeftButtonUp(sender1, args1):
								if sender1.Tag is not None:
									sender1.Tag.Stop(sender1)

								sender1.Tag = storyboard = Storyboard()
								color = Color.FromArgb(Byte.MaxValue, Byte.MaxValue, 0, 102)
								colorAnimation = ColorAnimation(sender1.Background.Color, color, TimeSpan.FromMilliseconds(500))
								sineEase = SineEase()

								sineEase.EasingMode = EasingMode.EaseIn
								colorAnimation.EasingFunction = sineEase

								def onCurrentStateInvalidated(sender2, args2):
									if sender2.CurrentState == ClockState.Filling:
										sender1.Background = SolidColorBrush(color)
										sender1.Tag = None
										storyboard.Remove(sender1)

								storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
								storyboard.Children.Add(colorAnimation)

								Storyboard.SetTargetProperty(colorAnimation, PropertyPath("(0).(1)", Border.BackgroundProperty, SolidColorBrush.ColorProperty))

								sender1.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

								closeTimer.Stop()

								sb = Storyboard()

								def onCurrentStateInvalidated(sender, args):
									if sender.CurrentState == ClockState.Filling:
										for element1 in stackPanel1.Children:
											for element2 in element1.Children:
												element2.Opacity = 0

										sb.Remove(contentControl)
										window.Close()

								sb.CurrentStateInvalidated += onCurrentStateInvalidated

								r = Random(Environment.TickCount)

								for element1 in stackPanel1.Children:
									for element2 in element1.Children:
										doubleAnimation = DoubleAnimation(element2.Opacity, 0, TimeSpan.FromMilliseconds(500))

										if not sender1 == element2:
											doubleAnimation.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(500)))

										sineEase = SineEase()

										sineEase.EasingMode = EasingMode.EaseIn
										doubleAnimation.EasingFunction = sineEase

										sb.Children.Add(doubleAnimation)

										Storyboard.SetTarget(doubleAnimation, element2)
										Storyboard.SetTargetProperty(doubleAnimation, PropertyPath(Border.OpacityProperty))

								contentControl.BeginStoryboard(sb, HandoffBehavior.SnapshotAndReplace, True)
								closeTimer.Tag = False

								Script.Instance.Search(sender1.Child.Child.Tag)

							score = trendsDictionary[key] / max

							border1.HorizontalAlignment = HorizontalAlignment.Center
							border1.VerticalAlignment = VerticalAlignment.Center
							border1.Margin = Thickness(4)
							border1.Padding = Thickness(16, 10, 16, 10)
							border1.CornerRadius = CornerRadius(4)
							border1.Background = SolidColorBrush(Color.FromArgb(Convert.ToByte(Byte.MaxValue * score), 51, 51, 50))
							border1.Opacity = 0
							border1.MouseEnter += onMouseEnter
							border1.MouseLeave += onMouseLeave
							border1.MouseLeftButtonUp += onMouseLeftButtonUp

							stackPanel2.Children.Add(border1)

							dropShadowEffect = DropShadowEffect()
							dropShadowEffect.BlurRadius = 1
							dropShadowEffect.Color = Colors.Black
							dropShadowEffect.Direction = 270
							dropShadowEffect.Opacity = 0.5
							dropShadowEffect.ShadowDepth = 1

							if dropShadowEffect.CanFreeze:
								dropShadowEffect.Freeze()

							border2 = Border()
							border2.HorizontalAlignment = HorizontalAlignment.Center
							border2.VerticalAlignment = VerticalAlignment.Center
							border2.Margin = Thickness(0)
							border2.Padding = Thickness(0)
							border2.Effect = dropShadowEffect
							border2.Tag = score

							border1.Child = border2

							textBlock = TextBlock()
							textBlock.HorizontalAlignment = HorizontalAlignment.Center
							textBlock.VerticalAlignment = VerticalAlignment.Center
							textBlock.Text = key.ToUpper()
							textBlock.Foreground = Brushes.White
							textBlock.FontSize = fontSizeConverter.ConvertFromString("12pt")
							textBlock.FontWeight = FontWeights.Bold
							textBlock.TextAlignment = TextAlignment.Center
							textBlock.TextWrapping = TextWrapping.NoWrap
							textBlock.Tag = key

							RenderOptions.SetClearTypeHint(textBlock, ClearTypeHint.Enabled)

							border2.Child = textBlock

						window.Show()

				window.Closing += onClosing
				window.MouseUp += onMouseUp
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)

		if clr.GetClrType(Balloon).IsInstanceOfType(window):
			if not balloonList.Contains(window):
				window.IsVisibleChanged += onIsVisibleChanged
				balloonList.Add(window)
				
			tempList.Add(window)

	balloonList.Clear()
	balloonList.AddRange(tempList)
	
	if File.Exists("Likes.json"):
		nameList = List[String]()
		currentLikesDictionary = Dictionary[String, List[DateTime]]()

		for character in Script.Instance.Characters:
			nameList.Add(character.Name)

		def onUpdate():
			try:
				fileStream = None
				streamReader = None
				
				try:
					dt1 = DateTime.Now - TimeSpan(7 * 2, 0, 0, 0)
					fileStream = FileStream("Likes.json", FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
					encoding = UTF8Encoding(False)
					streamReader = StreamReader(fileStream, encoding, True)
					jsonDictionary = Json.decode(streamReader.ReadToEnd())

					if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary):
						keyHashSet = HashSet[String](jsonDictionary.Keys)

						for key in nameList.ConvertAll[String](lambda x: Convert.ToBase64String(Encoding.UTF8.GetBytes(x))):
							if not keyHashSet.Contains(key):
								keyHashSet.Add(key)

						for key in keyHashSet:
							name = Encoding.UTF8.GetString(Convert.FromBase64String(key))
							
							currentLikesDictionary.Add(name, List[DateTime]())

							if jsonDictionary.ContainsKey(key):
								list = List[String]()

								if jsonDictionary[key] is not None:
									if clr.GetClrType(Array).IsInstanceOfType(jsonDictionary[key]):
										for value in jsonDictionary[key]:
											if clr.GetClrType(String).IsInstanceOfType(value):
												dt2 = DateTime(Int64.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value))))

												if dt2 > dt1:
													currentLikesDictionary[name].Add(dt2)
													list.Add(value)

									else:
										currentLikesDictionary.Clear()

										return

								if list.Count > 0 or nameList.Contains(name):
									jsonDictionary[key] = list.ToArray()
								else:
									jsonDictionary.Remove(key)

						if jsonDictionary.Count > 0:
							json = Json.encode(jsonDictionary)

							if json is not None:
								fileStream.SetLength(0)
								streamWriter = None
						
								try:
									streamWriter = StreamWriter(fileStream, encoding)
									streamWriter.Write(json)

								finally:
									if streamWriter is not None:
										streamWriter.Close()
						
						else:
							streamReader.Close()
							streamReader = None
							fileStream.Close()
							fileStream = None
							File.Delete("Likes.json")

				finally:
					if streamReader is not None:
						streamReader.Close()

					if fileStream is not None:
						fileStream.Close()

			except Exception, e:
				Trace.WriteLine(e.clsException.Message)
				Trace.WriteLine(e.clsException.StackTrace)

		def onCompleted(task):
			global likesDictionary

			likesDictionary.Clear()

			for kvp in currentLikesDictionary:
				likesDictionary.Add(kvp.Key, kvp.Value)
						
				for name in nameList:
					if name.Equals(kvp.Key):
						sequenceList = List[Sequence]()
				
						for sequence in Script.Instance.Sequences:
							if sequence.Name.Equals("Like") and kvp.Key.Equals(sequence.Owner):
								sequenceList.Add(sequence)

						if backingDictionary.ContainsKey(name):
							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, (backingDictionary[name] + kvp.Value.Count).ToString(CultureInfo.InvariantCulture)))
						else:
							Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, kvp.Value.Count.ToString(CultureInfo.InvariantCulture)))

						break
				
		Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

	timer.Start()
	chargesDictionary.Clear()
	imageDictionary.Clear()

def onStop(sender, args):
	global timer

	timer.Stop()

likesDictionary = Dictionary[String, List[DateTime]]()
remainingCount = 0
backingDictionary = Dictionary[String, Int32]()
chargesDictionary = Dictionary[String, List[Double]]()
imageDictionary = Dictionary[String, Uri]()
trendsDictionary = Dictionary[String, Double]()
dateTime = DateTime.Now - TimeSpan(12, 0, 0)
recentEntryList = List[Entry]()
recentWordList = List[Word]()
balloonList = List[Balloon]()
menuItem = MenuItem()
separator = None

if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
	menuItem.Header = "コミュニケーション"
else:
	menuItem.Header = "Play"

timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop
