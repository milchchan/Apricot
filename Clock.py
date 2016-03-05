# -*- coding: utf-8 -*-
# Clock.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Nullable, Byte, Int32, Double, String, DateTime, TimeSpan, Array, Environment, Math, Random, Action
from System.IO import FileStream, MemoryStream, SeekOrigin, FileMode, FileAccess, FileShare
from System.Collections.Generic import List, KeyValuePair
from System.Globalization import CultureInfo
from System.Threading.Tasks import Task, TaskCreationOptions, TaskScheduler
from System.Windows import Application, Window, WindowStartupLocation, WindowStyle, ResizeMode, SizeToContent, UIElement, HorizontalAlignment, VerticalAlignment, Point, Rect, Thickness, PropertyPath, GridLength, GridUnitType
from System.Windows.Controls import ContentControl, Grid, Border, Canvas, Image, ColumnDefinition, RowDefinition
from System.Windows.Input import Keyboard, ModifierKeys, MouseButtonEventArgs, MouseButton, MouseButtonState
from System.Windows.Media import Brushes, TransformGroup, TranslateTransform, ScaleTransform, Stretch
from System.Windows.Media.Animation import Storyboard, HandoffBehavior, Clock, ClockState, DoubleAnimation, SineEase, EasingMode
from System.Windows.Media.Imaging import BitmapImage, BitmapCacheOption, BitmapCreateOptions
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Script

