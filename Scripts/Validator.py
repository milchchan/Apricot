# -*- coding: utf-8 -*-
# Validator.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Byte, String, Environment, Math
from System.IO import FileStream, Path, Directory, File, FileMode, FileAccess, FileShare
from System.Collections.Generic import List
from System.Globalization import CultureInfo
from System.Threading.Tasks import Task, TaskCreationOptions, TaskContinuationOptions, TaskScheduler
from System.Windows import Application, Window, WindowStartupLocation, ResizeMode, SizeToContent, HorizontalAlignment, VerticalAlignment, Point, Thickness, SystemColors
from System.Windows.Controls import MenuItem, Separator, StackPanel, Border, Label, Button, Orientation
from System.Windows.Media import Color, Colors, Brushes, SolidColorBrush, LinearGradientBrush, GradientStopCollection, GradientStop, RenderOptions, ClearTypeHint
from System.Windows.Media.Effects import DropShadowEffect
from System.Xml import XmlDocument
from Apricot import Script
from Microsoft.Win32 import OpenFileDialog

def parseSequence(xmlNode, directory, warningList, errorList):
	for childNode1 in xmlNode.ChildNodes:
		if childNode1.Name.Equals("sequence"):
			parseSequence(childNode1, directory, warningList, errorList)

		elif childNode1.Name.Equals("message"):
			if childNode1.InnerText.Length == 0:
				if childNode1.InnerText.Length == 0:
					if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
						warningList.Add("メッセージタグ内のテキストが空です。")
					else:
						warningList.Add("The inner text is empty in message tag.")

		elif childNode1.Name.Equals("motion"):
			for childNode2 in childNode1.ChildNodes:
				if childNode2.Name.Equals("image"):
					if childNode2.InnerText.Length > 0 and not File.Exists(Path.Combine(directory, childNode2.InnerText)):
						if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
							errorList.Add(String.Format("指定されたファイルが見つかりません。 \"{0}\"", childNode2.InnerText))
						else:
							errorList.Add(String.Format("Could not find the specified file. \"{0}\"", childNode2.InnerText))

		elif childNode1.Name.Equals("sound"):
			if childNode1.InnerText.Length > 0 and not File.Exists(Path.Combine(directory, childNode1.InnerText)):
				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					errorList.Add(String.Format("指定されたファイルが見つかりません。 \"{0}\"", childNode1.InnerText))
				else:
					errorList.Add(String.Format("Could not find the specified file. \"{0}\"", childNode1.InnerText))

def attachStackPanel(stackPanel, brush, text):
	stackPanel1 = StackPanel()
	stackPanel1.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel1.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel1.Orientation = Orientation.Vertical
	stackPanel1.Background = brush

	stackPanel.Children.Add(stackPanel1)

	gradientStopCollection = GradientStopCollection()
	gradientStopCollection.Add(GradientStop(Color.FromArgb(0, 0, 0, 0), 0))
	gradientStopCollection.Add(GradientStop(Color.FromArgb(Byte.MaxValue, 0, 0, 0), 1))

	linearGradientBrush = LinearGradientBrush(gradientStopCollection, Point(0.5, 0), Point(0.5, 1))
	linearGradientBrush.Opacity = 0.1

	if linearGradientBrush.CanFreeze:
		linearGradientBrush.Freeze()

	stackPanel2 = StackPanel()
	stackPanel2.HorizontalAlignment = HorizontalAlignment.Stretch
	stackPanel2.VerticalAlignment = VerticalAlignment.Stretch
	stackPanel2.Orientation = Orientation.Vertical
	stackPanel2.Background = linearGradientBrush

	stackPanel1.Children.Add(stackPanel2)
	
	solidColorBrush1 = SolidColorBrush(Colors.White)
	solidColorBrush1.Opacity = 0.5

	if solidColorBrush1.CanFreeze:
		solidColorBrush1.Freeze()

	border1 = Border()
	border1.HorizontalAlignment = HorizontalAlignment.Stretch
	border1.VerticalAlignment = VerticalAlignment.Stretch
	border1.Padding = Thickness(0)
	border1.BorderThickness = Thickness(0, 1, 0, 0)
	border1.BorderBrush = solidColorBrush1
	
	solidColorBrush2 = SolidColorBrush(Colors.Black)
	solidColorBrush2.Opacity = 0.25

	if solidColorBrush2.CanFreeze:
		solidColorBrush2.Freeze()

	stackPanel2.Children.Add(border1)

	border2 = Border()
	border2.HorizontalAlignment = HorizontalAlignment.Stretch
	border2.VerticalAlignment = VerticalAlignment.Stretch
	border2.Padding = Thickness(10, 5, 10, 5)
	border2.BorderThickness = Thickness(0, 0, 0, 1)
	border2.BorderBrush = solidColorBrush2

	border1.Child = border2

	dropShadowEffect = DropShadowEffect()
	dropShadowEffect.BlurRadius = 1
	dropShadowEffect.Color = Colors.Black if Math.Max(Math.Max(SystemColors.ControlTextColor.R, SystemColors.ControlTextColor.G), SystemColors.ControlTextColor.B) > Byte.MaxValue / 2 else Colors.White;
	dropShadowEffect.Direction = 270
	dropShadowEffect.Opacity = 0.5
	dropShadowEffect.ShadowDepth = 1

	if dropShadowEffect.CanFreeze:
		dropShadowEffect.Freeze()

	border3 = Border()
	border3.HorizontalAlignment = HorizontalAlignment.Stretch
	border3.VerticalAlignment = VerticalAlignment.Stretch
	border3.Padding = Thickness(0)
	border3.Effect = dropShadowEffect

	border2.Child = border3

	label = Label()
	label.Foreground = SystemColors.ControlTextBrush
	label.Content = text

	RenderOptions.SetClearTypeHint(label, ClearTypeHint.Enabled)
	
	border3.Child = label
	
