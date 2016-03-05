# -*- coding: utf-8 -*-
# Gmail.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Byte, Char, String, Uri, DateTime, TimeSpan, StringComparison, Environment, Math
from System.IO import Stream, FileStream, StreamReader, StreamWriter, Path, Directory, File, FileMode, FileAccess, FileShare
from System.Collections.Generic import List, Dictionary
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Diagnostics import Trace
from System.Globalization import CultureInfo, DateTimeStyles
from System.Reflection import Assembly
from System.Text import StringBuilder, Encoding, UTF8Encoding
from System.Text.RegularExpressions import Regex, Match, Capture, RegexOptions
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Net import NetworkCredential, WebRequest, WebResponse
from System.Net.NetworkInformation import NetworkInterface
from System.Windows import Application, Window, WindowStartupLocation, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Point, Rect, Thickness, SystemColors
from System.Windows.Controls import MenuItem, Separator, StackPanel, Border, Label, TextBox, PasswordBox, Button, Orientation
from System.Windows.Media import Color, ColorConverter, Colors, SolidColorBrush, LinearGradientBrush, GradientStopCollection, GradientStop, ImageBrush, TileMode, BrushMappingMode, Stretch, RenderOptions, ClearTypeHint
from System.Windows.Media.Effects import DropShadowEffect
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System.Xml import XmlDocument
from Apricot import Script, Entry, Word, Sequence

username = ""
password = ""

