# -*- coding: utf-8 -*-
# BouyomiChan.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("PresentationFramework")
clr.AddReferenceByPartialName("Apricot")

from System import Object, Byte, Convert
from System.Collections.Generic import List
from System.IO import BufferedStream, BinaryWriter
from System.Diagnostics import Process
from System.Net.Sockets import TcpClient, NetworkStream
from System.Text import Encoding
from System.Threading.Tasks import Task, TaskCreationOptions
from System.Windows import Application, Window, DependencyPropertyChangedEventArgs
from Apricot import Balloon, Message, Script

def tryConnect(address, port):
	try:
		return TcpClient(address, port)
	
	except:
		return None

def speech(text):
	def onSpeech():
		for process in Process.GetProcesses():
			if process.ProcessName.Equals("BouyomiChan"):
				tcpClient = tryConnect("127.0.0.1", 50001)
		
				if tcpClient is not None:
					ns = None
					bs = None
					bw = None
					bytes = Encoding.UTF8.GetBytes(text)
			
					try:
						ns = tcpClient.GetStream()
						bs = BufferedStream(ns)
						bw = BinaryWriter(bs)
						bw.Write(Convert.ToInt16(0x0001))
						bw.Write(Convert.ToInt16(-1))
						bw.Write(Convert.ToInt16(-1))
						bw.Write(Convert.ToInt16(-1))
						bw.Write(Convert.ToInt16(1))
						bw.Write(Convert.ToByte(0))
						bw.Write(Convert.ToInt32(bytes.Length))
						bw.Write(bytes)
				
					finally:
						if bs is not None:
							bs.Close()
					
						if bw is not None:
							bw.Close()
					
						if ns is not None:
							ns.Close()		
	
						tcpClient.Close()
	
				break

	Task.Factory.StartNew(onSpeech, TaskCreationOptions.LongRunning)
	
def onIsVisibleChanged(s, e):
	if e.NewValue == True and s.Messages.Count > 0:
		speech(s.Messages[s.Messages.Count - 1].Text)

def onStart(s, e):
	global balloonList
	
	tempList = List[Balloon]()

	for window in Application.Current.Windows:
		if clr.GetClrType(Balloon).IsInstanceOfType(window):
			if not balloonList.Contains(window):
				window.IsVisibleChanged += onIsVisibleChanged
				balloonList.Add(window)
				
			tempList.Add(window)
		
	balloonList.Clear()
	balloonList.AddRange(tempList)

balloonList = List[Balloon]()
Script.Instance.Start += onStart