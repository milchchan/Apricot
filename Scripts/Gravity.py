# -*- coding: utf-8 -*-
# Gravity.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationCore")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Nullable, Double, TimeSpan, Math
from System.Diagnostics import Process, Trace
from System.Windows import Application, Window, PropertyPath
from System.Windows.Input import Keyboard, ModifierKeys
from System.Windows.Media.Animation import Storyboard, HandoffBehavior, Clock, ClockState, DoubleAnimation, BounceEase, EasingMode
from Apricot import Agent, Script

def onPreviewMouseLeftButtonDown(s, e):
	global y, storyboard 

	if storyboard is not None:
		storyboard.Stop(s)
		storyboard = None

	if (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
		y = Nullable[Double](s.Top)

def onKeyUp(s, e):
	global y

	y = None

def onMouseLeftButtonUp(s, e):
	global y, storyboard

	if y is not None:
		if y > s.Top and storyboard is None:
			storyboard = Storyboard()
			da = DoubleAnimation(s.Top, y, TimeSpan.FromSeconds(Math.Sqrt(2 * ((y - s.Top) * 0.1) / 9.80665)))
			bounceEase = BounceEase()

			bounceEase.EasingMode = EasingMode.EaseOut
			bounceEase.Bounces = 5
			da.EasingFunction = bounceEase

			def onCurrentStateInvalidated(sender, args):
				global storyboard

				if sender.CurrentState == ClockState.Filling:
					s.Top = da.To
					storyboard.Remove(s)
					storyboard = None

			storyboard.CurrentStateInvalidated += onCurrentStateInvalidated
			storyboard.Children.Add(da)

			Storyboard.SetTargetProperty(da, PropertyPath(Window.TopProperty))
			s.BeginStoryboard(storyboard, HandoffBehavior.SnapshotAndReplace, True)

		y = None

def onStart(s, e):
	for window in Application.Current.Windows:
		if clr.GetClrType(Agent).IsInstanceOfType(window):
			window.PreviewMouseLeftButtonDown += onPreviewMouseLeftButtonDown
			window.MouseLeftButtonUp += onMouseLeftButtonUp

def onStop(s, e):
	for window in Application.Current.Windows:
		if clr.GetClrType(Agent).IsInstanceOfType(window):
			window.PreviewMouseLeftButtonDown -= onPreviewMouseLeftButtonDown
			window.MouseLeftButtonUp -= onMouseLeftButtonUp
			
y = None
storyboard = None
Script.Instance.Start += onStart
Script.Instance.Stop += onStop