def onClick(s, e):
	currentDirectory = Directory.GetCurrentDirectory()

	openFileDialog = OpenFileDialog()
	openFileDialog.Multiselect = False

	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		openFileDialog.Filter = "XMLファイル (*.xml)|*.xml"
	else:
		openFileDialog.Filter = "XML files (*.xml)|*.xml"
	
	if openFileDialog.ShowDialog() == True:
		fileName = openFileDialog.FileName
		warningList = List[String]()
		errorList = List[String]()

		context = TaskScheduler.FromCurrentSynchronizationContext()
		
		def onValidate():
			fs = None

			try:
				fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)

				doc = XmlDocument()
				doc.Load(fs)

				if doc.DocumentElement.Name.Equals("script"):
					for childNode1 in doc.DocumentElement.ChildNodes:
						if childNode1.Name.Equals("character"):
							hasName = False

							for xmlAttribute in childNode1.Attributes:
								if xmlAttribute.Name.Equals("name"):
									hasName = True

							if not hasName:
								if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
									errorList.Add("characterタグにname属性がありません。")
								else:
									errorList.Add("Could not find name attribute in character tag.")

							for childNode2 in childNode1.ChildNodes:
								if childNode2.Name.Equals("sequence"):
									parseSequence(childNode2, Path.GetDirectoryName(fileName), warningList, errorList)

				else:
					if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
						errorList.Add("scriptタグがありません。")
					else:
						errorList.Add("Could not find script tag.")

			except Exception, e:
				errorList.Add(e.clsException.Message)

			finally:
				if fs is not None:
					fs.Close()

		def onCompleted(task):
			window = Window()
			stackPanel3 = StackPanel()
			
			def onLoaded(sender, args):
				stackPanel3.Width = Math.Ceiling(stackPanel3.ActualWidth)

			def onCloseClick(sender, args):
				window.Close()
			
			window.Owner = Application.Current.MainWindow

			if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
				window.Title = "バリデータ"
			else:
				window.Title = "Validator"
	
			window.WindowStartupLocation = WindowStartupLocation.CenterOwner
			window.ResizeMode = ResizeMode.NoResize
			window.SizeToContent = SizeToContent.WidthAndHeight
			window.Background = SystemColors.ControlBrush
			window.Loaded += onLoaded

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
			stackPanel2.Background = SystemColors.WindowBrush
			
			stackPanel1.Children.Add(stackPanel2)

			stackPanel3.HorizontalAlignment = HorizontalAlignment.Stretch
			stackPanel3.VerticalAlignment = VerticalAlignment.Stretch
			stackPanel3.Orientation = Orientation.Vertical

			if errorList.Count == 0:
				if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
					attachStackPanel(stackPanel3, Brushes.Lime, "有効なスクリプトです。")
				else:
					attachStackPanel(stackPanel3, Brushes.Lime, "Valid.")

			for warning in warningList:
				attachStackPanel(stackPanel3, Brushes.Yellow, warning)

			for error in errorList:
				attachStackPanel(stackPanel3, Brushes.Red, error)

			stackPanel2.Children.Add(stackPanel3)
			
			solidColorBrush = SolidColorBrush(Colors.White)
			solidColorBrush.Opacity = 0.5

			if solidColorBrush.CanFreeze:
				solidColorBrush.Freeze()

			border = Border()
			border.HorizontalAlignment = HorizontalAlignment.Stretch
			border.VerticalAlignment = VerticalAlignment.Stretch
			border.BorderThickness = Thickness(0, 1, 0, 0)
			border.BorderBrush = solidColorBrush

			stackPanel1.Children.Add(border)

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

			border.Child = closeButton

			window.Show()

	Task.Factory.StartNew(onValidate).ContinueWith(onCompleted, context)
	Directory.SetCurrentDirectory(currentDirectory)
		
def onStart(s, e):
	global menuItem, separator
	
	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				menuItem.Click += onClick
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)

menuItem = MenuItem()
separator = None

if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
	menuItem.Header = "CHARMLをチェック"
else:
	menuItem.Header = "Validate"

Script.Instance.Start += onStart