def update():
	global username, password

	request = WebRequest.Create("https://mail.google.com/mail/feed/atom")
	request.Credentials = NetworkCredential(username, password)
	
	entryList = List[Entry]()

	def onUpdate():
		if NetworkInterface.GetIsNetworkAvailable():
			try:
				response = None
				stream = None
				
				try:
					response = request.GetResponse()
					stream = response.GetResponseStream()
					doc = XmlDocument()
					doc.Load(stream)
				
					for entryXmlNode in doc.GetElementsByTagName("entry"):
						entry = Entry()
			
						for xmlNode in entryXmlNode.ChildNodes:
							if xmlNode.Name.Equals("title"):
								entry.Title = xmlNode.InnerText
							elif xmlNode.Name.Equals("issued"):
								entry.Created = DateTime.Parse(xmlNode.InnerText)
							elif xmlNode.Name.Equals("modified"):
								entry.Modified = DateTime.Parse(xmlNode.InnerText)
							elif xmlNode.Name.Equals("link"):
								for attribute in xmlNode.Attributes:
									if attribute.Name.Equals("href"):
										entry.Resource = Uri(attribute.Value)
							elif xmlNode.Name.Equals("author"):
								for childXmlNode in xmlNode.ChildNodes:
									if childXmlNode.Name.Equals("name"):
										entry.Author = childXmlNode.InnerText
			
						entry.Image = Uri("http://www.google.co.jp/options/icons/gmail.gif")
						entryList.Add(entry)

				finally:			
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
				if entry.Modified > dateTime:
					newEntryList.Add(entry)

				if entry.Modified > dt:
					dt = entry.Modified

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

	Task.Factory.StartNew(onUpdate, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

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

def onTick(timer, e):
	global username, password

	if not String.IsNullOrEmpty(username) and not String.IsNullOrEmpty(password):
		update()

	timer.Stop()
	timer.Interval = TimeSpan.FromMinutes(5)
	timer.Start()

def onOpened(s, e):
	global menuItem

	menuItem.Items.Clear()

	def onSignInClick(source, rea):
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
			fileName = config.AppSettings.Settings["BackgroundImage"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["BackgroundImage"].Value);
			fs = None
			bi = BitmapImage()

			try:
				fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)

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


			
		


		def onClick(source, args):
			global username, password

			if not String.IsNullOrEmpty(usernameTextBox.Text) and not String.IsNullOrEmpty(passwordBox.Password):
				username = usernameTextBox.Text
				password = passwordBox.Password

				def onSave():
					try:
						fs = None
						sr = None
						sw = None

						try:
							fs = FileStream(__file__, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
							encoding = UTF8Encoding(False)
							sr = StreamReader(fs, encoding, True)
							lines = Regex.Replace(Regex.Replace(sr.ReadToEnd(), "username\\s*=\\s*\"\"", String.Format("username = \"{0}\"", username), RegexOptions.CultureInvariant), "password\\s*=\\s*\"\"", String.Format("password = \"{0}\"", password), RegexOptions.CultureInvariant)
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

					except Exception, e:
						Trace.WriteLine(e.clsException.Message)
						Trace.WriteLine(e.clsException.StackTrace)

				def onCompleted(task):
					global menuItem
	
					for window in Application.Current.Windows:
						if window is Application.Current.MainWindow and window.ContextMenu is not None:
							if window.ContextMenu.Items.Contains(menuItem):
								window.ContextMenu.Items.Remove(menuItem)
								window.ContextMenu.Opened -= onOpened
								
								if window.ContextMenu.Items[10].GetType().IsInstanceOfType(window.ContextMenu.Items[window.ContextMenu.Items.Count - 4]):
									window.ContextMenu.Items.RemoveAt(10)

					menuItem = None

				Task.Factory.StartNew(onSave, TaskCreationOptions.LongRunning).ContinueWith(onCompleted, TaskScheduler.FromCurrentSynchronizationContext())

			window.Close()
			
		window.Owner = Application.Current.MainWindow
		window.Title = Application.Current.MainWindow.Title
		window.WindowStartupLocation = WindowStartupLocation.CenterScreen
		window.ResizeMode = ResizeMode.NoResize
		window.SizeToContent = SizeToContent.WidthAndHeight
		window.Background = SystemColors.ControlBrush

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
		stackPanel2.Background = SystemColors.ControlBrush if backgroundBrush is None else backgroundBrush
		
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

		dropShadowEffect1 = DropShadowEffect()
		dropShadowEffect1.BlurRadius = 1
		dropShadowEffect1.Color = Colors.Black if Math.Max(Math.Max(textColor.R, textColor.G), textColor.B) > Byte.MaxValue / 2 else Colors.White;
		dropShadowEffect1.Direction = 270
		dropShadowEffect1.Opacity = 0.5
		dropShadowEffect1.ShadowDepth = 1

		if dropShadowEffect1.CanFreeze:
			dropShadowEffect1.Freeze()

		stackPanel5.Effect = dropShadowEffect1

		stackPanel4.Children.Add(stackPanel5)

		usernameLabel = Label()
		usernameLabel.Foreground = textBrush

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			usernameLabel.Content = "ユーザ名"
		else:
			usernameLabel.Content = "Username"

		RenderOptions.SetClearTypeHint(usernameLabel, ClearTypeHint.Enabled)

		stackPanel5.Children.Add(usernameLabel)

		usernameTextBox = TextBox()
		usernameTextBox.Width = 240

		stackPanel4.Children.Add(usernameTextBox)

		dropShadowEffect2 = DropShadowEffect()
		dropShadowEffect2.BlurRadius = 1
		dropShadowEffect2.Color = Colors.Black if Math.Max(Math.Max(textColor.R, textColor.G), textColor.B) > Byte.MaxValue / 2 else Colors.White;
		dropShadowEffect2.Direction = 270
		dropShadowEffect2.Opacity = 0.5
		dropShadowEffect2.ShadowDepth = 1

		if dropShadowEffect2.CanFreeze:
			dropShadowEffect2.Freeze()

		stackPanel6 = StackPanel()
		stackPanel6.HorizontalAlignment = HorizontalAlignment.Stretch
		stackPanel6.VerticalAlignment = VerticalAlignment.Stretch
		stackPanel6.Orientation = Orientation.Vertical
		stackPanel6.Effect = dropShadowEffect2
		
		stackPanel4.Children.Add(stackPanel6)
		
		passwordLabel = Label()
		passwordLabel.Foreground = textBrush

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			passwordLabel.Content = "パスワード"
		else:
			passwordLabel.Content = "Password"

		RenderOptions.SetClearTypeHint(passwordLabel, ClearTypeHint.Enabled)

		stackPanel6.Children.Add(passwordLabel)

		passwordBox = PasswordBox()
		passwordBox.Width = 240
			
		stackPanel4.Children.Add(passwordBox)

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

		signInButton = Button()
		signInButton.HorizontalAlignment = HorizontalAlignment.Right
		signInButton.VerticalAlignment = VerticalAlignment.Center
		signInButton.Margin = Thickness(10, 10, 10, 10)
		signInButton.Padding = Thickness(10, 2, 10, 2)
		signInButton.IsDefault = True

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			signInButton.Content = "サインイン"
		else:
			signInButton.Content = "Sign in"

		signInButton.Click += onClick

		border2.Child = signInButton
		
		usernameTextBox.Focus()
		window.Show()

	signInMenuItem = MenuItem()

	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		signInMenuItem.Header = "サインイン"
	else:
		signInMenuItem.Header = "Sign in"

	signInMenuItem.Click += onSignInClick

	menuItem.Items.Add(signInMenuItem)

def onStart(s, e):
	global timer, menuItem, separator
	
	if String.IsNullOrEmpty(username) or String.IsNullOrEmpty(password):
		if menuItem is None:
			menuItem = MenuItem()
			menuItem.Header = "Gmail"

		for window in Application.Current.Windows:
			if window is Application.Current.MainWindow and window.ContextMenu is not None:
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
menuItem = None
separator = None
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
Script.Instance.Start += onStart
Script.Instance.Stop += onStop