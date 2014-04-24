# -*- coding: utf-8 -*-
# Talking.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System.Globalization import CultureInfo
from System.Windows import Application, Window
from System.Windows.Controls import MenuItem, Separator
from Apricot import Script

def onClick(s, e):
	Script.Instance.Activate()

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
	menuItem.Header = "次のトーク"
else:
	menuItem.Header = "Talk"

Script.Instance.Start += onStart