def onMouseUp(sender, args):
	if args.ChangedButton == MouseButton.Middle and Keyboard.Modifiers == ModifierKeys.Alt == ModifierKeys.Alt or args.ChangedButton == MouseButton.Left and Keyboard.Modifiers == ModifierKeys.Alt:
		def onLoad():
			streamList1 = List[MemoryStream]()
			streamList2 = List[MemoryStream]()

			for i in range(10):
				ms = None
				fs = None
			
				try:
					fs = FileStream(String.Format("Assets\\Number-{0}.png", i.ToString(CultureInfo.InvariantCulture)), FileMode.Open, FileAccess.Read, FileShare.Read)
					ms = MemoryStream()
					buffer = Array.CreateInstance(Byte, fs.Length)
					bytesRead = fs.Read(buffer, 0, buffer.Length)

					while bytesRead > 0:
						ms.Write(buffer, 0, bytesRead)
						bytesRead = fs.Read(buffer, 0, buffer.Length)

					ms.Seek(0, SeekOrigin.Begin)

					streamList1.Add(ms)

				except:
					if ms is not None:
						ms.Close()
						ms = None

				finally:
					if fs is not None:
						fs.Close()

			for path in ["Assets\\Hour.png", "Assets\\Minute.png", "Assets\\Second.png"]:
				ms = None
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

					streamList2.Add(ms)

				except:
					if ms is not None:
						ms.Close()
						ms = None

				finally:
					if fs is not None:
						fs.Close()

			return KeyValuePair[List[MemoryStream], List[MemoryStream]](streamList1, streamList2)

		def onCompleted(task):
			global maxWidth, maxHeight
			
			window = Window()
			contentControl = ContentControl()
			grid1 = Grid()
			tickTimer = DispatcherTimer(DispatcherPriority.Normal)
			closeTimer = DispatcherTimer(DispatcherPriority.Background)
		
			def onLoaded(sender, args):
				global rectList, digits

				storyboard = Storyboard()

				def onCurrentStateInvalidated(sender, args):
					if sender.CurrentState == ClockState.Filling:
						for element in grid1.Children:
							element.Opacity = 1

						storyboard.Remove(contentControl)

						if not grid1.Tag:
							closeTimer.Start()

				storyboard.CurrentStateInvalidated += onCurrentStateInvalidated

				r = Random(Environment.TickCount)
				dateTime = DateTime.Now

				digits[0] = dateTime.Hour / 10
				digits[1] = dateTime.Hour % 10
				digits[2] = dateTime.Minute / 10
				digits[3] = dateTime.Minute % 10
				digits[4] = dateTime.Second / 10
				digits[5] = dateTime.Second % 10

				for i in range(digits.Length):
					beginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(500)))

					for element1 in grid1.Children:
						if Grid.GetColumn(element1) == i:
							doubleAnimation = DoubleAnimation(element1.Opacity, 1, TimeSpan.FromMilliseconds(500))
							doubleAnimation.BeginTime = beginTime
							sineEase = SineEase()

							sineEase.EasingMode = EasingMode.EaseOut
							doubleAnimation.EasingFunction = sineEase

							storyboard.Children.Add(doubleAnimation)
								
							Storyboard.SetTarget(doubleAnimation, element1)
							Storyboard.SetTargetProperty(doubleAnimation, PropertyPath(UIElement.OpacityProperty))

							if Grid.GetRow(element1) == 0:
								scale1 = Math.Max(element1.ActualWidth / maxWidth, element1.ActualHeight / maxHeight)

								if rectList[digits[Grid.GetColumn(element1)]].Width > maxWidth and rectList[digits[Grid.GetColumn(element1)]].Height > maxHeight:
									translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digits[Grid.GetColumn(element1)]].Width - maxWidth) / 2.0))
									translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digits[Grid.GetColumn(element1)]].Height - maxHeight) / 2.0))

								elif rectList[digits[Grid.GetColumn(element1)]].Width > maxWidth:
									translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digits[Grid.GetColumn(element1)]].Width - maxWidth) / 2.0))
									translateY = Math.Round(-rectList[digits[Grid.GetColumn(element1)]].Y)

								elif rectList[digits[Grid.GetColumn(element1)]].Height > maxHeight:
									translateX = Math.Round(-rectList[digits[Grid.GetColumn(element1)]].X)
									translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digits[Grid.GetColumn(element1)]].Height - maxHeight) / 2.0))

								else:
									translateX = Math.Round(-rectList[digits[Grid.GetColumn(element1)]].X)
									translateY = Math.Round(-rectList[digits[Grid.GetColumn(element1)]].Y)

								scale2 = Math.Max(maxWidth / rectList[digits[Grid.GetColumn(element1)]].Width, maxHeight / rectList[digits[Grid.GetColumn(element1)]].Height)

								if scale2 > 1:
									scale2 = 1

								for element2 in element1.Child.Children:
									transformGroup1 = TransformGroup()
									transformGroup1.Children.Add(TranslateTransform((element2.ActualWidth - maxWidth) / 2, (element2.ActualHeight - maxHeight) / 2))
									transformGroup1.Children.Add(ScaleTransform(scale1, scale1, element2.ActualWidth / 2, element2.ActualHeight / 2))
				
									element2.RenderTransform = transformGroup1
									
									for element3 in element2.Children:
										transformGroup2 = TransformGroup()
										transformGroup2.Children.Add(TranslateTransform(translateX, translateY))
										transformGroup2.Children.Add(ScaleTransform(scale2, scale2, maxWidth / 2, maxHeight / 2))
				
										element3.RenderTransform = transformGroup2

				contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

				tickTimer.Start()

			def onWindowMouseEnter(sender, args):
				closeTimer.Stop()
				grid1.Tag = True

			def onWindowMouseLeave(sender, args):
				if closeTimer.Tag:
					closeTimer.Start()

				grid1.Tag = False

			def onTick(sender, args):
				global rectList, digits

				if rectList.Count > 0:
					dateTime = DateTime.Now

					for element1 in grid1.Children:
						if Grid.GetRow(element1) == 0:
							if Grid.GetColumn(element1) == 0:
								digit = dateTime.Hour / 10
							elif Grid.GetColumn(element1) == 1:
								digit = dateTime.Hour % 10
							elif Grid.GetColumn(element1) == 2:
								digit = dateTime.Minute / 10
							elif Grid.GetColumn(element1) == 3:
								digit = dateTime.Minute % 10
							elif Grid.GetColumn(element1) == 4:
								digit = dateTime.Second / 10
							else:
								digit = dateTime.Second % 10

							if digit != digits[Grid.GetColumn(element1)]:
								for element2 in element1.Child.Children:
									for element3 in element2.Children:
										storyboard = Storyboard()

										for transform in element3.RenderTransform.Children:
											if clr.GetClrType(TranslateTransform).IsInstanceOfType(transform):
												if rectList[digit].Width > maxWidth and rectList[digit].Height > maxHeight:
													translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digit].X + (rectList[digit].Width - maxWidth) / 2.0 - rectList[digits[Grid.GetColumn(element1)]].X)))
													translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digit].Y + (rectList[digit].Height - maxHeight) / 2.0 - rectList[digits[Grid.GetColumn(element1)]].Y)))

												elif rectList[digit].Width > maxWidth:
													translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digit].X + (rectList[digit].Width - maxWidth) / 2.0 - rectList[digits[Grid.GetColumn(element1)]].X)))
													translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digit].Y - rectList[digits[Grid.GetColumn(element1)]].Y)))

												elif rectList[digit].Height > maxHeight:
													translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digit].X - rectList[digits[Grid.GetColumn(element1)]].X)))
													translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digit].Y + (rectList[digit].Height - maxHeight) / 2.0 - rectList[digits[Grid.GetColumn(element1)]].Y)))

												else:
													translateX = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].X + (rectList[digit].X - rectList[digits[Grid.GetColumn(element1)]].X)))
													translateY = Math.Round(-(rectList[digits[Grid.GetColumn(element1)]].Y + (rectList[digit].Y - rectList[digits[Grid.GetColumn(element1)]].Y)))

												doubleAnimation1 = DoubleAnimation(translateX, TimeSpan.FromMilliseconds(500))
												doubleAnimation2 = DoubleAnimation(translateY, TimeSpan.FromMilliseconds(500))
												sineEase = SineEase()

												sineEase.EasingMode = EasingMode.EaseInOut
												doubleAnimation1.EasingFunction = sineEase
												doubleAnimation2.EasingFunction = sineEase

												storyboard.Children.Add(doubleAnimation1)
												storyboard.Children.Add(doubleAnimation2)
								
												Storyboard.SetTarget(doubleAnimation1, element3)
												Storyboard.SetTarget(doubleAnimation2, element3)
												Storyboard.SetTargetProperty(doubleAnimation1, PropertyPath("(0).(1)[0].(2)", Canvas.RenderTransformProperty, TransformGroup.ChildrenProperty, TranslateTransform.XProperty))
												Storyboard.SetTargetProperty(doubleAnimation2, PropertyPath("(0).(1)[0].(2)", Canvas.RenderTransformProperty, TransformGroup.ChildrenProperty, TranslateTransform.YProperty))

											else:
												scale1 = Math.Max(maxWidth / rectList[digits[Grid.GetColumn(element1)]].Width, maxHeight / rectList[digits[Grid.GetColumn(element1)]].Height)
												scale2 = Math.Max(maxWidth / rectList[digit].Width, maxHeight / rectList[digit].Height)

												if scale1 > 1:
													scale1 = 1

												if scale2 > 1:
													scale2 = 1

												transform.ScaleX = transform.ScaleY = scale1 + (scale2 - scale1)

												doubleAnimation1 = DoubleAnimation(scale1 + (scale2 - scale1), TimeSpan.FromMilliseconds(500))
												doubleAnimation2 = DoubleAnimation(scale1 + (scale2 - scale1), TimeSpan.FromMilliseconds(500))
												sineEase = SineEase()

												sineEase.EasingMode = EasingMode.EaseInOut
												doubleAnimation1.EasingFunction = sineEase
												doubleAnimation2.EasingFunction = sineEase

												storyboard.Children.Add(doubleAnimation1)
												storyboard.Children.Add(doubleAnimation2)
								
												Storyboard.SetTarget(doubleAnimation1, element3)
												Storyboard.SetTarget(doubleAnimation2, element3)
												Storyboard.SetTargetProperty(doubleAnimation1, PropertyPath("(0).(1)[1].(2)", Canvas.RenderTransformProperty, TransformGroup.ChildrenProperty, ScaleTransform.ScaleXProperty))
												Storyboard.SetTargetProperty(doubleAnimation2, PropertyPath("(0).(1)[1].(2)", Canvas.RenderTransformProperty, TransformGroup.ChildrenProperty, ScaleTransform.ScaleYProperty))

										element3.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace)

								digits[Grid.GetColumn(element1)] = digit

			def onClose(sender, args):
				global digits

				closeTimer.Stop()

				storyboard = Storyboard()

				def onCurrentStateInvalidated(sender, args):
					if sender.CurrentState == ClockState.Filling:
						for element in grid1.Children:
							element.Opacity = 0

						storyboard.Remove(contentControl)
						tickTimer.Stop()
						window.Close()

				storyboard.CurrentStateInvalidated += onCurrentStateInvalidated

				r = Random(Environment.TickCount)

				for i in range(digits.Length):
					beginTime = Nullable[TimeSpan](TimeSpan.FromMilliseconds(r.Next(500)))

					for element in grid1.Children:
						if Grid.GetColumn(element) == i:
							doubleAnimation = DoubleAnimation(element.Opacity, 0, TimeSpan.FromMilliseconds(500))
							doubleAnimation.BeginTime = beginTime
							sineEase = SineEase()

							sineEase.EasingMode = EasingMode.EaseIn
							doubleAnimation.EasingFunction = sineEase

							storyboard.Children.Add(doubleAnimation)

							Storyboard.SetTarget(doubleAnimation, element)
							Storyboard.SetTargetProperty(doubleAnimation, PropertyPath(UIElement.OpacityProperty))

				contentControl.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)
				closeTimer.Tag = False

			tickTimer.Tick += onTick
			tickTimer.Interval = TimeSpan.FromMilliseconds(100)

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
			window.Loaded += onLoaded
			window.MouseEnter += onWindowMouseEnter
			window.MouseLeave += onWindowMouseLeave

			contentControl.UseLayoutRounding = True
			contentControl.HorizontalAlignment = HorizontalAlignment.Stretch
			contentControl.VerticalAlignment = VerticalAlignment.Stretch

			window.Content = contentControl

			grid1.HorizontalAlignment = HorizontalAlignment.Center
			grid1.VerticalAlignment = VerticalAlignment.Center
			grid1.Background = Brushes.Transparent
			grid1.Tag = False

			contentControl.Content = grid1

			bitmapImageList1 = List[BitmapImage]()
			bitmapImageList2 = List[BitmapImage]()
			width = 0
			height = 0

			for stream in task.Result.Key:
				try:
					bitmapImage = BitmapImage()
					bitmapImage.BeginInit()
					bitmapImage.StreamSource = stream
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad
					bitmapImage.CreateOptions = BitmapCreateOptions.None
					bitmapImage.EndInit()
					
					width += bitmapImage.PixelWidth

					if bitmapImage.PixelHeight > height:
						height = bitmapImage.PixelHeight

					bitmapImageList1.Add(bitmapImage)

				finally:
					stream.Close()

			for stream in task.Result.Value:
				try:
					bitmapImage = BitmapImage()
					bitmapImage.BeginInit()
					bitmapImage.StreamSource = stream
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad
					bitmapImage.CreateOptions = BitmapCreateOptions.None
					bitmapImage.EndInit()
					
					bitmapImageList2.Add(bitmapImage)

				finally:
					stream.Close()

			x = 0
			kvpList = List[KeyValuePair[Point, BitmapImage]]()

			for bitmapImage in bitmapImageList1:
				rect = Rect(x, (height - bitmapImage.PixelHeight) / 2, bitmapImage.PixelWidth, bitmapImage.PixelHeight)

				rectList.Add(rect)
				kvpList.Add(KeyValuePair[Point, BitmapImage](rect.Location, bitmapImage))

				x += bitmapImage.PixelWidth

			rowDefinition1 = RowDefinition()
			rowDefinition1.Height = GridLength(1, GridUnitType.Auto)

			grid1.RowDefinitions.Add(rowDefinition1)

			for i in range(digits.Length):
				columnDefinition = ColumnDefinition()
				columnDefinition.Width = GridLength(1, GridUnitType.Star)

				grid1.ColumnDefinitions.Add(columnDefinition)

				border = Border()
				border.HorizontalAlignment = HorizontalAlignment.Stretch
				border.VerticalAlignment = VerticalAlignment.Stretch
				border.Margin = Thickness(4)
				border.BorderBrush = Brushes.Black
				border.BorderThickness = Thickness(1)
				border.Padding = Thickness(0)
				border.Width = 160
				border.Height = 480
				border.Background = Brushes.White
				border.Opacity = 0

				grid1.Children.Add(border)
			
				Grid.SetColumn(border, i);
				Grid.SetRow(border, 0)

				grid2 = Grid()
				grid2.HorizontalAlignment = HorizontalAlignment.Stretch
				grid2.VerticalAlignment = VerticalAlignment.Stretch
				grid2.Background = Brushes.Transparent
				grid2.ClipToBounds = True

				border.Child = grid2

				grid3 = Grid()
				grid3.HorizontalAlignment = HorizontalAlignment.Left
				grid3.VerticalAlignment = VerticalAlignment.Top
				grid3.Width = 160
				grid3.Height = 480
				grid3.Background = Brushes.Transparent

				grid2.Children.Add(grid3)

				canvas = Canvas()
				canvas.HorizontalAlignment = HorizontalAlignment.Left
				canvas.VerticalAlignment = VerticalAlignment.Top
				canvas.Width = width
				canvas.Height = maxHeight
				canvas.Background = Brushes.Transparent

				grid3.Children.Add(canvas)

				for kvp in kvpList:
					image = Image()
					image.HorizontalAlignment = HorizontalAlignment.Left
					image.VerticalAlignment = VerticalAlignment.Top
					image.Source = kvp.Value
					image.Width = kvp.Value.PixelWidth
					image.Height = kvp.Value.PixelHeight
					image.Stretch = Stretch.Fill
					
					canvas.Children.Add(image)

					Canvas.SetLeft(image, kvp.Key.X)
					Canvas.SetTop(image, kvp.Key.Y)
			
			column = 1

			rowDefinition2 = RowDefinition()
			rowDefinition2.Height = GridLength(1, GridUnitType.Auto)

			grid1.RowDefinitions.Add(rowDefinition2)

			for bitmapImage in bitmapImageList2:
				image = Image()
				image.HorizontalAlignment = HorizontalAlignment.Right
				image.VerticalAlignment = VerticalAlignment.Top
				image.Margin = Thickness(0, 0, 8, 0)
				image.Source = bitmapImage
				image.Width = bitmapImage.PixelWidth / 2
				image.Height = bitmapImage.PixelHeight / 2
				image.Stretch = Stretch.Fill

				grid1.Children.Add(image)
			
				Grid.SetColumn(image, column);
				Grid.SetRow(image, 1);

				column += 2

			window.Show()

		Task.Factory.StartNew[KeyValuePair[List[MemoryStream], List[MemoryStream]]](onLoad, TaskCreationOptions.LongRunning).ContinueWith(Action[Task[KeyValuePair[List[MemoryStream], List[MemoryStream]]]](onCompleted), TaskScheduler.FromCurrentSynchronizationContext())

def onStart(sender, args):
	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow:
			window.MouseUp += onMouseUp

def onStop(sender, args):
	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow:
			window.MouseUp -= onMouseUp

rectList = List[Rect]()
maxWidth = 120
maxHeight = 320
digits = Array.CreateInstance(Int32, 6)
digits[0] = digits[1] = digits[2] = digits[3] = digits[4] = digits[5] = 0
Script.Instance.Start += onStart
Script.Instance.Stop += onStop