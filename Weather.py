# -*- coding: utf-8 -*-
# Weather.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Core")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, Nullable, Byte, UInt32, Double, Char, String, Uri, DateTime, TimeSpan, Array, StringComparison, Convert, BitConverter, Math, Action
from System.Collections.Generic import List, Dictionary, KeyValuePair, HashSet
from System.IO import Stream, StreamReader, MemoryStream, FileStream, SeekOrigin, FileMode, FileAccess, FileShare
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, NumberStyles
from System.Text import StringBuilder, Encoding
from System.Text.RegularExpressions import Regex, Match, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Linq import Enumerable
from System.Net import WebRequest, WebResponse
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, WindowStyle, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Rect, PropertyPath
from System.Windows.Controls import ContentControl, Grid
from System.Windows.Media import Color, Colors, Brushes, SolidColorBrush, TranslateTransform, ScaleTransform, RectangleGeometry, EllipseGeometry, ImageBrush, TileMode, BrushMappingMode, Stretch, AlignmentX, AlignmentY, DrawingGroup, DrawingContext, DrawingImage
from System.Windows.Media.Animation import Storyboard, Clock, ClockState, DoubleAnimation, SineEase, EasingMode
from System.Windows.Media.Effects import DropShadowEffect
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
from System.Windows.Shapes import Rectangle
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Script, Sequence

# e.g. Tokyo, Japan
location = "Tokyo, Japan"

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

