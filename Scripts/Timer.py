# -*- coding: utf-8 -*-
# Timer.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import String, DateTime, TimeSpan, Math
from System.Collections.Generic import List
from System.Globalization import CultureInfo
from System.Windows import Application, Window
from System.Windows.Controls import MenuItem, Separator
from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from Apricot import Script, Entry

def onTick(timer, e):
	global ts

	ts = ts.Add(timer.Interval)
	entry = Entry()
	
	if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
		entry.Title = String.Format("予定の{0}分前になりました", Math.Abs(ts.Minutes).ToString())
	else:
		if Math.Abs(ts.Minutes) == 1:
			entry.Title = String.Format("{0} minute remaining", Math.Abs(ts.Minutes).ToString())
		else:
			entry.Title = String.Format("{0} minutes remaining", Math.Abs(ts.Minutes).ToString())

	if ts.TotalMilliseconds >= 0:
		timer.Stop()
		ts = TimeSpan.Zero

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			entry.Title = "予定の時間になりました"
		else:
			entry.Title= "Time expired"

	entryList = List[Entry]()
	entryList.Add(entry)
	Script.Instance.Alert(entryList)

def onOpened(s, e):
	global menuItem, timer

	menuItem.Items.Clear()

	if timer.IsEnabled:
		def onClick1(sender, args):
			global timer

			timer.Stop()

		stopMenuItem = MenuItem()
		stopMenuItem.Click += onClick1

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			stopMenuItem.Header = "停止"
		else:
			stopMenuItem.Header = "Stop"

		menuItem.Items.Add(stopMenuItem)
		menuItem.Items.Add(Separator())

	def onClick2(sender, args):
		global timer, ts

		ts = TimeSpan(0, -1 * sender.Tag, 0)
		timer.Start()

	for i in range(1, 6):
		mi = MenuItem()
		mi.Click += onClick2
		mi.Tag = i

		if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
			mi.Header = i.ToString() + "分"
		else:
			if i == 1:
				mi.Header = i.ToString() + " minute"
			else:
				mi.Header = i.ToString() + " minutes"

		menuItem.Items.Add(mi)

def onStart(s, e):
	global menuItem, separator
	
	for window in Application.Current.Windows:
		if window is Application.Current.MainWindow and window.ContextMenu is not None:
			if not window.ContextMenu.Items.Contains(menuItem):
				window.ContextMenu.Opened += onOpened
				window.ContextMenu.Items.Insert(window.ContextMenu.Items.Count - 4, menuItem)
				
				if not clr.GetClrType(Separator).IsInstanceOfType(window.ContextMenu.Items[10]):
					separator = Separator()
					window.ContextMenu.Items.Insert(10, separator)
	
def onStop(s, e):
	global timer
	
	timer.Stop()

ts = TimeSpan.Zero
timer = DispatcherTimer(DispatcherPriority.Background)
timer.Tick += onTick
timer.Interval = TimeSpan.FromMinutes(1)
menuItem = MenuItem()
separator = None

if CultureInfo.CurrentCulture.Equals(CultureInfo.GetCultureInfo("ja-JP")):
	menuItem.Header = "タイマー"
else:
	menuItem.Header = "Timer"

Script.Instance.Start += onStart
Script.Instance.Stop += onStop