def onTick(timer, e):
	def onUpdate():
		temp = 0
		windSpeed = 0
		windDeg = 0
		weatherIdList = List[Double]()
		weatherPathHashSet = HashSet[String]()
		weatherStreamList = List[MemoryStream]()
		weatherConditionList = List[String]()

		if NetworkInterface.GetIsNetworkAvailable():
			try:
				request = WebRequest.Create(Uri(String.Concat("http://api.openweathermap.org/data/2.5/find?q=", urlEncode(location), "&units=metric&cnt=1")))
				response = None
				stream = None
				streamReader = None

				try:
					nowDateTime = DateTime.Now
					response = request.GetResponse()
					stream = response.GetResponseStream()
					streamReader = StreamReader(stream)
					jsonDictionary = JsonDecoder.decode(streamReader.ReadToEnd())

					if jsonDictionary is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(jsonDictionary) and jsonDictionary.ContainsKey("list") and jsonDictionary["list"] is not None and clr.GetClrType(Array).IsInstanceOfType(jsonDictionary["list"]):
						for obj in jsonDictionary["list"]:
							if obj is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj):
								if obj.ContainsKey("main") and obj["main"] is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["main"]) and obj["main"].ContainsKey("temp"):
									temp = obj["main"]["temp"]

								if obj.ContainsKey("wind") and obj["wind"] is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(obj["wind"]):
									if obj["wind"].ContainsKey("speed"):
										windSpeed = obj["wind"]["speed"]

									if obj["wind"].ContainsKey("deg"):
										windDeg = obj["wind"]["deg"]

								if obj.ContainsKey("weather") and obj["weather"] is not None and clr.GetClrType(Array).IsInstanceOfType(obj["weather"]):
									for o in obj["weather"]:
										if o is not None and clr.GetClrType(Dictionary[String, Object]).IsInstanceOfType(o) and o.ContainsKey("id") and o["id"] is not None:
											weatherIdList.Add(o["id"])
																
								for id in weatherIdList:
									digit = Convert.ToInt32(id / 100)
									path = None
									s = None

									if digit == 2:
										path = "Assets\\Cloud-Lightning.png"
										weatherConditionList.Add("Thunderstorm")

									elif  digit == 3:
										path = "Assets\\Cloud-Drizzle.png"
										weatherConditionList.Add("Drizzle")
														
									elif  digit == 5:
										d = Convert.ToInt32(id / 10)

										if d == 0:
											if nowDateTime.Hour > 6 and nowDateTime.Hour <= 18:
												path = "Assets\\Cloud-Rain-Sun.png"
																
											else:
												path = "Assets\\Cloud-Rain-Moon.png"

										else:
											path = "Assets\\Cloud-Rain.png"

										weatherConditionList.Add("Rain")
														
									elif  digit == 6:
										path = "Assets\\Cloud-Snow.png"
										weatherConditionList.Add("Snow")
														
									elif  digit == 7:
										path = "Assets\\Cloud-Fog.png"
															
										if Convert.ToInt32(id) == 701:
											weatherConditionList.Add("Mist")

										elif Convert.ToInt32(id) == 711:
											weatherConditionList.Add("Smoke")

										elif Convert.ToInt32(id) == 721:
											weatherConditionList.Add("Haze")

										elif Convert.ToInt32(id) == 731:
											weatherConditionList.Add("Dust")

										elif Convert.ToInt32(id) == 741:
											weatherConditionList.Add("Fog")
														
									elif  digit == 8:
										if Convert.ToInt32(id) == 800:
											if nowDateTime.Hour > 6 and nowDateTime.Hour <= 18:
												path = "Assets\\Sun.png"
												weatherConditionList.Add("Sunny")
																
											else:
												path = "Assets\\Moon.png"
												weatherConditionList.Add("Clear")

										elif Convert.ToInt32(id) >= 801 and Convert.ToInt32(id) <= 803:
											if nowDateTime.Hour > 6 and nowDateTime.Hour <= 18:
												path = "Assets\\Cloud-Sun.png"
																
											else:
												path = "Assets\\Cloud-Moon.png"

											weatherConditionList.Add("Cloudy")

										elif Convert.ToInt32(id) == 804:
											path = "Assets\\Cloud.png"
											weatherConditionList.Add("Overcast")
															
									else:
										if Convert.ToInt32(id) == 900:
											path = "Assets\\Tornado.png"
											weatherConditionList.Add("Tornado")

										elif Convert.ToInt32(id) == 905:
											path = "Assets\\Wind.png"
											weatherConditionList.Add("Windy")

										elif Convert.ToInt32(id) == 906:
											path = "Assets\\Cloud-Hail.png"
											weatherConditionList.Add("Hail")

									if path is not None and weatherPathHashSet.Contains(path) == False:
										fs = None

										try:
											fs = FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
											ms = MemoryStream()
											buffer = Array.CreateInstance(Byte, fs.Length)
											bytesRead = fs.Read(buffer, 0, buffer.Length)

											while bytesRead > 0:
												ms.Write(buffer, 0, bytesRead)
												bytesRead = fs.Read(buffer, 0, buffer.Length)

											ms.Seek(0, SeekOrigin.Begin)
											weatherStreamList.Add(ms)

										finally:
											if fs is not None:
												fs.Close()

										weatherPathHashSet.Add(path)

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

		return KeyValuePair[List[String], KeyValuePair[Double, KeyValuePair[KeyValuePair[Double, Double], KeyValuePair[List[Double], List[MemoryStream]]]]](weatherConditionList, KeyValuePair[Double, KeyValuePair[KeyValuePair[Double, Double], KeyValuePair[List[Double], List[MemoryStream]]]](temp, KeyValuePair[KeyValuePair[Double, Double], KeyValuePair[List[Double], List[MemoryStream]]](KeyValuePair[Double, Double](windSpeed, windDeg), KeyValuePair[List[Double], List[MemoryStream]](weatherIdList, weatherStreamList))))

	def onCompleted(task):
		global idList

		if task.Result.Key.Count > 0:
			sequenceList = List[Sequence]()

			for sequence in Script.Instance.Sequences:
				if sequence.Name.Equals("Weather"):
					sequenceList.Add(sequence)

			for s in task.Result.Key:
				Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, s))

		if Application.Current.MainWindow.IsVisible and task.Result.Value.Value.Value.Value.Count > 0 and not Enumerable.SequenceEqual[Double](idList, task.Result.Value.Value.Value.Key):
			width = 128
			height = 128
			max = 4
			window = Window()
			contentControl = ContentControl()
			grid = Grid()
			storyboard = Storyboard()

			def onCurrentStateInvalidated(sender, args):
				if sender.CurrentState == ClockState.Filling:
					window.Close()

			storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
		
			def onLoaded(sender, args):
				time = 0
				speed = task.Result.Value.Value.Key.Key * 1000 / 60 / 60

				contentControl.Width = contentControl.ActualWidth * 1.5 if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight * 1.5 
				contentControl.Height = contentControl.ActualWidth * 1.5 if contentControl.ActualWidth > contentControl.ActualHeight else contentControl.ActualHeight * 1.5
				contentControl.RenderTransform.CenterX = contentControl.Width / 2
				contentControl.RenderTransform.CenterY = contentControl.Height / 2

				doubleAnimation1 = DoubleAnimation(contentControl.Opacity, 1, TimeSpan.FromMilliseconds(500))
				doubleAnimation2 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
				doubleAnimation3 = DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500))
				doubleAnimation4 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
				doubleAnimation5 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
				doubleAnimation6 = DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(500))
				sineEase1 = SineEase()
				sineEase2 = SineEase()

				sineEase1.EasingMode = EasingMode.EaseOut
				sineEase2.EasingMode = EasingMode.EaseIn
				doubleAnimation1.EasingFunction = doubleAnimation2.EasingFunction = doubleAnimation3.EasingFunction = sineEase1
				doubleAnimation4.EasingFunction = doubleAnimation5.EasingFunction = doubleAnimation6.EasingFunction = sineEase2

				doubleAnimation4.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds((250 * (max - 1) * 2 + 1000 + 3000) * task.Result.Value.Value.Value.Value.Count - 500))
				doubleAnimation5.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds((250 * (max - 1) * 2 + 1000 + 3000) * task.Result.Value.Value.Value.Value.Count - 500))
				doubleAnimation6.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds((250 * (max - 1) * 2 + 1000 + 3000) * task.Result.Value.Value.Value.Value.Count - 500))

				storyboard.Children.Add(doubleAnimation1)
				storyboard.Children.Add(doubleAnimation2)
				storyboard.Children.Add(doubleAnimation3)
				storyboard.Children.Add(doubleAnimation4)
				storyboard.Children.Add(doubleAnimation5)
				storyboard.Children.Add(doubleAnimation6)

				Storyboard.SetTarget(doubleAnimation1, contentControl)
				Storyboard.SetTarget(doubleAnimation2, contentControl)
				Storyboard.SetTarget(doubleAnimation3, contentControl)
				Storyboard.SetTarget(doubleAnimation4, contentControl)
				Storyboard.SetTarget(doubleAnimation5, contentControl)
				Storyboard.SetTarget(doubleAnimation6, contentControl)
				Storyboard.SetTargetProperty(doubleAnimation1, PropertyPath(ContentControl.OpacityProperty))
				Storyboard.SetTargetProperty(doubleAnimation2, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
				Storyboard.SetTargetProperty(doubleAnimation3, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))
				Storyboard.SetTargetProperty(doubleAnimation4, PropertyPath(ContentControl.OpacityProperty))
				Storyboard.SetTargetProperty(doubleAnimation5, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleXProperty))
				Storyboard.SetTargetProperty(doubleAnimation6, PropertyPath("(0).(1)", ContentControl.RenderTransformProperty, ScaleTransform.ScaleYProperty))

				for element1 in grid.Children:
					for element2 in element1.Children:
						w = element2.Width / 2 if speed > 15 else element2.Width / 2 * speed / 15;
						da1 = DoubleAnimation(element2.Opacity, 1, TimeSpan.FromMilliseconds(1000))
						da2 = DoubleAnimation(-w if Convert.ToInt32(task.Result.Value.Value.Key.Value / 180) % 2 == 0 else w, 0, TimeSpan.FromMilliseconds(1000))
						da3 = DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(1000))
						da4 = DoubleAnimation(0, w if Convert.ToInt32(task.Result.Value.Value.Key.Value / 180) % 2 == 0 else -w, TimeSpan.FromMilliseconds(1000))
						se1 = SineEase()
						se2 = SineEase()

						da1.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(time + 250 * element2.Tag))
						da2.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(time + 250 * element2.Tag))
						da3.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(time + 250 * element2.Tag * 2 + 250 * (max - 1) - 250 * element2.Tag + 3000))
						da4.BeginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(time + 250 * element2.Tag * 2 + 250 * (max - 1) - 250 * element2.Tag + 3000))

						se1.EasingMode = EasingMode.EaseOut
						se2.EasingMode = EasingMode.EaseIn
						da1.EasingFunction = da2.EasingFunction = se1
						da3.EasingFunction = da4.EasingFunction = se2

						storyboard.Children.Add(da1)
						storyboard.Children.Add(da2)
						storyboard.Children.Add(da3)
						storyboard.Children.Add(da4)

						Storyboard.SetTarget(da1, element2)
						Storyboard.SetTarget(da2, element2)
						Storyboard.SetTarget(da3, element2)
						Storyboard.SetTarget(da4, element2)
						Storyboard.SetTargetProperty(da1, PropertyPath(Rectangle.OpacityProperty))
						Storyboard.SetTargetProperty(da2, PropertyPath("(0).(1)", Rectangle.RenderTransformProperty, TranslateTransform.XProperty))
						Storyboard.SetTargetProperty(da3, PropertyPath(Rectangle.OpacityProperty))
						Storyboard.SetTargetProperty(da4, PropertyPath("(0).(1)", Rectangle.RenderTransformProperty, TranslateTransform.XProperty))

					time += 250 * (max - 1) + 1000 + 3000

				storyboard.Begin()
			
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

			backgroundBrush = ImageBrush(DrawingImage(dg))
			backgroundBrush.TileMode = TileMode.Tile
			backgroundBrush.ViewportUnits = BrushMappingMode.Absolute
			backgroundBrush.Viewport = Rect(0, 0, bi.Width, bi.Height)
			backgroundBrush.Stretch = Stretch.None

			if backgroundBrush.CanFreeze:
				backgroundBrush.Freeze()

			grid.HorizontalAlignment = HorizontalAlignment.Stretch
			grid.VerticalAlignment = VerticalAlignment.Stretch
			grid.Background = backgroundBrush
			grid.Width = 150
			grid.Height = 150
			grid.Clip = EllipseGeometry(Rect(0, 0, 150, 150))

			dropShadowEffect = DropShadowEffect()
			dropShadowEffect.BlurRadius = 1
			dropShadowEffect.Color = Colors.Black
			dropShadowEffect.Direction = 270
			dropShadowEffect.Opacity = 0.5
			dropShadowEffect.ShadowDepth = 1

			if dropShadowEffect.CanFreeze:
				dropShadowEffect.Freeze()

			grid.Effect = dropShadowEffect

			contentControl.Content = grid

			solidColorBrush = SolidColorBrush(colorFromAhsb(Byte.MaxValue, 60, 1.0, 1.0) if task.Result.Value.Key < 0 else colorFromAhsb(Byte.MaxValue, 0, 1.0, 0.4) if task.Result.Value.Key > 37 else colorFromAhsb(Byte.MaxValue, 60 - 60 * task.Result.Value.Key / 37, 1.0, 0.4 + 0.6 * Math.Pow(Math.E, (37 / 5 - task.Result.Value.Key) - 37 / 5) if task.Result.Value.Key < 37 / 5 else 0.4))

			if solidColorBrush.CanFreeze:
				solidColorBrush.Freeze()


			for stream in task.Result.Value.Value.Value.Value:
				try:
					bi = BitmapImage()
					bi.BeginInit()
					bi.StreamSource = stream
					bi.CacheOption = BitmapCacheOption.OnLoad
					bi.CreateOptions = BitmapCreateOptions.None
					bi.EndInit()

				finally:
					stream.Close()

				imageBrush = ImageBrush(bi)
				imageBrush.TileMode = TileMode.None
				imageBrush.ViewportUnits = BrushMappingMode.Absolute
				imageBrush.Viewport = Rect(0, 0, width, height)
				imageBrush.Stretch = Stretch.Uniform

				if imageBrush.CanFreeze:
					imageBrush.Freeze()

				g = Grid()
				g.HorizontalAlignment = HorizontalAlignment.Center
				g.VerticalAlignment = VerticalAlignment.Center
				g.Background = Brushes.Transparent
				g.Width = width
				g.Height = height

				grid.Children.Add(g)

				for i in range(max):
					rectangle = Rectangle()
					rectangle.HorizontalAlignment = HorizontalAlignment.Left
					rectangle.VerticalAlignment = VerticalAlignment.Top
					rectangle.Width = width
					rectangle.Height = height
					rectangle.Fill = solidColorBrush
					rectangle.Opacity = 0
					rectangle.OpacityMask = imageBrush
					rectangle.Clip = RectangleGeometry(Rect(0, height / max * i, width, height / max))
					rectangle.RenderTransform = TranslateTransform(0, 0)
					rectangle.Tag = i
					
					g.Children.Add(rectangle)

			window.Show()

			idList.Clear()
			idList.AddRange(task.Result.Value.Value.Value.Key)
	
	Task.Factory.StartNew[KeyValuePair[List[String], KeyValuePair[Double, KeyValuePair[KeyValuePair[Double, Double], KeyValuePair[List[Double], List[MemoryStream]]]]]](onUpdate, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[KeyValuePair[List[String], KeyValuePair[Double, KeyValuePair[KeyValuePair[Double, Double], KeyValuePair[List[Double], List[MemoryStream]]]]]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(5)
	timer.Start()

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

def colorFromAhsb(a, h, s, b):
	if 0 > a or 255 < a:
		return Colors.Transparent

	if 0 > h or 360 < h:
		return Colors.Transparent

	if 0 > s or 1 < s:
		return Colors.Transparent

	if 0 > b or 1 < b:
		return Colors.Transparent;

	if 0 == s:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(b * Byte.MaxValue), Convert.ToByte(b * Byte.MaxValue), Convert.ToByte(b * Byte.MaxValue))
		
	fMax = b - (b * s) + s if 0.5 < b else b + (b * s)
	fMin = b + (b * s) - s if 0.5 < b else b - (b * s)
	iSextant = Convert.ToInt32(Math.Floor(h / 60.0))

	if 300 <= h:
		h -= 360

	h = h / 60.0
	h -= 2 * Convert.ToSingle(Math.Floor(((iSextant + 1) % 6.0) / 2))

	fMid = h * (fMax - fMin) + fMin if 0 == iSextant % 2 else fMin - h * (fMax - fMin)
	iMax = Convert.ToInt32(fMax * Byte.MaxValue)
	iMid = Convert.ToInt32(fMid * Byte.MaxValue)
	iMin = Convert.ToInt32(fMin * Byte.MaxValue)
	
	if iSextant == 1:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMid), Convert.ToByte(iMax), Convert.ToByte(iMin))

	elif iSextant == 2:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMin), Convert.ToByte(iMax), Convert.ToByte(iMid))

	elif iSextant == 3:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMin), Convert.ToByte(iMid), Convert.ToByte(iMax))

	elif iSextant == 4:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMid), Convert.ToByte(iMin), Convert.ToByte(iMax))

	elif iSextant == 5:
		return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMax), Convert.ToByte(iMin), Convert.ToByte(iMid))

	return Color.FromArgb(Convert.ToByte(a), Convert.ToByte(iMax), Convert.ToByte(iMid), Convert.ToByte(iMin))

def onStart(s, e):
	global timer

	timer.Start()

def onStop(s, e):
	global timer
	
	timer.Stop()

idList = List[Double]()
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMilliseconds(30000)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop